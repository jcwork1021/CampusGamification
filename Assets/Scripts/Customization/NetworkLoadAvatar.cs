using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkLoadAvatar : MonoBehaviour
{
    [Header("Script References")]
    [SerializeField] private AvatarApplier _avatarApplierCS;
    [SerializeField] private CustomizationDemo _customizationDemoCS;

    [Header("Server Configuration")]
    [SerializeField] private string apiUrl = "https://your-game-server.com/api/player/avatar";

    [Header("Detected Character Data")]
    // Publicly accessible so other scripts can read the gender anytime
    public string currentGender = "Unknown";

    public void LoadAvatarFromServer(int playerId)
    {
        StartCoroutine(Co_GetAvatarData(playerId));
    }

    private IEnumerator Co_GetAvatarData(int playerId)
    {
        string requestUrl = $"{apiUrl}?player_id={playerId}";

        using (UnityWebRequest request = UnityWebRequest.Get(requestUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"<color=cyan>[Server Response Received]: {jsonResponse}</color>");

                // 1. Directly deserialize into your existing CharacterSaveData model
                CharacterSaveData saveData = JsonUtility.FromJson<CharacterSaveData>(jsonResponse);

                // 2. Extract Gender from asset paths (e.g., "m/body" -> "Male", "f/body" -> "Female")
                currentGender = ParseGenderFromSaveData(saveData);
                Debug.Log($"<color=yellow>[NetworkLoadAvatar] Detected Gender: {currentGender}</color>");

                // 3. Overwrite PlayerPrefs so local offline caching works seamlessly
                PlayerPrefs.SetString("Rukha93_SavedCharacterData", jsonResponse);
                PlayerPrefs.SetString("Rukha93_SavedGender", currentGender); // Storing gender in cache as well
                PlayerPrefs.Save();

                /*
                // 4. Rebuild character on screen immediately using your applier script
                if (_avatarApplierCS != null)
                {
                    _avatarApplierCS.LoadAndBuildCharacter();
                }

                if (_customizationDemoCS != null)
                {
                    _customizationDemoCS.LoadCharacter();
                }
                */
            }
            else
            {
                Debug.LogError($"[Network Error Fetching Character]: {request.error}");
            }
        }
    }

    /// <summary>
    /// Helper method to evaluate gender based on asset paths before models finish loading.
    /// </summary>
    private string ParseGenderFromSaveData(CharacterSaveData saveData)
    {
        if (saveData == null || saveData.equippedPaths == null || saveData.equippedPaths.Count == 0)
        {
            return "Unknown";
        }

        foreach (string path in saveData.equippedPaths)
        {
            if (string.IsNullOrEmpty(path)) continue;

            // Check path prefixes matching standard directory conventions
            if (path.StartsWith("m/") || path.StartsWith("male/"))
            {
                return "Male";
            }
            if (path.StartsWith("f/") || path.StartsWith("female/"))
            {
                return "Female";
            }
        }

        return "Unknown";
    }
}