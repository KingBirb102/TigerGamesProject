using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Physics-based character movement + horizontal (yaw) look, using Unity's new Input System.
/// Movement is driven by forces on a Rigidbody; yaw rotation is applied via Rigidbody.MoveRotation
/// inside FixedUpdate so it never fights the physics step.
///
/// Setup:
/// 1. Attach to the player's ROOT GameObject (the one with the Rigidbody + Collider).
/// 2. Put your camera as a CHILD of this object, at head height, with FirstPersonLook on it
///    (FirstPersonLook now only handles vertical pitch — this script owns horizontal yaw).
/// 3. Make sure the Input System package is active (Project Settings > Player > Active Input Handling).
/// 4. Assign a "Ground" layer to walkable surfaces and set groundLayer to match.
/// 5. Create an empty child object at the character's feet, assign it to groundCheck.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float acceleration = 60f;      // how fast we reach target speed
    [SerializeField] private float airAcceleration = 20f;   // reduced control while airborne
    [SerializeField] private float maxSlopeAngle = 45f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float coyoteTime = 0.15f;      // grace period after leaving ground
    [SerializeField] private float jumpBufferTime = 0.15f;  // grace period before landing
    [SerializeField] private float extraGravityMultiplier = 2.5f; // snappier falls

    [Header("Look (Yaw)")]
    [SerializeField] private float mouseSensitivity = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction lookAction;

    private Vector3 moveInput;
    private float yawDelta;
    private Vector3 groundNormal = Vector3.up;
    private bool isGrounded;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool jumpQueued;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Freeze tipping on X/Z but leave Y rotation free — we drive Y ourselves via MoveRotation.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");

        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        jumpAction.performed += OnJumpPerformed;

        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        lookAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
        lookAction.Disable();
    }

    private void OnDestroy()
    {
        jumpAction.performed -= OnJumpPerformed;
        moveAction.Dispose();
        jumpAction.Dispose();
        lookAction.Dispose();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpBufferTimer = jumpBufferTime;
    }

    private void Update()
    {
        // --- Movement input, relative to this object's OWN facing (no Camera.main needed —
        // this object owns yaw, so its forward/right IS the look direction) ---
        Vector2 raw = moveAction.ReadValue<Vector2>();
        Vector3 rawInput = new Vector3(raw.x, 0f, raw.y);
        if (rawInput.sqrMagnitude > 1f) rawInput.Normalize();
        moveInput = transform.forward * rawInput.z + transform.right * rawInput.x;

        // --- Accumulate mouse X for yaw; applied in FixedUpdate via MoveRotation ---
        yawDelta += lookAction.ReadValue<Vector2>().x * mouseSensitivity;

        // --- Jump buffer countdown ---
        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f && (isGrounded || coyoteTimer > 0f))
        {
            jumpQueued = true;
            jumpBufferTimer = 0f;
        }

        // --- Coyote time ---
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        ApplyYaw();
        CheckGround();
        ApplyMovement();
        ApplyExtraGravity();

        if (jumpQueued)
        {
            Jump();
            jumpQueued = false;
        }
    }

    private void ApplyYaw()
    {
        if (yawDelta != 0f)
        {
            Quaternion turn = Quaternion.Euler(0f, yawDelta, 0f);
            rb.MoveRotation(rb.rotation * turn);
            yawDelta = 0f;
        }
    }

    private void CheckGround()
    {
        isGrounded = false;
        groundNormal = Vector3.up;

        Collider[] hits = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, groundLayer);
        foreach (var hit in hits)
        {
            Vector3 closest = hit.ClosestPoint(groundCheck.position);
            Vector3 dir = (groundCheck.position - closest);
            if (dir.sqrMagnitude < 0.0001f) continue;

            float angle = Vector3.Angle(Vector3.up, dir.normalized);
            if (angle <= maxSlopeAngle)
            {
                isGrounded = true;
                groundNormal = dir.normalized;
                break;
            }
        }
    }

    private void ApplyMovement()
    {
        Vector3 targetDir = Vector3.ProjectOnPlane(moveInput, groundNormal).normalized;
        Vector3 targetVelocity = targetDir * moveSpeed;

        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        float accel = isGrounded ? acceleration : airAcceleration;
        Vector3 velocityChange = (targetVelocity - currentHorizontal);
        velocityChange = Vector3.ClampMagnitude(velocityChange, accel * Time.fixedDeltaTime);

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private void ApplyExtraGravity()
    {
        if (!isGrounded && rb.linearVelocity.y < 0f)
        {
            rb.AddForce(Physics.gravity * (extraGravityMultiplier - 1f), ForceMode.Acceleration);
        }
    }

    private void Jump()
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        coyoteTimer = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}