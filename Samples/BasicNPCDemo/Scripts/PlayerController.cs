using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LLMCharacters.Samples
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float mouseSensitivity = 0.15f;

        private CharacterController _controller;
        private float _pitch;
        private float _velocityY;
        private bool _dialogueMode;

        private const float Gravity = -9.81f;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            SetDialogueMode(false);
        }

        private void Update()
        {
            HandleMovement();
            if (!IsTypingInField())
                HandleCameraRotation();
        }

        /// <summary>
        /// Call with true when the dialogue canvas is visible so the cursor is
        /// unlocked and camera rotation is suspended.
        /// </summary>
        public void SetDialogueMode(bool active)
        {
            _dialogueMode = active;
            Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = active;
        }

        /// <summary>
        /// Instantly rotates the player (yaw) and camera (pitch) to face a world point.
        /// Call this when the player selects the dialogue input field so the canvas
        /// ends up centered instead of wherever the camera happened to be aimed.
        /// </summary>
        public void LookAtPoint(Vector3 worldPoint)
        {
            Vector3 origin = cameraTransform != null ? cameraTransform.position : transform.position;
            Vector3 toTarget = worldPoint - origin;

            Vector3 flatDir = toTarget;
            flatDir.y = 0f;
            if (flatDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(flatDir.normalized);

            float horizontalDist = flatDir.magnitude;
            _pitch = Mathf.Clamp(-Mathf.Atan2(toTarget.y, horizontalDist) * Mathf.Rad2Deg, -80f, 80f);
            if (cameraTransform != null)
                cameraTransform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        private static bool IsTypingInField()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            return sel != null && sel.GetComponent<TMPro.TMP_InputField>() != null;
        }

        private void HandleMovement()
        {
            if (IsTypingInField()) return;

            var kb = Keyboard.current;
            float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);

            Vector3 move = transform.right * h + transform.forward * v;
            if (move.sqrMagnitude > 1f) move.Normalize();
            _controller.Move(move * moveSpeed * Time.deltaTime);

            if (_controller.isGrounded && _velocityY < 0f)
                _velocityY = -2f;
            _velocityY += Gravity * Time.deltaTime;
            _controller.Move(Vector3.up * (_velocityY * Time.deltaTime));
        }

        private void HandleCameraRotation()
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            transform.Rotate(Vector3.up, delta.x);

            _pitch -= delta.y;
            _pitch = Mathf.Clamp(_pitch, -80f, 80f);
            if (cameraTransform != null)
                cameraTransform.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }
    }
}
