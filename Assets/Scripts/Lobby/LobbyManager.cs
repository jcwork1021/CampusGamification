using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;


public class LobbyManager : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] LobbyManagerRefSO _lobbyManagerSO;
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] CustomizationManagerRefSO _customizationManagerSO;
    [SerializeField] NetworkAutomaticJoinRefSO _networkAutomaticJoinSO;
    [SerializeField] ChatManagerRefSO _chatManagerSO;

    [Header("From Heirarchy")]
    [SerializeField] CinemachineCamera _lobbyCinemachine;

    [Header("UI Panels")]
    [SerializeField] CanvasGroup _lobbyCanvas;

    #region Unity Function
    private void Awake()
    {
        _lobbyManagerSO.Instance = this;

        if (_rememberMeSO.Instance.timedOutError)
        {
            _rememberMeSO.Instance.ToggleLoadingPanel(false);
            _rememberMeSO.Instance.EnableWarning("Timed-Out due to Inactivity");
            _rememberMeSO.Instance.timedOutError = false;
            return;
        }


        if (_rememberMeSO.Instance.disconnectedServerError)
        {
            _rememberMeSO.Instance.ToggleLoadingPanel(false);
            _rememberMeSO.Instance.EnableWarning("Disonnected from Server");
            _rememberMeSO.Instance.disconnectedServerError = false;
            return;
        }


        //because if either of this 2 is true  then we can not disable the loading since it will automatically create or join another session
        if (_rememberMeSO.Instance.sceneTransitionSO != null)
        {
            return;
        }

        _rememberMeSO.Instance.ToggleRememberMeCanvas(false);

        if (_rememberMeSO.Instance.kicked)
        {
            _rememberMeSO.Instance.EnableWarning("You have been kicked");
        }
    }

    private void OnDestroy()
    {
        _lobbyManagerSO.Instance = null;
    }

    private async void Start()
    {
        _chatManagerSO.Instance.SelectGlobalTab();
        _chatManagerSO.Instance.UnsubscribeToPrevChannel();

        try
        {
            CharacterSaveData savedData = await _rememberMeSO.Instance.LoadAvatarClothesDataAsync();

            if (savedData == null)
            {
                Debug.LogWarning("[Start] Failed to load avatar clothes data!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Start] Exception thrown during execution: {ex}");
        }
    }

    #endregion

    #region Function other script can be called
    public void TransitionToLobby() //Called by CustomizationManager.cs
    {
        StartCoroutine(DelayTransitionToLobby());
    }

    IEnumerator DelayTransitionToLobby()
    {
        _lobbyCinemachine.Priority = 10;
        yield return new WaitForSeconds(2);
        EnableLobbyCanvas(true);
        _rememberMeSO.Instance.UnloadScene(_rememberMeSO.Instance.customizationScenePath, null);
    }

    #endregion

    #region UI Button Handlers

    public void ClickPlay()
    {
        _networkAutomaticJoinSO.Instance.AutoJoin();
    }

    public void ClickCustomization()
    {
        _lobbyCinemachine.Priority = 0; //give way to the cinemachine of costumizaion
        _rememberMeSO.Instance.LoadSceneAdditive(_rememberMeSO.Instance.customizationScenePath, null);
    }

    #endregion

    #region Reusable Function

    public void EnableLobbyCanvas(bool yeah)
    {
        if (yeah)
        {
            _lobbyCanvas.alpha = 1;
            _lobbyCanvas.blocksRaycasts = yeah;
        }
        else
        {
            _lobbyCanvas.alpha = 0;
            _lobbyCanvas.blocksRaycasts = yeah;
        }
    }

    #endregion
}