using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotator : MonoBehaviour
{
    [Header("Sensitivity Settings")]
    [SerializeField] private float analogRotationSpeed = 100.0f; // Virtual stick / Gamepad speed
    [SerializeField] private float mouseRotationSpeed = 0.1f;    // Mouse drag sensitivity

    [Header("Inversion Settings")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Vertical Angle Limits")]
    [SerializeField] private float minPitch = -40.0f; // Look down limit
    [SerializeField] private float maxPitch = 70.0f;  // Look up limit

    [Header("Input Options")]
    [SerializeField] private bool requireMouseClickToDrag = true; // Toggle if mouse drag requires right click
    [SerializeField] private InputActionReference lookAction;

    private float currentPitch = 0f;

    private void OnEnable()
    {
        lookAction?.action.Enable();
    }

    private void OnDisable()
    {
        lookAction?.action.Disable();
    }

    private void Start()
    {
        // Cache initial pitch angle to preserve starting rotation
        Vector3 currentAngles = transform.localEulerAngles;
        currentPitch = currentAngles.x > 180f ? currentAngles.x - 360f : currentAngles.x;
    }

    void Update()
    {
        if (lookAction == null) return;

        InputAction action = lookAction.action;
        Vector2 input = action.ReadValue<Vector2>();

        // Ignore deadzone on both axes
        if (input.sqrMagnitude <= 0.0001f) return;

        bool isMouse = action.activeControl?.device is Mouse;

        // If using mouse, require Right Click to drag (unless disabled)
        if (isMouse && requireMouseClickToDrag && !Mouse.current.rightButton.isPressed)
        {
            return;
        }

        // Apply directional inversion
        float dirX = invertX ? 1f : -1f;
        float dirY = invertY ? 1f : -1f;

        float yawAmount = 0f;
        float pitchAmount = 0f;

        if (isMouse)
        {
            // Mouse Delta is frame-rate independent raw pixel movement
            yawAmount = input.x * mouseRotationSpeed * dirX;
            pitchAmount = input.y * mouseRotationSpeed * dirY;
        }
        else
        {
            // Virtual Stick / Gamepad requires Time.deltaTime
            yawAmount = input.x * analogRotationSpeed * Time.deltaTime * dirX;
            pitchAmount = input.y * analogRotationSpeed * Time.deltaTime * dirY;
        }

        // Horizontal Rotation (Yaw - World Y Axis)
        transform.Rotate(Vector3.up, yawAmount, Space.World);

        // Vertical Rotation (Pitch - Clamped Local X Axis)
        currentPitch += pitchAmount;
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        Vector3 currentEuler = transform.localEulerAngles;
        transform.localRotation = Quaternion.Euler(currentPitch, currentEuler.y, 0f);
    }
}