using UnityEngine;

public class InteractableSession : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] InteractableSessionRefSO _interactableSessionSO;
    [SerializeField] SessionFinderManagerRefSO _sessionFinderManagerSO;

    [Header("Scene/Stage/Map we need to load")]
    [SerializeField] SceneTransitionSO _sceneTransitionSO;

    #region GameEvent Overrides
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();

        _interactableSessionSO.Instance = this;

    }
    #endregion

    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();
        _rememberMeSO.Instance.sceneTransitionSO = _sceneTransitionSO; //Pass this so scene loader knows what scene we want to load and which Receiver we should use

        _sessionFinderManagerSO.Instance.OpenStageBrowserUI();
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
        _interactableSessionSO.Instance = null;
    }


    //NOTE: Exiting this Interactable is in SessionFinderManager.cs in 2 decssions Create and Join
}
