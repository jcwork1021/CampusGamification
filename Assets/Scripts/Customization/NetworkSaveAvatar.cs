using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class SaveAvatarRequest
{
    public int player_id;
    public CharacterSaveData avatarDetails; // Your existing customization class
}

[Serializable]
public class SaveAvatarResponse
{
    public string status;
    public string message;
}

// 1. Inner character customization data
[Serializable]
public class CharacterSaveData
{
    public List<string> categories = new List<string>();
    public List<string> equippedPaths = new List<string>();
}

// 2. Main Network Payload Wrapper
[Serializable]
public class PlayerPayload
{
    public int playerID;
    public CharacterSaveData Character; // Matches the key name in the JSON
}

public class NetworkSaveAvatar : MonoBehaviour
{
    private string apiUrl = "https://your-game-server.com/api/player/avatar";

    public void SaveAvatarToServer(int playerId, CharacterSaveData currentAvatarData)
    {
        StartCoroutine(Co_PostAvatarData(playerId, currentAvatarData));
    }

    private IEnumerator Co_PostAvatarData(int playerId, CharacterSaveData avatarData)
    {
        // 1. Wrap data into payload struct
        SaveAvatarRequest payload = new SaveAvatarRequest
        {
            player_id = playerId,
            avatarDetails = avatarData
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        // 2. Create UnityWebRequest POST
        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                SaveAvatarResponse response = JsonUtility.FromJson<SaveAvatarResponse>(request.downloadHandler.text);
                Debug.Log($"<color=green>[Server Response]: {response.message}</color>");
            }
            else
            {
                Debug.LogError($"[Network Error]: {request.error} | {request.downloadHandler.text}");
            }
        }
    }
}