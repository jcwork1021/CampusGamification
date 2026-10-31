using Fusion;
using Rukha93.ModularAnimeCharacter.Customization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarApplier : NetworkBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] AvatarApplierRefSO _avatarApplierSO;
    [SerializeField] FakeLoaderRefSO _fakeLoaderSO;
    [SerializeField] RememberMeRefSO _rememberMeSO;

    [Header("Target Character Setup")]
    [Tooltip("Assign your pre-spawned character Animator here. If left empty, a body prefab will be instantiated automatically.")]
    public Animator targetBody;

    [Header("Character Info & References (Read-Only)")]

    [Tooltip("Reference to the instantiated head/face object once loaded.")]
    public GameObject instantiatedHead;

    [Header("Read-Only")]
    [Networked, OnChangedRender(nameof(OnPlayerClothesChanged))]
    public NetworkString<_512> playerClothesJSON { get; set; }

    private SkinnedMeshRenderer m_ReferenceMesh;
    private Dictionary<BodyPartType, BodyPartTag> m_BodyParts = new Dictionary<BodyPartType, BodyPartTag>();
    private Dictionary<string, CustomizationItemAsset> m_EquipedAssets = new Dictionary<string, CustomizationItemAsset>();


    private void OnDestroy()
    {
        if (HasInputAuthority)
        {
            _avatarApplierSO.Instance = null;
        }
    }
 
    public override void Spawned()
    {
        
        // Auto-assign targetBody locally on EVERY client (Local, Host, and Remote Clients)
        if (targetBody == null)
        {
            targetBody = GetComponentInChildren<Animator>();
        }

        // Only the owner with authority reads their saved local JSON and sets the networked property
        if (HasInputAuthority)
        {
            _avatarApplierSO.Instance = this;
            LoadAndBuildCharacter();
        }
        // If we are a remote client / late joiner and the JSON is already synchronized:
        else if (!string.IsNullOrEmpty(playerClothesJSON.ToString()))
        {
            OnPlayerClothesChanged();
        }
        
    }
    

    public void LoadAndSyncCharacterClothes()
    {
        // Auto-assign targetBody locally on EVERY client (Local, Host, and Remote Clients)
        if (targetBody == null)
        {
            targetBody = GetComponentInChildren<Animator>();
        }

        // Only the owner with authority reads their saved local JSON and sets the networked property
        if (HasInputAuthority)
        {
            LoadAndBuildCharacter();
        }
        // If we are a remote client / late joiner and the JSON is already synchronized:
        else if (!string.IsNullOrEmpty(playerClothesJSON.ToString()))
        {
            OnPlayerClothesChanged();
        }
    }


    private void LoadAndBuildCharacter()
    {
        // Make sure authority check is safe
        if (!HasInputAuthority && !HasStateAuthority) return;

        //When playerClothesJSON is changed like below OnPlayerCothesChanged() function will automatically be called
        playerClothesJSON = _rememberMeSO.Instance.avatarClothesJSON;
    }

    private IEnumerator Co_BuildCharacter(CharacterSaveData saveData)
    {
        // 1. Determine body type ("f" or "m") and set public gender variable
        int bodyIndex = saveData.categories.IndexOf("body");
        string bodyGenderCode = "f";

        if (bodyIndex != -1)
        {
            string bodyPath = saveData.equippedPaths[bodyIndex];
            bodyGenderCode = bodyPath.StartsWith("m") ? "m" : "f";
        }

        //Saved Gender for Reference
        _rememberMeSO.Instance.avatarGender = (bodyGenderCode == "m") ? "Male" : "Female";

        // 2. Setup Target Body Reference
        if (targetBody == null)
        {
            Debug.LogError("There is no Target body so we should not proceed" + gameObject.name);
            yield break;
        }

        m_BodyParts.Clear();
        m_EquipedAssets.Clear();

        // Find reference mesh and bone targets from targetBody
        var meshes = targetBody.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (meshes.Length > 0)
        {
            m_ReferenceMesh = meshes[0]; // Grab main reference mesh
        }
        else
        {
            Debug.LogError("[PlayerCharacterApplier] Target body has no SkinnedMeshRenderer to use as bone reference!");
            yield break;
        }

        // Cache body part tags for mesh hiding logic
        var bodyPartsArray = targetBody.GetComponentsInChildren<BodyPartTag>();
        foreach (var part in bodyPartsArray)
            m_BodyParts[part.type] = part;

        // 3. Load and Equip each saved clothing/accessory item
        for (int i = 0; i < saveData.categories.Count; i++)
        {
            string cat = saveData.categories[i];
            string path = saveData.equippedPaths[i];

            if (cat.Equals("body") || string.IsNullOrEmpty(path))
                continue;

            yield return StartCoroutine(Co_EquipItem(cat, path));
        }

        // 4. Update body hiding rules (turn off body meshes obscured by clothing)
        UpdateBodyRenderers();

        Debug.Log($"<color=green>[PlayerCharacterApplier] {_rememberMeSO.Instance.avatarGender} character equipment successfully applied!</color>");
    }

    private IEnumerator Co_EquipItem(string category, string path)
    {
        bool itemLoaded = false;
        CustomizationItemAsset itemAsset = null;

        yield return _fakeLoaderSO.Instance.LoadAsset<CustomizationItemAsset>(path, res =>
        {
            itemAsset = res;
            itemLoaded = true;
        });

        while (!itemLoaded) yield return null;

        if (itemAsset == null) yield break;

        m_EquipedAssets[category] = itemAsset;

        List<GameObject> instantiatedObjs = new List<GameObject>();

        // 1. Instantiate Skinned Meshes (Clothes, Hair, Accessories)
        foreach (var mesh in itemAsset.meshes)
        {
            GameObject go = new GameObject(mesh.name);

            go.transform.SetParent(targetBody.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            SkinnedMeshRenderer smr = go.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh.sharedMesh;
            smr.sharedMaterials = mesh.sharedMaterials;

            // Re-bind bones to reference body skeleton
            smr.rootBone = m_ReferenceMesh.rootBone;
            smr.bones = m_ReferenceMesh.bones;
            smr.localBounds = m_ReferenceMesh.localBounds;

            instantiatedObjs.Add(go);

            if ((category.Equals("head") || category.Equals("hair")) && instantiatedHead == null)
            {
                instantiatedHead = go;
            }
        }

        // 2. Instantiate Props attached to target bones (hair objects, accessories, etc.)
        foreach (var obj in itemAsset.objects)
        {
            Transform parentBone = null;

            // Attempt 1: Check Character Creator / Mixamo bone naming explicitly
            string targetName = obj.targetBone.ToString();
            parentBone = FindBoneInHierarchy(targetBody.transform, targetName);

            // Attempt 2: Humanoid Animator fallback
            if (parentBone == null && targetBody.isHuman)
            {
                parentBone = targetBody.GetBoneTransform(obj.targetBone);
            }

            // Attempt 3: Specifically check for Character Creator Head tip bone or generic Head
            if (parentBone == null && (category.Equals("hair") || category.Equals("head")))
            {
                parentBone = FindBoneInHierarchy(targetBody.transform, "CC_Base_Head_end")
                          ?? FindBoneInHierarchy(targetBody.transform, "CC_Base_Head")
                          ?? FindBoneInHierarchy(targetBody.transform, "head");
            }

            // Attempt 4: Ultimate Fallback to Target Body transform
            if (parentBone == null)
            {
                parentBone = targetBody.transform;
            }

            GameObject go = Instantiate(obj.prefab, parentBone, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            instantiatedObjs.Add(go);

            if ((category.Equals("head") || category.Equals("hair")) && instantiatedHead == null)
            {
                instantiatedHead = go;
            }
        }

        // Notify animator controllers or sub-scripts on the body if needed
        targetBody.SendMessage("OnChangeEquip", new object[] { category, instantiatedObjs }, SendMessageOptions.DontRequireReceiver);
    }

    // Searches the transform hierarchy for exact or partial bone name matches (CC_Base_Head_end, Head, etc.)
    private Transform FindBoneInHierarchy(Transform root, string targetBoneName)
    {
        Transform[] allTransforms = root.GetComponentsInChildren<Transform>(true);

        foreach (Transform t in allTransforms)
        {
            // Direct exact match
            if (t.name.Equals(targetBoneName, System.StringComparison.OrdinalIgnoreCase))
                return t;

            // Match if rig has prefix/suffix (e.g., "Armature/CC_Base_Head_end")
            if (t.name.EndsWith(targetBoneName, System.StringComparison.OrdinalIgnoreCase))
                return t;
        }

        return null;
    }

    private void UpdateBodyRenderers()
    {
        List<BodyPartType> disabledParts = new List<BodyPartType>();

        foreach (var asset in m_EquipedAssets.Values)
        {
            if (asset == null || asset.bodyParts == null) continue;

            foreach (var part in asset.bodyParts)
            {
                if (!disabledParts.Contains(part))
                    disabledParts.Add(part);
            }
        }

        foreach (var part in m_BodyParts)
        {
            part.Value.gameObject.SetActive(!disabledParts.Contains(part.Key));
        }

        // Adjust height offset if wearing shoes
        Vector3 localPos = targetBody.transform.localPosition;
        localPos.y = m_EquipedAssets.ContainsKey("shoes") ? 0.02f : 0f;
        //targetBody.transform.localPosition = localPos;
    }

    #region Networked Function
    private void OnPlayerClothesChanged()
    {
        string jsonString = playerClothesJSON.ToString();
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("The JSON Clothes is empty");
            return;
        }

        CharacterSaveData saveData = JsonUtility.FromJson<CharacterSaveData>(jsonString);
        if (saveData != null)
        {
            StartCoroutine(Co_BuildCharacter(saveData));
        }
    }
    #endregion
}