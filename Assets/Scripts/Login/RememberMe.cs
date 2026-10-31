using Fusion;
using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class RememberMe : MonoBehaviour
{
    [Header("Reference from Project")]
    [SerializeField] RememberMeRefSO _rememberMeSO;

    [Header("Scene Reference")]
    [ScenePath] public string lobbyScenePath;
    [ScenePath] public string customizationScenePath;
    [ScenePath] public string gameScenePath;

    [Header("Reference from Itself")]
    [SerializeField] Camera _globalCamera;

    [Header("Reference from RememberMe_Canvas")]
    [SerializeField] GameObject _rememberMeCanvas;
    [SerializeField] GameObject _warningPanel;
    [SerializeField] GameObject _loadingPanel;

    [Header("Reference from Warning_Panel")]
    [SerializeField] TMP_Text _warningTxt;

    [Header("Reference from Loading_Panel")]
    [SerializeField] Image _loadingIcon;
    [SerializeField] TMP_Text _loadingTxt;

    [Header("Player Data")]
    public string playerName;
    public int playerLevel;

    [Header("Avatar Data")]
    public string avatarGender;
    public string avatarClothesJSON;

    [Header("Scene Transition Data")]
    [Tooltip("Must be left Blank for default or else LobbyManager will load the scene immediately")]
    public SceneTransitionSO sceneTransitionSO;

    [Header("Scene Creation Data")]
    public string scenePassword;
    public int playerLimit;
    public string sessionName;
    public bool automaticallyHost;
    public bool automaticallyJoin;

    [Header("Bools Read-Only")]
    public bool timedOutError;
    public bool disconnectedServerError;
    public bool kicked;

    [Header("Read-Only")]
    public NetworkRunner runner;

    private string SAVE_KEY = "Rukha93_SavedCharacterData";

    private void Awake()
    {
        Application.runInBackground = true;

        DontDestroyOnLoad(this.gameObject);

        _rememberMeSO.Instance = this;

        EnableMainCamera(true);
        ToggleRememberMeCanvas(false); //Disable Canvas by default;
    }

    #region Calls by other script to Enable specific panel etc
    public void EnableMainCamera(bool yeah)
    {
        _globalCamera.gameObject.SetActive(yeah);
    }

    public void EnableLoading(string message)
    {
        _rememberMeCanvas.SetActive(true);
        _loadingPanel.SetActive(true);
        _loadingTxt.text = message;
    }

    public void EnableWarning(string message)
    {
        _rememberMeCanvas.SetActive(true);
        _warningPanel.SetActive(true);
        _warningTxt.text = message;
    }


    //Calls in AvatarApplier.cs and CustomizationDemo.cs once they wanted to fetch clothes data
    public async Task<CharacterSaveData> LoadAvatarClothesDataAsync()
    {
        string clothesJSON = "";

        if (!string.IsNullOrEmpty(avatarClothesJSON))
        {
            clothesJSON = avatarClothesJSON;
        }
        else if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            avatarClothesJSON = PlayerPrefs.GetString(SAVE_KEY);
            clothesJSON = PlayerPrefs.GetString(SAVE_KEY);
        }
        else
        {
            Debug.LogWarning("[AvatarApplier] No saved character data found! we will be Fetching from API");
            await Task.Delay(5000); // Simulated network delay for testing
        }

        if (string.IsNullOrEmpty(clothesJSON)) 
        {
            Debug.LogWarning("No saved character data found!");
            return null;
        } 

        CharacterSaveData saveData = JsonUtility.FromJson<CharacterSaveData>(clothesJSON);
        UpdateGender(saveData);
        return saveData;
    }

    public void UpdateGender(CharacterSaveData saveData) //Calls by CustomizationDemo.cs once it will be saving new data
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
        avatarGender = (bodyGenderCode == "m") ? "Male" : "Female";
    }
    #endregion

    //This is also onclick of Okay_Btn from warning_panel
    public void ToggleRememberMeCanvas(bool yeah)
    {
        _rememberMeCanvas.SetActive(yeah);

        if (!yeah)
        {
            ToggleLoadingPanel(false);
            ToggleWarningPanel(false);
        }
    }

    public void ToggleLoadingPanel(bool yeah)
    {
        _loadingPanel.SetActive(yeah);
        _loadingTxt.text = "Please Wait";
    }

    public void ToggleWarningPanel(bool yeah)
    {
        _warningPanel.SetActive(yeah);
    }


    public void LoadSceneAdditive(string scenePath, Action onComplete = null)
    {
        StartCoroutine(Co_LoadSceneAdditive(scenePath, onComplete));
    }

    private IEnumerator Co_LoadSceneAdditive(string scenePath, Action onComplete)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError("[SceneLoader] Provided scene path is null or empty!");
            yield break;
        }

        //Start loading the scene additively in the background
        UnityEngine.AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);

        if (asyncLoad == null)
        {
            Debug.LogError($"[SceneLoader] Failed to start loading scene at path: {scenePath}. Ensure it is added to Build Settings!");
            yield break;
        }

        //Wait until the scene has fully loaded
        while (!asyncLoad.isDone)
        {
            // We can track progress here
            yield return null;
        }

        Debug.Log($"<color=green>[SceneLoader] Successfully loaded scene additively: {scenePath}</color>");

        //Execute optional callback
        onComplete?.Invoke();
    }

    public void UnloadScene(string sceneName, Action onComplete = null)
    {
        StartCoroutine(Co_UnloadAdditiveScene(sceneName, onComplete));
    }

    private IEnumerator Co_UnloadAdditiveScene(string scenePathOrName, Action onComplete)
    {
        if (string.IsNullOrEmpty(scenePathOrName))
        {
            Debug.LogError("[SceneUnloader] Scene name cannot be null or empty.");
            yield break;
        }

        // Clean path (e.g., converts "Assets/Scenes/AvatarCustomization.unity" -> "AvatarCustomization")
        string cleanSceneName = Path.GetFileNameWithoutExtension(scenePathOrName);

        // 1. Find scene by short name
        Scene sceneToUnload = SceneManager.GetSceneByName(cleanSceneName);

        // 2. Fallback check: if GetSceneByName failed, iterate loaded scenes by path
        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.path == scenePathOrName || s.name == cleanSceneName)
                {
                    sceneToUnload = s;
                    break;
                }
            }
        }

        // 3. Safety Check: Verify scene is found and loaded
        if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded)
        {
            Debug.LogWarning($"[SceneUnloader] Additive scene '{scenePathOrName}' (parsed as '{cleanSceneName}') is not loaded or valid.");
            yield break;
        }

        // 4. Safety Check: Don't unload if it's the only remaining scene
        if (SceneManager.sceneCount <= 1)
        {
            Debug.LogError($"[SceneUnloader] Cannot unload '{cleanSceneName}' because it is the only loaded scene!");
            yield break;
        }

        // 5. Unload scene asynchronously
        UnityEngine.AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(sceneToUnload);

        if (asyncUnload == null)
        {
            Debug.LogError($"[SceneUnloader] Failed to initiate unload for scene: {cleanSceneName}");
            yield break;
        }

        while (!asyncUnload.isDone)
        {
            yield return null;
        }

        Debug.Log($"<color=yellow>[SceneUnloader] Successfully unloaded additive scene: {cleanSceneName}</color>");

        // Clean up unreferenced asset memory
        yield return Resources.UnloadUnusedAssets();

        onComplete?.Invoke();
    }

    //Calls in GameManager.cs after Spawning a Player
    public void ResetSceneSessionData()
    {
        //Clear the scene reference, so next time we know what we should load next
        sceneTransitionSO = null;
        automaticallyHost = false;
        automaticallyJoin = false;

        /*
        scenePassword = null;
        playerLimit = 20;
        automaticallyHost = false;
        automaticallyJoin = false;
        */
    }
}
