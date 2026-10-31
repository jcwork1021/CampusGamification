using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmoteManager : MonoBehaviour
{
    private const string RECENT_EMOTES_KEY = "Player_Recent_Emotes";
    private const int MAX_RECENTS = 6;

    [Header("Database & Target References")]
    [SerializeField] EmoteLibrarySO _emoteLibrarySO;
    [SerializeField] PlayerEmoteRefSO _playerEmoteSO; // Reference to local player's handler

    [Header("Reference from Emote_Panel")]
    [SerializeField] GameObject _emoteBtn;
    [SerializeField] CanvasGroup _emotePanel;
    [SerializeField] Transform _recentContent;
    [SerializeField] Transform _emojiContent;
    [SerializeField] CanvasGroup _coolDownPanel;

    [Header("Reference from Project")]
    [SerializeField] GameObject _emoteBtnPrefab; // Prefab containing a Button & child Image

    private List<int> _recentEmoteIDs = new List<int>();

    #region Unity Functions
    private void Start()
    {
        _emoteLibrarySO.Initialize();

        LoadRecentEmotes();
        GenerateAllEmoteButtons();
        RefreshRecentEmoteButtons();

        //Default Disable Panels
        TogglePanel(_coolDownPanel, false);
        TogglePanel(_emotePanel, false);

        //Disabling the Emote Panel is handled by GameManager.cs
    }
    #endregion

    #region Emote Functions

    /// <summary>
    /// Generates the full list of emojis in the main grid container.
    /// </summary>
    public void GenerateAllEmoteButtons()
    {
        if (_emoteLibrarySO == null || _emoteBtnPrefab == null || _emojiContent == null)
        {
            Debug.LogError("[EmoteGridUI] Missing required references in Inspector!");
            return;
        }

        // Clear existing children
        foreach (Transform child in _emojiContent)
        {
            Destroy(child.gameObject);
        }

        int totalEmotes = _emoteLibrarySO.Count;
        for (int i = 0; i < totalEmotes; i++)
        {
            int emoteID = i;
            Sprite emoteSprite = _emoteLibrarySO.GetEmoteSprite(emoteID);

            if (emoteSprite == null) continue;

            CreateEmoteButton(emoteID, emoteSprite, _emojiContent);
        }
    }

    /// <summary>
    /// Clears and re-spawns the Recents container buttons based on saved IDs.
    /// </summary>
    private void RefreshRecentEmoteButtons()
    {
        if (_recentContent == null) return;

        // Clear current recent buttons
        foreach (Transform child in _recentContent)
        {
            Destroy(child.gameObject);
        }

        // Spawn buttons for stored IDs
        for (int i = 0; i < _recentEmoteIDs.Count; i++)
        {
            int emoteID = _recentEmoteIDs[i];
            Sprite emoteSprite = _emoteLibrarySO.GetEmoteSprite(emoteID);

            if (emoteSprite == null) continue;

            CreateEmoteButton(emoteID, emoteSprite, _recentContent);
        }
    }

    /// <summary>
    /// Helper to instantiate a button and assign the exact Emote ID to its click listener.
    /// </summary>
    private void CreateEmoteButton(int emoteID, Sprite sprite, Transform parent)
    {
        GameObject buttonObj = Instantiate(_emoteBtnPrefab, parent);

        // Assign Icon
        Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage == null) iconImage = buttonObj.GetComponent<Image>();

        if (iconImage != null)
        {
            iconImage.sprite = sprite;
        }

        // Setup OnClick using the actual emoteID (NOT grid loop index)
        Button buttonComponent = buttonObj.GetComponent<Button>();
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveAllListeners();
            buttonComponent.onClick.AddListener(() => OnEmoteButtonClicked(emoteID));
        }
    }

    private void OnEmoteButtonClicked(int id)
    {
        // 1. Trigger Emote on Local Player
        if (_playerEmoteSO != null && _playerEmoteSO.Instance != null)
        {
            _playerEmoteSO.Instance.ShowEmoteByID(id);

            //Lock the Emote
            StartCoroutine(LockEmoji());
        }
        else
        {
            Debug.LogWarning($"[EmoteGridUI] Clicked Emote ID {id}, but local player instance is missing!");
        }

        // 2. Register Emote in Recents list
        AddEmoteToRecents(id);
    }

    IEnumerator LockEmoji()
    {
        TogglePanel(_coolDownPanel, true);
        _emotePanel.interactable = false;
        yield return new WaitForSeconds(3);
        TogglePanel(_coolDownPanel, false);
        _emotePanel.interactable = true;
    }

    private void AddEmoteToRecents(int id)
    {
        // Remove duplicate if it already exists in recents (brings it to the front)
        if (_recentEmoteIDs.Contains(id))
        {
            _recentEmoteIDs.Remove(id);
        }

        // Add to index 0 (Most recent)
        _recentEmoteIDs.Insert(0, id);

        // Cap size at 7 max items
        if (_recentEmoteIDs.Count > MAX_RECENTS)
        {
            _recentEmoteIDs.RemoveAt(_recentEmoteIDs.Count - 1);
        }

        // Save & Refresh UI
        SaveRecentEmotes();
        RefreshRecentEmoteButtons();
    }
    #endregion

    #region PlayerPrefs Persistence

    private void SaveRecentEmotes()
    {
        // Convert list of ints to a comma-separated string (e.g. "5,12,0")
        string savedData = string.Join(",", _recentEmoteIDs);
        PlayerPrefs.SetString(RECENT_EMOTES_KEY, savedData);
        PlayerPrefs.Save();
    }

    private void LoadRecentEmotes()
    {
        _recentEmoteIDs.Clear();

        if (!PlayerPrefs.HasKey(RECENT_EMOTES_KEY)) return;

        string savedData = PlayerPrefs.GetString(RECENT_EMOTES_KEY, "");
        if (string.IsNullOrEmpty(savedData)) return;

        string[] splitIDs = savedData.Split(',');
        foreach (string idString in splitIDs)
        {
            if (int.TryParse(idString, out int parsedID))
            {
                // Verify ID still exists in current library before adding
                if (_emoteLibrarySO.GetEmoteSprite(parsedID) != null)
                {
                    _recentEmoteIDs.Add(parsedID);
                }
            }
        }
    }

    #endregion

    #region Toggle

    public void ToggleEmotePanel(bool visible)
    {
        TogglePanel(_emotePanel, visible);
    }

    private void TogglePanel(CanvasGroup cg, bool visible)
    {
        // 0 = no display, 1 = fully visible
        cg.alpha = visible ? 1f : 0f;
        // Prevents interacting with buttons on this panel when false
        cg.interactable = visible;
        // Stops this panel from blocking clicks to elements/gameplay behind it when false
        cg.blocksRaycasts = visible;
    }
    #endregion
}