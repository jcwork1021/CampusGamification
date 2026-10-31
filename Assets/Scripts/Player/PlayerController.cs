using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Necessary SO")]
    [SerializeField] PlayerControllerRefSO _playerControllerSO;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintMultiplier = 1.8f;
    [SerializeField] float rotationSpeed = 15f;

    [Header("Control Flags")]
    [SerializeField] bool canMove = true; // Added flag to toggle movement on/off
    public bool CanMove => canMove;

    [Header("Jumping & Gravity")]
    [SerializeField] bool enableFalling;
    [SerializeField] float jumpHeight = 1.5f;
    [SerializeField] float gravityValue = -19.62f;
    [SerializeField] float slopeSnapForce = -15f;

    [Header("Read-Only")]
    [SerializeField] Transform cameraTransform;

    [Header("Animation")]
    [SerializeField] Animator animator;
    [SerializeField] float animationDampTime = 0.1f;
    [SerializeField] float fallAnimationDelay = 0.15f;

    CharacterController controller;
    Vector2 input;
    bool isSprinting;
    bool jumpRequested;
    float verticalVelocity;
    float ungroundedTimer;

    [Header("Debug / State")]
    [SerializeField] bool isGrounded;
    public bool IsGrounded => isGrounded;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int SitHash = Animator.StringToHash("Sit");
    static readonly int JumpHash = Animator.StringToHash("isJump");
    static readonly int IsFallingHash = Animator.StringToHash("isFalling");

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        EnableFalling();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            _playerControllerSO.Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (HasStateAuthority)
        {
            _playerControllerSO.Instance = null;
        }
    }

    public void OnMove(InputValue value)
    {
        if (!canMove) return; // Ignore input if movement is disabled
        input = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        if (!canMove) return;
        isSprinting = value.isPressed;
    }

    public void OnSit(InputValue value)
    {
        if (animator == null || !canMove) return;
        animator.SetBool(SitHash, value.isPressed);
    }

    public void OnJump(InputValue value)
    {
        if (!enableFalling || !canMove) return;
        if (value.isPressed) jumpRequested = true;
    }

    public void ClickJump()
    {
        if (!enableFalling || !canMove) return;
        jumpRequested = true;
    }

    // ==========================================
    // MOVEMENT TOGGLE METHODS
    // ==========================================

    /// <summary>
    /// Call this to freeze the player in place and reset their movement to idle.
    /// </summary>
    public void DisableMovement() //Calls by PlayerManager.cs 
    {
        canMove = false;
        input = Vector2.zero; // Clear current directional input
        isSprinting = false;
        jumpRequested = false;

        {
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, 0f);
            }
        }
    }

    /// <summary>
    /// Call this to restore player control.
    /// </summary>
    public void EnableMovement() //Calls by PlayerManager.cs 
    {
        canMove = true;
    }


    public void EnableFalling()
    {
        enableFalling = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (controller == null || !controller.enabled || cameraTransform == null)
            return;

        Move();
    }

    void Move()
    {
        isGrounded = controller.isGrounded;

        // If movement is disabled, zero out movement vector but keep gravity active
        Vector3 movement = Vector3.zero;

        if (canMove)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            movement = cameraForward * input.y + cameraRight * input.x;

            if (movement.sqrMagnitude > 1f)
                movement.Normalize();
        }

        float targetSpeed = moveSpeed * (isSprinting ? sprintMultiplier : 1f);
        Vector3 velocity = movement * targetSpeed;

        // Jumping & Gravity (Gravity stays active so player doesn't float when movement is disabled)
        if (enableFalling)
        {
            if (isGrounded && verticalVelocity <= 0f)
            {
                verticalVelocity = slopeSnapForce;
            }

            if (jumpRequested && canMove)
            {
                if (isGrounded)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
                    if (animator != null) animator.SetTrigger(JumpHash);
                }
                jumpRequested = false;
            }

            verticalVelocity += gravityValue * Runner.DeltaTime;
            velocity.y = verticalVelocity;
        }
        else
        {
            verticalVelocity = 0f;
            velocity.y = 0f;
            jumpRequested = false;
        }

        // Apply Movement
        controller.Move(velocity * Runner.DeltaTime);

        // Animation
        if (animator != null)
        {
            float currentSpeed = canMove ? movement.magnitude * targetSpeed : 0f;
            animator.SetFloat(SpeedHash, currentSpeed, animationDampTime, Runner.DeltaTime);

            if (isGrounded)
            {
                ungroundedTimer = 0f;
                animator.SetBool(IsFallingHash, false);
            }
            else
            {
                ungroundedTimer += Runner.DeltaTime;
                bool isFalling = enableFalling && verticalVelocity < 0f && ungroundedTimer > fallAnimationDelay;
                animator.SetBool(IsFallingHash, isFalling);
            }
        }

        // Rotation
        if (canMove && movement.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);
        }
    }
}