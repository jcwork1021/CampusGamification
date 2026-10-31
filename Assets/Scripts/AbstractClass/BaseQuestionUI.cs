using System;
using TMPro;
using UnityEngine;
using static Payload;

public abstract class BaseQuestionUI : MonoBehaviour
{
    [Header("Mandatory UI")]
    public GameObject answerPanel;
    public TMP_Text questionText;
    public abstract void Setup(QuestionData data, Action<bool, int> onAnswerSubmitted);
}