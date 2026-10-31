using Rukha93.ModularAnimeCharacter.Customization;
using Rukha93.ModularAnimeCharacter.Customization.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CustomizationDemo : MonoBehaviour
{
    public class EquipedItem
    {
        public string path;
        public List<GameObject> instantiatedObjects;
        public CustomizationItemAsset assetReference;

        // for material customization
        public Renderer[] renderers;
    }

    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;

    [SerializeField] private UICustomizationDemo m_UI;

    [Header("Save & Load UI Buttons")]
    [SerializeField] private Button m_SaveButton;
    [SerializeField] private Button m_LoadButton;

    [Header("Read-Only")]
    [SerializeField] string savedJSON;
    [SerializeField] string loadedJSON;

    private const string SAVE_KEY = "Rukha93_SavedCharacterData";

    private IAssetLoader m_AssetLoader;
    private List<string> m_Categories = new List<string>
        {
            "body",
            "head",
            "hairstyle",
            "acc_head",
            "top",
            "bottom",
            "shoes",
            "outfit"
        };

    private Animator m_Character;
    private SkinnedMeshRenderer m_ReferenceMesh;

    private Dictionary<string, List<string>> m_CustomizationOptions; // <categoryId, assetPath[]>

    private Dictionary<BodyPartType, BodyPartTag> m_BodyParts;
    private Dictionary<string, EquipedItem> m_Equiped;
    private Dictionary<Material, MaterialPropertyBlock> m_MaterialProperties;

    private Coroutine m_LoadingCoroutine;

    private void Awake()
    {
        // init variables
        m_AssetLoader = GetComponentInChildren<IAssetLoader>();
        m_CustomizationOptions = new Dictionary<string, List<string>>();
        m_Equiped = new Dictionary<string, EquipedItem>();
        m_BodyParts = new Dictionary<BodyPartType, BodyPartTag>();
        m_MaterialProperties = new Dictionary<Material, MaterialPropertyBlock>();

        // ui callbacks
        if (m_UI != null)
        {
            m_UI.OnClickCategory += OnSelectCategory;
            m_UI.OnChangeItem += OnSwapItem;
            m_UI.OnChangeColor += OnChangeColor;
        }

        // Save & Load UI Button Callbacks
        if (m_SaveButton != null)
            m_SaveButton.onClick.AddListener(SaveCharacter);

        if (m_LoadButton != null)
            m_LoadButton.onClick.AddListener(LoadCharacter);
    }

    private void Start()
    {
        // init categories UI
        if (m_UI != null)
        {
            m_UI.SetCategories(m_Categories.ToArray());
            for (int i = 0; i < m_Categories.Count; i++)
                m_UI.SetCategoryValue(i, "");
        }

        m_LoadingCoroutine = StartCoroutine(Co_LoadAndInitBody("f"));
    }

    #region SAVE & LOAD SYSTEM

    public void SaveCharacter()
    {
        CharacterSaveData saveData = new CharacterSaveData();

        foreach (var kvp in m_Equiped)
        {
            if (kvp.Value != null && !string.IsNullOrEmpty(kvp.Value.path))
            {
                saveData.categories.Add(kvp.Key);
                saveData.equippedPaths.Add(kvp.Value.path);
            }
        }

        savedJSON = JsonUtility.ToJson(saveData, true);
        PlayerPrefs.SetString(SAVE_KEY, savedJSON);
        PlayerPrefs.Save();

        //Saved to RememberMe so we do not need to call it again later in API
        _rememberMeSO.Instance.avatarClothesJSON = savedJSON;

        //Update the Gender in RemembeMe.cs for debug and for Spawning player
        _rememberMeSO.Instance.UpdateGender(saveData);

        Debug.Log($"<color=green>[CustomizationDemo] Saved Character JSON:</color>\n{savedJSON}");
    }

    //Calls in Avatar Customization
    public async void LoadCharacter()
    {
        // Stop any in-progress character loading coroutines
        if (m_LoadingCoroutine != null)
        {
            StopCoroutine(m_LoadingCoroutine);
            m_LoadingCoroutine = null;
        }

        // 1. Fetch data asynchronously (Handles API delay / Local fallback)
        CharacterSaveData saveData = await _rememberMeSO.Instance.LoadAvatarClothesDataAsync();

        // 2. Validate data
        if (saveData == null)
        {
            Debug.LogWarning("[CustomizationDemo] Failed to load character save data!");
            return;
        }

        // 3. Pass data into your existing coroutine
        m_LoadingCoroutine = StartCoroutine(Co_ApplySavedCharacter(saveData));
    }

    private IEnumerator Co_ApplySavedCharacter(CharacterSaveData saveData)
    {
        // 1. Determine body type/gender from the saved body path
        int bodyIndex = saveData.categories.IndexOf("body");
        string bodyGender = "f"; // fallback default

        if (bodyIndex != -1)
        {
            string bodyPath = saveData.equippedPaths[bodyIndex];
            bodyGender = bodyPath.StartsWith("m") ? "m" : "f";
        }

        // 2. Re-initialize base body
        yield return StartCoroutine(Co_LoadAndInitBody(bodyGender));

        // 3. Equip saved items across categories
        for (int i = 0; i < saveData.categories.Count; i++)
        {
            string cat = saveData.categories[i];
            string path = saveData.equippedPaths[i];

            if (cat.Equals("body") || string.IsNullOrEmpty(path))
                continue;

            yield return StartCoroutine(Co_LoadAndEquip(cat, path));
        }

        m_LoadingCoroutine = null;
        Debug.Log("<color=cyan>[CustomizationDemo] Character loaded successfully!</color>");
    }
    #endregion

    private void InitBody(string path, GameObject prefab)
    {
        // instantiate the body prefab and store the animator
        m_Character = Instantiate(prefab, this.transform).GetComponent<Animator>();

        // get a random body mesh to be used as reference
        var meshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        m_ReferenceMesh = meshes[meshes.Length / 2];

        // initialize all tagged body parts
        // they be used to disable meshes that are hidden by clothes
        var bodyparts = m_Character.GetComponentsInChildren<BodyPartTag>();
        foreach (var part in bodyparts)
            m_BodyParts[part.type] = part;

        var equip = new EquipedItem()
        {
            path = path,
            assetReference = null,
            instantiatedObjects = new List<GameObject>() { m_Character.gameObject }
        };
        InitRenderersForItem(equip);
        m_Equiped["body"] = equip;

        // update ui
        if (m_UI != null)
        {
            m_UI.SetCategoryValue(m_Categories.IndexOf("body"), path);
            if (m_UI.IsCustomizationOpen && m_UI.CurrentCategory == "body")
                m_UI.SetCustomizationMaterials(equip.renderers);
        }
    }

    private IEnumerator Co_LoadAndInitBody(string bodyType)
    {
        // destroy old character
        if (m_Character != null)
        {
            Destroy(m_Character.gameObject);

            // clear old items
            m_Equiped.Clear();
            m_CustomizationOptions.Clear();
            m_BodyParts.Clear();
        }

        // init the customization options for the selected body type
        List<Coroutine> coroutines = new List<Coroutine>();
        for (int i = 0; i < m_Categories.Count; i++)
        {
            int index = i;
            string path = m_Categories[i].Equals("body") ? "body" : GetAssetPath(bodyType, m_Categories[i]);
            coroutines.Add(StartCoroutine(m_AssetLoader.LoadAssetList(path, res => m_CustomizationOptions[m_Categories[index]] = new List<string>(res))));
        }
        for (int i = 0; i < m_Categories.Count; i++)
        {
            yield return coroutines[i];

            // add an empty item for all categories that can be empty
            if (m_Categories[i].Equals("body"))
                continue;
            if (m_Categories[i].Equals("head"))
                continue;
            m_CustomizationOptions[m_Categories[i]].Insert(0, "");
        }

        // initialize the body
        var bodyPath = GetAssetPath(bodyType, "body");
        yield return m_AssetLoader.LoadAsset<GameObject>(bodyPath, res => InitBody(bodyPath, res));

        // initialize the head with the first available
        string assetPath = m_CustomizationOptions["head"][0];
        yield return m_AssetLoader.LoadAsset<CustomizationItemAsset>(assetPath, res => Equip("head", assetPath, res));

        m_LoadingCoroutine = null;
    }

    #region EQUIPMENT

    public void Equip(string cat, string path, CustomizationItemAsset item)
    {
        // if outfit, remove all other pieces
        if (cat.Equals("outfit"))
        {
            Unequip("top", false);
            Unequip("bottom", false);
        }
        else if (cat.Equals("top") || cat.Equals("bottom"))
        {
            Unequip("outfit", false);
        }

        // unequip previous item
        Unequip(cat, false);

        EquipedItem equip = new EquipedItem()
        {
            path = path,
            assetReference = item,
            instantiatedObjects = new List<GameObject>()
        };
        m_Equiped[cat] = equip;

        // instantiate new meshes, init properties, parent to character
        GameObject go = null;
        SkinnedMeshRenderer skinnedMesh = null;
        foreach (var mesh in item.meshes)
        {
            // instantiate new gameobject
            go = new GameObject(mesh.name);
            go.transform.SetParent(m_Character.transform, false);
            m_Equiped[cat].instantiatedObjects.Add(go);

            // add the renderer
            skinnedMesh = go.AddComponent<SkinnedMeshRenderer>();
            skinnedMesh.rootBone = m_ReferenceMesh.rootBone;
            skinnedMesh.bones = m_ReferenceMesh.bones;
            skinnedMesh.localBounds = m_ReferenceMesh.localBounds;
            skinnedMesh.sharedMesh = mesh.sharedMesh;
            skinnedMesh.sharedMaterials = mesh.sharedMaterials;
        }

        // instantiate objects, parent to target bones
        foreach (var obj in item.objects)
        {
            go = Instantiate(obj.prefab, m_Character.GetBoneTransform(obj.targetBone));
            equip.instantiatedObjects.Add(go);
        }

        // update bodyparts
        UpdateBodyRenderers();

        // map renderers
        InitRenderersForItem(equip);

        // update ui
        if (m_UI != null)
        {
            m_UI.SetCategoryValue(m_Categories.IndexOf(cat), path);
            if (m_UI.IsCustomizationOpen && m_UI.CurrentCategory == cat)
                m_UI.SetCustomizationMaterials(equip.renderers);
        }

        // send message to the character
        m_Character.SendMessage("OnChangeEquip", new object[] { cat, equip.instantiatedObjects }, SendMessageOptions.DontRequireReceiver);
    }

    private IEnumerator Co_LoadAndEquip(string cat, string path)
    {
        yield return m_AssetLoader.LoadAsset<CustomizationItemAsset>(path, res => Equip(cat, path, res));
        m_LoadingCoroutine = null;
    }

    public void Unequip(string category, bool updateRenderers = true)
    {
        if (m_Equiped.ContainsKey(category) == false)
            return;

        var item = m_Equiped[category];
        m_Equiped.Remove(category);

        // destroy instances
        foreach (var go in item.instantiatedObjects)
            Destroy(go);

        // update body parts
        if (updateRenderers)
            UpdateBodyRenderers();

        // update UI
        if (m_UI != null)
        {
            m_UI.SetCategoryValue(m_Categories.IndexOf(category), "");
            if (m_UI.IsCustomizationOpen)
                m_UI.SetCustomizationMaterials(null);
        }
    }

    public void UpdateBodyRenderers()
    {
        List<BodyPartType> disabled = new List<BodyPartType>();

        // get all parts that are hidden by equips
        foreach (var equip in m_Equiped.Values)
        {
            if (equip.assetReference == null)
                continue;

            foreach (var part in equip.assetReference.bodyParts)
                if (!disabled.Contains(part))
                    disabled.Add(part);
        }

        // set active value of each part
        foreach (var part in m_BodyParts)
            part.Value.gameObject.SetActive(!disabled.Contains(part.Key));

        var localPos = m_Character.transform.localPosition;
        localPos.y = m_Equiped.ContainsKey("shoes") ? 0.02f : 0;
        m_Character.transform.localPosition = localPos;
    }

    private void SyncMaterialChange(Material sharedMaterial, MaterialPropertyBlock newProperties)
    {
        foreach (var equip in m_Equiped.Values)
        {
            foreach (var renderer in equip.renderers)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] != sharedMaterial)
                        continue;
                    renderer.SetPropertyBlock(newProperties, i);
                }
            }
        }
    }

    private void SyncNewItemMaterials(Renderer renderer)
    {
        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            if (m_MaterialProperties.ContainsKey(renderer.sharedMaterials[i]) == false)
                continue;
            renderer.SetPropertyBlock(m_MaterialProperties[renderer.sharedMaterials[i]], i);
        }
    }

    #endregion

    private void InitRenderersForItem(EquipedItem item)
    {
        List<Renderer> renderers = new List<Renderer>();

        foreach (var obj in item.instantiatedObjects)
            renderers.AddRange(obj.GetComponentsInChildren<Renderer>());

        item.renderers = renderers.ToArray();

        foreach (var renderer in item.renderers)
            SyncNewItemMaterials(renderer);
    }

    #region HELPERS

    public string GetAssetPath(string bodyType, string asset)
    {
        return $"{bodyType}/{asset}".ToLower();
    }

    public string GetAssetPath(string bodyType, string category, string asset)
    {
        return $"{bodyType}/{category}/{asset}".ToLower();
    }

    #endregion

    #region UI CALLBACKS

    private void OnSelectCategory(string cat)
    {
        if (m_UI == null) return;

        if (string.Equals(m_UI.CurrentCategory, cat))
        {
            m_UI.ShowCustomization(false);
            return;
        }

        m_UI.SetCustomizationOptions(cat, m_CustomizationOptions[cat].ToArray(), m_Equiped.ContainsKey(cat) ? m_Equiped[cat].path : "");

        if (m_Equiped.ContainsKey(cat))
            m_UI.SetCustomizationMaterials(m_Equiped[cat].renderers);
        else
            m_UI.SetCustomizationMaterials(null);

        m_UI.ShowCustomization(true);
    }

    private void OnSwapItem(string cat, string asset)
    {
        if (string.IsNullOrEmpty(asset))
        {
            Unequip(cat);
            return;
        }

        if (m_LoadingCoroutine != null)
            StopCoroutine(m_LoadingCoroutine);

        if (cat.Equals("body"))
            m_LoadingCoroutine = StartCoroutine(Co_LoadAndInitBody(asset.StartsWith("m") ? "m" : "f"));
        else
            m_LoadingCoroutine = StartCoroutine(Co_LoadAndEquip(cat, asset));
    }

    private void OnChangeColor(Renderer renderer, int materialIndex, string property, Color color)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block, materialIndex);

        block.SetColor(property, color);
        renderer.SetPropertyBlock(block, materialIndex);

        var sharedMaterial = renderer.sharedMaterials[materialIndex];
        m_MaterialProperties[sharedMaterial] = block;

        SyncMaterialChange(sharedMaterial, block);
    }

    #endregion
}