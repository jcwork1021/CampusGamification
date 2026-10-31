using System.Collections.Generic;
using UnityEngine;

public class CameraDitherOccluder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Fills via script of the Gamemanager.cs after instantiating the player")]
    public Transform playerTarget;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Dither Settings")]
    [Range(0f, 1f)]
    [Tooltip("Target opacity when occluded (0 = invisible, 1 = fully solid)")]
    [SerializeField] private float fadedOpacity = 0.25f;
    [SerializeField] private float fadeSpeed = 8f;

    // Shader Graph Property Reference (Ensure this matches the internal 'Reference' field in Shader Graph)
    private static readonly int DitherFadePropertyId = Shader.PropertyToID("_Opacity");

    private readonly List<Renderer> activeOccluders = new List<Renderer>();
    private readonly Dictionary<Renderer, float> currentOpacities = new Dictionary<Renderer, float>();
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (!playerTarget) return;

        Vector3 direction = playerTarget.position - transform.position;
        float distance = Vector3.Distance(transform.position, playerTarget.position);

        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, distance, obstacleLayer);
        HashSet<Renderer> currentHits = new HashSet<Renderer>();

        foreach (RaycastHit hit in hits)
        {
            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                currentHits.Add(rend);

                if (!activeOccluders.Contains(rend))
                {
                    activeOccluders.Add(rend);
                    if (!currentOpacities.ContainsKey(rend))
                    {
                        currentOpacities.Add(rend, 1.0f);
                    }
                }
            }
        }

        // Animate fade values on active wall materials
        for (int i = activeOccluders.Count - 1; i >= 0; i--)
        {
            Renderer rend = activeOccluders[i];

            if (rend == null)
            {
                activeOccluders.RemoveAt(i);
                currentOpacities.Remove(rend);
                continue;
            }

            float targetOpacity = currentHits.Contains(rend) ? fadedOpacity : 1.0f;
            bool isFinished = AdjustWallFade(rend, targetOpacity);

            if (isFinished && targetOpacity == 1.0f)
            {
                activeOccluders.RemoveAt(i);
                currentOpacities.Remove(rend);
            }
        }
    }

    private bool AdjustWallFade(Renderer rend, float targetOpacity)
    {
        if (!currentOpacities.TryGetValue(rend, out float currentOpacity))
        {
            currentOpacity = 1.0f;
        }

        float newOpacity = Mathf.MoveTowards(currentOpacity, targetOpacity, Time.deltaTime * fadeSpeed);
        currentOpacities[rend] = newOpacity;

        // Apply to renderer using MaterialPropertyBlock without instantiating materials
        rend.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(DitherFadePropertyId, newOpacity);
        rend.SetPropertyBlock(propertyBlock);

        return Mathf.Approximately(newOpacity, targetOpacity);
    }
}