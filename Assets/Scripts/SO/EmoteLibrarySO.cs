using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SO/Library/Emotes")]
public class EmoteLibrarySO : ScriptableObject
{
    [Header("Drag All Sliced Emote Sprites Here")]
    [SerializeField] private List<Sprite> emoteSprites = new List<Sprite>();

    // Runtime Fast-Lookup Caches
    private Dictionary<int, Sprite> idToSpriteLookup;
    private Dictionary<string, Sprite> nameToSpriteLookup;

    /// <summary>
    /// Builds fast O(1) dictionaries at runtime based on list indices and file names.
    /// </summary>
    public void Initialize()
    {
        if (idToSpriteLookup != null) return; // Already initialized

        idToSpriteLookup = new Dictionary<int, Sprite>();
        nameToSpriteLookup = new Dictionary<string, Sprite>();

        for (int i = 0; i < emoteSprites.Count; i++)
        {
            Sprite sprite = emoteSprites[i];
            if (sprite == null) continue;

            // 1. Index-based ID lookup
            if (!idToSpriteLookup.ContainsKey(i))
            {
                idToSpriteLookup.Add(i, sprite);
            }

            // 2. Filename-based lookup (e.g., "E1", "heart_emoji")
            if (!nameToSpriteLookup.ContainsKey(sprite.name))
            {
                nameToSpriteLookup.Add(sprite.name, sprite);
            }
        }
    }

    /// <summary>
    /// Get Sprite by Index / Emote ID (Recommended for Multiplayer - 1 to 4 bytes payload)
    /// </summary>
    public Sprite GetEmoteSprite(int id)
    {
        if (idToSpriteLookup == null) Initialize();

        if (idToSpriteLookup.TryGetValue(id, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[EmoteDatabase] Emote ID '{id}' not found!");
        return null;
    }

    /// <summary>
    /// Get Sprite by Filename (e.g., "E1")
    /// </summary>
    public Sprite GetEmoteSprite(string fileName)
    {
        if (nameToSpriteLookup == null) Initialize();

        if (nameToSpriteLookup.TryGetValue(fileName, out Sprite sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[EmoteDatabase] Emote name '{fileName}' not found!");
        return null;
    }

    /// <summary>
    /// Total count of emotes in the database.
    /// </summary>
    public int Count => emoteSprites.Count;
}