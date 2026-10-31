using UnityEngine;


[CreateAssetMenu(menuName = "SO/ReactAction2/Quiz")]
public class QuizReactAction2 : InteractorUIReactActions
{
    [SerializeField] QuestionManagerRefSO _questionManagerSO;

    public override void InitializeUIAsset()
    {
        // Implement any UI initialization if required by the base class, 
        // otherwise leave blank like the GameEvent example.
    }

    public override void InteractorReact(Interactable interactable)
    {
        _questionManagerSO.Instance.GetQuestionAPIFromServer(); //This is Direct
    }
}
