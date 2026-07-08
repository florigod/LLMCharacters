using UnityEngine;

namespace LLMCharacters.Samples
{
    /// <summary>
    /// Rotates a World Space canvas to always face the player camera (Y-axis only,
    /// so the canvas stays upright). Attach to the canvas GameObject.
    /// No visibility check needed — LateUpdate doesn't fire on inactive GameObjects.
    /// </summary>
    public class CanvasBillboard : MonoBehaviour
    {
        [Tooltip("Leave empty to use Camera.main.")]
        [SerializeField] private Camera targetCamera;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (targetCamera == null) return;

            Vector3 dir = transform.position - targetCamera.transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
