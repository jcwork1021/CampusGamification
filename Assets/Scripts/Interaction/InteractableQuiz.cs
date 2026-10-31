using UnityEngine;

public class InteractableQuiz : BaseInteractable
{
    [Header("Necessary SO")]
    [SerializeField] QuestionManagerRefSO _questionManagerSO;
    [SerializeField] InteractableQuizRefSO _interactableQuizSO;

    [Header("API")]
    [SerializeField] SOString getQuestionAPI;

    #region GameEvent Overrides
    public override void CallWhenInteracted()
    {
        base.CallWhenInteracted();

        if (_interactableQuizSO != null) _interactableQuizSO.Instance = this;

    }
    #endregion

    protected override void OnLevelRequirementPassed()
    {
        base.OnLevelRequirementPassed();

        _questionManagerSO.Instance.GetQuestionAPIFromServer();
    }
    protected override void OnLevelRequirementFailed()
    {
        base.OnLevelRequirementFailed();

        Debug.Log("Displaying 'Level Too Low' notification UI to player.");
    }


    public override void ExitInteractable()
    {
        base.ExitInteractable();

        Debug.LogWarning("Exited Quiz Podium");
    }

    //NOTE: Exiting this Interactable is in QuizManager.cs
}
