using UnityEngine;

public class InteractableAutoJoin : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractableAutoJoinRefSO _interactableAutoJoinSO;

    [Header("Scene/Stage/Map we need to load")]
    [SerializeField] SceneTransitionSO _sceneTransitionSO;

    #region GameEvent Overrides
    public override void CallWhenInteracted()
    {
        _interactableAutoJoinSO.Instance = this;
    }
    #endregion

    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();

        if(_sceneTransitionSO == null)
        {
            Debug.LogError("Interactable do not have SceneTransitionSO");
            ExitInteractable();
            return;
        }

        //Pass SceneTransitionSO so in LobbyScene Player can auto join new session
        _rememberMeSO.Instance.sceneTransitionSO = _sceneTransitionSO;

        //Ask the InteractableManager which is our Handler to end this Interactable first before doing Transition
        ExitInteractable();

        //Player Should Leave
        _gameManagerSO.Instance.PlayerExitingGameSession();
    }

    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();

        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
    }

    public override void ExitInteractable()
    {
        base.ExitInteractable();

        Debug.Log("Exited stage browser interactable.");
    }
}
