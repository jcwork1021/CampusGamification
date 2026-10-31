using UnityEngine;

public class BlobShadowTracker : MonoBehaviour
{
    public Transform TargetPlayer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastDistance = 5f;
    [SerializeField] private float heightOffset = 0.02f;

    private MeshRenderer meshRenderer;
    private Vector3 initialScale;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        initialScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (TargetPlayer == null) return;

        Vector3 rayOrigin = TargetPlayer.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
        {
            if (!meshRenderer.enabled) meshRenderer.enabled = true;

            transform.position = hit.point + (hit.normal * heightOffset);
            //transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(-90f, 0f, 0f);
            //transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(90f, 0f, 0f);
            transform.rotation = Quaternion.LookRotation(-hit.normal);

            float distanceToGround = hit.distance - 0.5f;
            float alphaFactor = Mathf.Clamp01(1f - (distanceToGround / 3f));
            transform.localScale = initialScale * alphaFactor;
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }
}