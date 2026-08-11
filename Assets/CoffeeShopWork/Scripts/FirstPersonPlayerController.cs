using UnityEngine;
using UnityEngine.InputSystem;

namespace CoffeeShop
{
    public sealed class FirstPersonPlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float gravity = -20f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float lookSensitivity = 0.08f;
        [SerializeField, Min(0f)] private float touchLookSensitivity = 0.14f;
        [SerializeField, Range(1f, 89f)] private float maxLookAngle = 85f;
        [SerializeField] private bool lockCursorOnStart = true;

        private CharacterController characterController;
        private float verticalVelocity;
        private float cameraPitch;
        private bool cursorLocked;
        private bool gameplayInputEnabled = true;

        public bool GameplayInputEnabled => gameplayInputEnabled;
        public bool IsCursorLocked => cursorLocked;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera == null)
            {
                Debug.LogError("FirstPersonPlayerController requires a child Camera.", this);
                return;
            }

            cameraPitch = playerCamera.transform.localEulerAngles.x;
            if (cameraPitch > 180f)
            {
                cameraPitch -= 360f;
            }
        }

        private void Start()
        {
            if (lockCursorOnStart && !PlatformSupport.IsTouchDevice)
            {
                LockCursor();
            }
            else if (PlatformSupport.IsTouchDevice)
            {
                UnlockCursor();
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (!gameplayInputEnabled ||
                (GameSessionManager.Instance != null && GameSessionManager.Instance.IsPaused))
            {
                if (cursorLocked)
                {
                    UnlockCursor();
                }

                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (PlatformSupport.IsTouchDevice)
            {
                if (cursorLocked)
                {
                    UnlockCursor();
                }

                Cursor.visible = false;
                HandleLookDelta(MobileControlsUI.ConsumeLookDelta(), touchLookSensitivity);
                HandleMovement(keyboard, MobileControlsUI.MoveInput);
                return;
            }

            if (!cursorLocked && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                LockCursor();
            }

            if (cursorLocked)
            {
                HandleLook(mouse);
            }

            HandleMovement(keyboard, Vector2.zero);
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            gameplayInputEnabled = enabled;

            if (enabled && !PlatformSupport.IsTouchDevice)
            {
                LockCursor();
            }
            else
            {
                UnlockCursor();
                if (PlatformSupport.IsTouchDevice)
                {
                    Cursor.visible = false;
                }
            }
        }

        private void HandleMovement(Keyboard keyboard, Vector2 additionalInput)
        {
            Vector2 moveInput = additionalInput;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed)
                {
                    moveInput.y += 1f;
                }

                if (keyboard.sKey.isPressed)
                {
                    moveInput.y -= 1f;
                }

                if (keyboard.dKey.isPressed)
                {
                    moveInput.x += 1f;
                }

                if (keyboard.aKey.isPressed)
                {
                    moveInput.x -= 1f;
                }
            }

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);

            Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * moveSpeed;
            velocity.y = verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleLook(Mouse mouse)
        {
            if (mouse == null || playerCamera == null)
            {
                return;
            }

            Vector2 lookDelta = mouse.delta.ReadValue();
            HandleLookDelta(lookDelta, lookSensitivity);
        }

        private void HandleLookDelta(Vector2 lookDelta, float sensitivity)
        {
            if (playerCamera == null || lookDelta.sqrMagnitude <= 0f)
            {
                return;
            }

            transform.Rotate(Vector3.up, lookDelta.x * sensitivity);

            cameraPitch = Mathf.Clamp(
                cameraPitch - lookDelta.y * sensitivity,
                -maxLookAngle,
                maxLookAngle);

            playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void LockCursor()
        {
            cursorLocked = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void UnlockCursor()
        {
            cursorLocked = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                UnlockCursor();
            }
        }

        private void OnDisable()
        {
            UnlockCursor();
        }
    }
}
