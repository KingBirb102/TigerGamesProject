using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Very simple first-person camera pitch (looking up/down).
/// Horizontal turning (yaw) now lives in PhysicsMovementController, applied via
/// Rigidbody.MoveRotation in FixedUpdate so it doesn't fight the physics step.
/// This script only tilts the camera itself — no rigidbody involved, so plain
/// Transform rotation here is perfectly safe.
///
/// Setup:
/// 1. Put the camera under the player as a child (e.g. Player > CameraHolder > Main Camera),
///    positioned at head height.
/// 2. Attach this script to the camera.
/// </summary>
public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private bool lockCursor = true;

    private InputAction lookAction;
    private float pitch;

    private void Awake()
    {
        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
    }

    private void OnEnable()
    {
        lookAction.Enable();
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        lookAction.Disable();
    }

    private void OnDestroy()
    {
        lookAction.Dispose();
    }

    private void Update()
    {
        float deltaY = lookAction.ReadValue<Vector2>().y * mouseSensitivity;

        pitch -= deltaY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}