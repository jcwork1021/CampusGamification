using Fusion;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CustomizationManager : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] RememberMeRefSO _rememberMeSO;
    [SerializeField] LobbyManagerRefSO _lobbyManagerSO;
    [SerializeField] CustomizationManagerRefSO _customizationManagerSO;

    [Header("From Heirarchy")]
    [SerializeField] CinemachineCamera _avatarCinemachine;
    [SerializeField] CanvasGroup _customizationCanvas;

    private void Awake()
    {
        _customizationManagerSO.Instance = this;

        //For Safety
        DisableManager();
    }

    private void OnDestroy()
    {
        _customizationManagerSO.Instance = null;
    }

    //This is Added in LevelManager EventListener
    public void EnableManager()
    {
        StartCoroutine(DelayEnableManager());
    }

    IEnumerator DelayEnableManager()
    {
        _lobbyManagerSO.Instance.EnableLobbyCanvas(false);
        _avatarCinemachine.Priority = 10;

        yield return new WaitForSeconds(1);

        _customizationCanvas.alpha = 1;
    }

    public void DisableManager()
    {
        _customizationCanvas.alpha = 0;
        _avatarCinemachine.Priority = 0;
    }

    public void ClickBack()
    {
        DisableManager();
        _lobbyManagerSO.Instance.TransitionToLobby(); //Just do camera transition then after we will unload this scene
    }
}
