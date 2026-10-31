using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Payload;

public class TrueFalseHandler : BaseQuestionUI
{
    [Header("UI References")]
    [SerializeField] Transform _optionsContainer;

    [Header("From Project cuz its prefab")]
    [SerializeField] GameObject _tf_OptionsBtn;

    public override void Setup(QuestionData data, Action<bool, int> onAnswerSubmitted)
    {
        //Set the question text
        questionText.text = data.text;

        // Clean up old option buttons from previous questions
        foreach (Transform child in _optionsContainer)
        {
            Destroy(child.gameObject);
        }

        //Populate Buttons
        for (int i = 0; i < data.options.Count; i++)
        {
            OptionData option = data.options[i];
            GameObject btnObj = Instantiate(_tf_OptionsBtn, _optionsContainer);

            //Set button display text (ex "A.) Option Content")
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            btnText.text = $"{option.content}";

            Button buttonComponent = btnObj.GetComponent<Button>();

            //Give details (isCorrect, points) and invoke callback on click
            buttonComponent.onClick.AddListener(() =>
            {
                //Trigger the callback back to QuestionManager!
                onAnswerSubmitted?.Invoke(option.isCorrect, data.points);
            });
        }
    }
}