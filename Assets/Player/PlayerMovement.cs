using UnityEngine;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;

public class MovementOhYa : MonoBehaviour
{
    //NOTE: IF / WHEN I DO ANIMATIONS FOR MY DUDE, GO TO THIS VIDEO AND SEE STEP #9
    [Header("Camera")]
    public Camera myCamera;
    public Vector3 cameraOffset = new Vector3(0f, 0.7f, 0f);
    [Range(0, 1)]public float mouseSensitivity = 3f;
    public float maxLookPitch = 80f;

    [Header("Movement")]
    public float walkAccel = 5f;

    [Header("Jumping")]
    public float raycastDist = 1.1f;
    public float jumpForce;

    [Header("Movement Physics Stuffs")]
    public float moveDragCoeff = .95f;
    public float abilityDragCoeff = .80f;
    public float maxSpeed = 25f;

    [Header("Debug Different Movement Vectors")]
    [SerializeField] private Vector3 moveVel;
    [SerializeField] public Vector3 abilityVel;

    [Header("Debug stuffs bc code is stoopid")]
    [SerializeField] private Vector2 mousePosForCamera;
    [SerializeField] private Vector3 inputDir;
    
    [Header("Private Debug")]
    private Rigidbody _rb;
    private bool jumpNextPhysicsFrame = false;
    private LayerMask layerMask;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;

    [Header("PUBLIC FOR OTHER SCRIPTS TO INTERACT")]
    [SerializeField] public bool applyDrag = true;
    [SerializeField] public bool isGrounded = false;
    [SerializeField] public bool canMove = true;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;

        moveAction = InputSystem.actions.FindAction("Player/Move");
        lookAction = InputSystem.actions.FindAction("Player/Look");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");

        // Define mask
        string[] excludedLayers = { "Ignore Raycast", "Player", "UI", "RaycastOnlyHitbox", "PlayerTeamUndef", "PlayerTeam0", "PlayerTeam1", "PlayerTeam2", "PlayerTeam3" };
        layerMask = ~LayerMask.GetMask(excludedLayers);

        // Safety check for camera
        if (myCamera == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if(camObj != null) myCamera = camObj.GetComponent<Camera>();
        }

        if (myCamera != null)
        {
            myCamera.transform.SetParent(transform, false);
            myCamera.transform.localPosition = cameraOffset;
            myCamera.transform.localRotation = Quaternion.identity;
        }
    }

    void Update() 
    {
        HandleFirstPersonCamera();
        CheckForJump();
        inputDir = GetInputVector();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleJumping();
        ApplyDrag();

        Vector3 moveVelEdited = moveVel;
        moveVelEdited.y = _rb.linearVelocity.y;
        _rb.linearVelocity = moveVelEdited + abilityVel;
    }













    #region Camera Logic
    void HandleFirstPersonCamera()
    {
        if (myCamera == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
            if (camObj != null) myCamera = camObj.GetComponent<Camera>();
            if (myCamera == null) return;
        }

        Vector2 lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        mousePosForCamera.x += lookInput.x * mouseSensitivity;
        mousePosForCamera.y -= lookInput.y * mouseSensitivity;
        mousePosForCamera.y = Mathf.Clamp(mousePosForCamera.y, -maxLookPitch, maxLookPitch);

        transform.rotation = Quaternion.Euler(0f, mousePosForCamera.x, 0f);
        myCamera.transform.localRotation = Quaternion.Euler(mousePosForCamera.y, 0f, 0f);
        myCamera.transform.localPosition = cameraOffset;
    }
    #endregion

    #region Movement Logic
    void HandleMovement()
    {
        if (!canMove) return;

        bool isMoving = inputDir.sqrMagnitude > 0.01f;

        if (isMoving)
        {
            // Normalize input to prevent diagonal speed boost
            Vector3 moveInput = inputDir.normalized;

            // Align to camera yaw so movement feels like a first-person controller
            Vector3 moveDir = AlignInputToCamera(moveInput);

            Vector3 targetVelocity = moveDir * maxSpeed;
            targetVelocity.y = _rb.linearVelocity.y;

            // Smoothly move toward the target velocity instead of clamping
            moveVel = Vector3.Lerp(_rb.linearVelocity, 
                                    targetVelocity, 
                                    walkAccel * Time.fixedDeltaTime);
        }
    }

    Vector3 GetInputVector()
    {
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 vec = new Vector3(moveInput.x, 0f, moveInput.y);
        return vec;
    }

    Vector3 AlignInputToCamera(Vector3 input)
    {
        Vector3 camFwd = myCamera.transform.forward;
        Vector3 camRight = myCamera.transform.right;

        camFwd.y = 0;
        camRight.y = 0;
        camFwd.Normalize();
        camRight.Normalize();

        return (camFwd * input.z) + (camRight * input.x);
    }
    #endregion

    #region Jump Logic
    void CheckForJump()
    {
        if (jumpAction != null && jumpAction.WasPressedThisFrame())
        {
            jumpNextPhysicsFrame = true;
        }
    }

    void HandleJumping()
    {
        isGrounded = CheckIsGrounded();
        if (jumpNextPhysicsFrame && isGrounded)
        {
            _rb.AddForce(0, jumpForce, 0);
            jumpNextPhysicsFrame = false;
        }
    }

    bool CheckIsGrounded()
    {
        /*
        origin	The starting point of the ray in world coordinates.
        direction	The direction of the ray.
        maxDistance	The max distance the ray should check for collisions.
        layerMask	A Layer mask that is used to selectively filter which colliders are considered when casting a ray.
        */
        RaycastHit hit;
        return Physics.Raycast(transform.position, Vector3.down, out hit, raycastDist, layerMask, QueryTriggerInteraction.Ignore);
    }
    #endregion

    #region Physics Logic
    void ApplyDrag()
    {
        moveVel.x *= moveDragCoeff;
        moveVel.z *= moveDragCoeff;

        moveVel.x = (Mathf.Abs(moveVel.x) <= .05f) ? 0 : moveVel.x;
        moveVel.z = (Mathf.Abs(moveVel.z) <= .05f) ? 0 : moveVel.z;



        abilityVel.x *= abilityDragCoeff;
        abilityVel.z *= abilityDragCoeff;

        abilityVel.x = (Mathf.Abs(abilityVel.x) <= .05f) ? 0 : abilityVel.x;
        abilityVel.z = (Mathf.Abs(abilityVel.z) <= .05f) ? 0 : abilityVel.z;
    }
    #endregion








    #region Ability Functions
    public void Ability_AddForce(float x, float y, float z)
    {
        Ability_AddForce(new Vector3(x, y, z));
    }

    public void Ability_AddForce(Vector3 input)
    {
        _rb.AddForce(0, input.y * 400, 0);

        abilityVel.x += input.x;
        abilityVel.z += input.z;
    }

    #endregion
}