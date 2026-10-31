using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class JoinRoomDetails : MonoBehaviour
{
    [Header("UI Text References")]
    public TMP_Text roomTxt;
    public TMP_Text hostNameTxt;
    public TMP_Text stageNameTxt; 
    public TMP_Text allowedLevelTxt;
    public TMP_Text playerCountTxt;

    [Header("Password Setup")]
    public GameObject lockIcon;

    [Header("Buttons")]
    public Button joinBtn;

    private SessionFinderManager _sessionFinderManagerCS;
    private SessionInfo _sessionInfo;
    public string _roomPassword = ""; //Calls by SessionFinderManager.cs to check password

    public void Setup(SessionInfo session, SessionFinderManager sessionFinderManager)
    {
        _sessionInfo = session;
        _sessionFinderManagerCS = sessionFinderManager;

        // 🔍 DEBUG LOG: Inspect received session properties
        if (session.Properties == null)
        {
            Debug.LogError($"[Setup] session.Properties is NULL for room '{session.Name}'!");
        }
        else
        {
            Debug.Log($"[Setup] Received {session.Properties.Count} properties for room '{session.Name}':");
            foreach (var kvp in session.Properties)
            {
                Debug.LogWarning($"   -> Key: '{kvp.Key}' | Value: '{kvp.Value.PropertyValue}'");
            }
        }

        // Basic Native Properties
        roomTxt.text = session.Name;
        playerCountTxt.text = $"{session.PlayerCount} / {session.MaxPlayers}";

        // Extract Custom Properties safely
        if (session.Properties != null && session.Properties.Count > 0)
        {
            // Host Name
            if (session.Properties.TryGetValue("HostName", out var host) && host.PropertyValue != null)
                hostNameTxt.text = host.PropertyValue.ToString();
            else
                hostNameTxt.text = "Host";

            // Stage Name
            if (stageNameTxt != null)
            {
                if (session.Properties.TryGetValue("StageName", out var stage) && stage.PropertyValue != null)
                    stageNameTxt.text = stage.PropertyValue.ToString();
                else
                    stageNameTxt.text = "Default Stage";
            }

            // Allowed Level
            if (allowedLevelTxt != null)
            {
                if (session.Properties.TryGetValue("AllowedLevel", out var level) && level.PropertyValue != null)
                    allowedLevelTxt.text = $"Lvl {level.PropertyValue}+";
                else
                    allowedLevelTxt.text = "Lvl 1+";
            }

            // Password Check
            if (session.Properties.TryGetValue("Password", out var pass) && pass.PropertyValue != null)
            {
                _roomPassword = pass.PropertyValue.ToString();
            }
        }

        // Toggle Lock Icon UI
        bool isPasswordProtected = !string.IsNullOrEmpty(_roomPassword);
        if (lockIcon != null)
        {
            lockIcon.SetActive(isPasswordProtected);
        }

        // Listeners
        joinBtn.onClick.RemoveAllListeners(); //for safety only
        joinBtn.onClick.AddListener(AskSessionFinderManagerToJoin);
    }

    //Check if we need to enable password_panel first regardless ask the SessionFinderManager.cs for this
    private void AskSessionFinderManagerToJoin()
    {
        //Pass the SessionName to SessionFinderManager.cs to know where to join, NOTE: Session Name is unique per session
        _sessionFinderManagerCS.sessionName = _sessionInfo.Name;

        //PASSWORD CHECK LOGIC HERE
        if (!string.IsNullOrEmpty(_roomPassword))
        {
            _sessionFinderManagerCS.requiredPasswordByRoom = _roomPassword;
            _sessionFinderManagerCS.OpenPasswordPanel();
        }
        else
        {
            _sessionFinderManagerCS.JoinTheIntendedRoom();
        }
    }
}