using System;
using System.Collections.Generic;
using UnityEngine;

public class Payload : MonoBehaviour
{
    [Serializable]
    public class SaveAvatarRequest
    {
        public int player_id;
        public CharacterSaveData avatarDetails; // Your existing customization class
    }

    [Serializable]
    public class SaveAvatarResponse
    {
        public string status;
        public string message;
    }

    // 1. Inner character customization data
    [Serializable]
    public class CharacterSaveData
    {
        public List<string> categories = new List<string>();
        public List<string> equippedPaths = new List<string>();
    }

    // 2. Main Network Payload Wrapper
    [Serializable]
    public class PlayerPayload
    {
        public int playerID;
        public CharacterSaveData Character; // Matches the key name in the JSON
    }

    [Serializable]
    public class QuizPayload
    {
        public string id;
        public string title;
        public string description;
        public int pointsReward;
        public string timeType;
        public int timeLimitMinutes;
        public int passingScore;
        public int attemptLimit;
        public bool shuffleQuestions;
        public bool shuffleOptions;
        public bool showAnswersAfterCompletion;
        public bool isPublished;
        public int questionCount;
        public List<QuestionData> questions;
    }

    [Serializable]
    public class QuestionData
    {
        public string id;
        public string type;
        public string text;
        public int points;
        public string explanation;
        public List<OptionData> options;
    }

    [Serializable]
    public class OptionData
    {
        public string id;
        public string content;
        public bool isCorrect;
    }
}
