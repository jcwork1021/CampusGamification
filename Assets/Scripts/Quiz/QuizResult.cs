using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class QuizResult : MonoBehaviour
{
    [Header("Reference from Result_Panel")]
    [SerializeField] GameObject _resultPanel;
    [SerializeField] TMP_Text _resultTxt;
    [SerializeField] TMP_Text _scoreTxt;
    [SerializeField] TMP_Text _passingScoreTxt;
    [SerializeField] TMP_Text _attemptTxt;
    [SerializeField] Button _continueBtn;
    [SerializeField] Button _retryBtn;
    [SerializeField] Button _exitBtn;

    public GameEvent onQuizPassed;
    public GameEvent onQuizFailed;

    public UnityEvent OnPassedEvent;
    public UnityEvent OnFailedEvent;

    private void Awake()
    {
        _resultPanel.SetActive(false);
    }

    public void DisplayResult(int score, int passingScore, int attempt, int maxAttempt)
    {
        //Enable and Disable what we need
        _resultPanel.SetActive(true);
        _continueBtn.gameObject.SetActive(false);
        _retryBtn.gameObject.SetActive(false);
        _exitBtn.gameObject.SetActive(false);

        if (score >= passingScore)
        {
            _resultTxt.text = "You Win";
            Passed();
        }
        else
        {
            _resultTxt.text = "You Lose";
            Failed();
        }

        _scoreTxt.text = score.ToString();
        _passingScoreTxt.text = $"Passing Score: {passingScore}";
        _attemptTxt.text = $"Attempt: {attempt}/{maxAttempt}";
    }

    public void Passed()
    {
        _continueBtn.gameObject.SetActive(true);

        onQuizPassed.Raise();
        OnPassedEvent?.Invoke();
    }

    public void Failed()
    {
        _retryBtn.gameObject.SetActive(true);
        _exitBtn.gameObject.SetActive(true);

        onQuizFailed.Raise();
        OnFailedEvent?.Invoke();
    }

    #region button Onclicks

    #endregion
}
