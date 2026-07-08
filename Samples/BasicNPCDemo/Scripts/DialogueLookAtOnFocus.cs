using TMPro;
using UnityEngine;

namespace LLMCharacters.Samples
{
    /// <summary>
    /// Sample: centers the player's view on the dialogue canvas the moment the
    /// input field gains focus (TMP_InputField.onSelect fires on click, before
    /// typing starts). Fixes the case where mouse movement toward the field
    /// rotates the camera away from the NPC instead of landing on it.
    ///
    /// Lives in Samples (not Runtime) because it depends on the sample
    /// PlayerController — the core SDK stays player-agnostic.
    /// </summary>
    public class DialogueLookAtOnFocus : MonoBehaviour
    {
        [Tooltip("Same TMP_InputField assigned to DialogueUI's Player Input slot.")]
        [SerializeField] private TMP_InputField playerInput;

        [SerializeField] private PlayerController playerController;

        [Tooltip("World point to look at. Leave empty to use this GameObject's own position " +
                 "(works if this script sits on the canvas root).")]
        [SerializeField] private Transform lookTarget;

        private void Start()
        {
            if (playerInput != null)
                playerInput.onSelect.AddListener(OnInputSelected);
        }

        private void OnDestroy()
        {
            if (playerInput != null)
                playerInput.onSelect.RemoveListener(OnInputSelected);
        }

        private void OnInputSelected(string _)
        {
            if (playerController == null) return;
            Vector3 target = lookTarget != null ? lookTarget.position : transform.position;
            playerController.LookAtPoint(target);
        }
    }
}
