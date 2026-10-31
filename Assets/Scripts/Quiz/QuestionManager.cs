using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using static Payload;

public class QuestionManager : MonoBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] QuestionManagerRefSO _questionManagerSO;
    [SerializeField] InteractableQuizRefSO _interactableQuizSO;
    [SerializeField] InteractableRefSO _interactableSO;

    [Header("Player SO")]
    [SerializeField] SOInteractor _playerInteractorSO;

    [Header("Reference from Itself")]
    [SerializeField] QuizResult _quizResultCS;

    [Header("Reference from its child")]
    [SerializeField] CinemachineCamera _gameCinemachine;
    [SerializeField] GameObject _gameCanvas;

    [Header("Reference from Question_Panel")]
    [SerializeField] GameObject _questionPanel;
    [SerializeField] TMP_Text _titleTxt;
    [SerializeField] TMP_Text _descriptionTxt;
    [SerializeField] TMP_Text _timeLimitTxt;
    [SerializeField] TMP_Text _attemptTxt;

    [Header("Question UI Handlers")]
    [SerializeField] BaseQuestionUI _multipleChoiceHandler;
    [SerializeField] BaseQuestionUI _trueFalseHandler;

    [Header("Just for debugging")]
    [TextArea(5, 10)]
    public string jsonToDeserialize;

    [Header("Read-Only State")]
    public bool doingExam; //Calls by other script
    [SerializeField] int _earnedScore = 0;
    [SerializeField] int _passingScore = 0;
    [SerializeField] int _attempt = 0;
    [SerializeField] int _attemptLimit = 0;
    [SerializeField] int _currentQuestionIndex = 0;
    [SerializeField] float _timeRemainingSeconds = 0f;
    [SerializeField] bool _isTimerRunning = false;

    // Active Quiz Session State
    QuizPayload _currentQuiz;
    Dictionary<string, BaseQuestionUI> _handlerMap;

    void Awake()
    {
        _questionManagerSO.Instance = this;

        // Map question types (from JSON) to their respective UI handlers
        _handlerMap = new Dictionary<string, BaseQuestionUI>
        {
            { "MULTIPLE_CHOICE", _multipleChoiceHandler },
            { "TRUE_FALSE", _trueFalseHandler }
        };

        EnableGameCanvas(false); // Safety
    }

    void OnDestroy()
    {
        _questionManagerSO.Instance = null;
    }

    void Start()
    {

    }

    void Update()
    {
        HandleTimer();
    }

    public void FakeJson()
    {
        Debug.Log("Fake JSON");
        // Debugging test: uncomment to test with editor string
        if (!string.IsNullOrEmpty(jsonToDeserialize))
        {
            DeserializeJSON(jsonToDeserialize);
        }
    }

    /// <summary>
    /// Handles the real-time countdown clock.
    /// </summary>
    void HandleTimer()
    {
        if (!_isTimerRunning) return;

        if (_timeRemainingSeconds > 0f)
        {
            _timeRemainingSeconds -= Time.deltaTime;
            UpdateTimerUI(_timeRemainingSeconds);
        }
        else
        {
            // Time expired!
            _timeRemainingSeconds = 0f;
            _isTimerRunning = false;
            UpdateTimerUI(0f);

            Debug.LogWarning("[QuestionManager] Time run out! Completing quiz.");
            CompleteQuiz();
        }
    }

    void UpdateTimerUI(float totalSeconds)
    {
        if (_timeLimitTxt == null) return;

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        _timeLimitTxt.text = $"{minutes:00}:{seconds:00}";
    }

    public void GetQuestionAPIFromServer()
    {
        //This function should Fetch API from server to get the list of question API and filter them out, filter them by classroom? or grade class? we will know in the future

        //Since we filtered out the API we need lets now Get the question inside of that API 
        GetJsonFromAPI("");
    }

    private void GetJsonFromAPI(string api)
    {
        //Process to Get the JSON Question from API after we have it lets deserialize it 
        //DeserializeJSON("");

        //For now lets just test it first
        FakeJson();
    }

    /// <summary>
    /// Parses incoming JSON string and initializes the quiz.
    /// </summary>
    private void DeserializeJSON(string jsonToConvert)
    {
        if (string.IsNullOrEmpty(jsonToConvert))
        {
            Debug.LogError("[QuestionManager] Empty JSON payload string!");
            return;
        }

        _currentQuiz = JsonUtility.FromJson<QuizPayload>(jsonToConvert);

        if (_currentQuiz == null || _currentQuiz.questions == null || _currentQuiz.questions.Count == 0)
        {
            Debug.LogError("[QuestionManager] Failed to parse quiz data or no questions found.");
            return;
        }

        // Reset state
        _currentQuestionIndex = 0;
        _earnedScore = 0;
        _passingScore = _currentQuiz.passingScore;
        _attempt = 0;
        _attemptLimit = _currentQuiz.attemptLimit;

        //Reset UI
        _questionPanel.SetActive(true);

        // Shuffle questions if requested
        if (_currentQuiz.shuffleQuestions)
        {
            ShuffleList(_currentQuiz.questions);
        }

        // Optionally shuffle options within each question if specified
        if (_currentQuiz.shuffleOptions)
        {
            foreach (var q in _currentQuiz.questions)
            {
                if (q.options != null && q.options.Count > 0)
                {
                    ShuffleList(q.options);
                }
            }
        }

        // Setup Header UI
        if (_titleTxt != null) _titleTxt.text = _currentQuiz.title;
        if (_descriptionTxt != null) _descriptionTxt.text = _currentQuiz.description;

        // Initialize Timer
        _timeRemainingSeconds = _currentQuiz.timeLimitMinutes * 60f;
        UpdateTimerUI(_timeRemainingSeconds);

        // Show Canvas & Start Quiz
        EnableGameCanvas(true);
        CountAttempt(); // Check attempt first, if valid -> StartDisplayingQuestion()
    }

    void CountAttempt()
    {
        _attempt += 1;

        // Fixed attempt condition logic:
        if (_attempt > _currentQuiz.attemptLimit)
        {
            Debug.LogWarning($"Attempt Exceeded {_attempt}/{_currentQuiz.attemptLimit}");
            GameExit();
            return;
        }

        if (_attemptTxt != null) _attemptTxt.text = $"{_attempt}/{_currentQuiz.attemptLimit}";

        // Start Timer & Begin Quiz
        _isTimerRunning = true;
        StartDisplayingQuestion();
    }

    /// <summary>
    /// Displays the question at _currentQuestionIndex.
    /// </summary>
    void StartDisplayingQuestion()
    {
        doingExam = true; //Mark that the player is doing exam

        if (_currentQuestionIndex >= _currentQuiz.questions.Count)
        {
            CompleteQuiz();
            return;
        }

        QuestionData currentQuestion = _currentQuiz.questions[_currentQuestionIndex];

        // Hide all option handlers first
        HideAllQuestionHandlers();

        // Show and setup the correct handler
        if (_handlerMap.TryGetValue(currentQuestion.type, out BaseQuestionUI handler))
        {
            handler.gameObject.SetActive(true);
            handler.Setup(currentQuestion, OnOptionSelected);
        }
        else
        {
            Debug.LogError($"[QuestionManager] Unsupported question type: {currentQuestion.type}");
        }
    }

    /// <summary>
    /// Callback passed to question handlers when an option is picked.
    /// </summary>
    void OnOptionSelected(bool isCorrect, int points)
    {
        if (isCorrect)
        {
            _earnedScore += points;
            Debug.Log($"Correct! Earned {points} pts. Total: {_earnedScore}");
        }
        else
        {
            Debug.Log("Incorrect answer.");
        }

        // Move to next question after answering
        NextQuestion();
    }

    public void NextQuestion()
    {
        _currentQuestionIndex++;
        StartDisplayingQuestion();
    }

    void HideAllQuestionHandlers()
    {
        foreach (var handler in _handlerMap.Values)
        {
            if (handler != null)
                handler.gameObject.SetActive(false);
        }
    }

    void CompleteQuiz()
    {
        _isTimerRunning = false; // Stop timer on completion
        HideAllQuestionHandlers();

        if (_quizResultCS != null)
        {
            _questionPanel.SetActive(false);
            _quizResultCS.DisplayResult(_earnedScore, _passingScore, _attempt, _attemptLimit);
        }
    }

    public void EnableGameCanvas(bool yeah)
    {
        /*
        _gameCanvas.alpha = yeah ? 1f : 0f;
        _gameCanvas.interactable = yeah;
        _gameCanvas.blocksRaycasts = yeah;
        */


        _gameCanvas.gameObject.SetActive(yeah);
        Debug.Log("Enable Question Canvas");
    }

    //Calls in CountAttempt and Exit_Btn
    public void GameExit()
    {
        _isTimerRunning = false;
        EnableGameCanvas(false);

        if (_gameCinemachine != null)
            _gameCinemachine.Priority = 0;

        doingExam = false; //Mark that the player is done with exam

        if (_interactableSO.Instance != null)
        {
            _interactableSO.Instance.ToggleSecondInteractable(false); //Mark in Interactable that we exited 
        }
        else
        {
            Debug.LogWarning("No Interactable");
        }
    }

    /// <summary>
    /// Generic Fisher-Yates shuffle algorithm to randomize lists without duplicate indices.
    /// </summary>
    void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}