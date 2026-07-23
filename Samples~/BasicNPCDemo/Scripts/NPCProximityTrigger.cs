using UnityEngine;

namespace LLMCharacters.Samples
{
    /// <summary>
    /// Attach to the NPC's GameObject alongside a SphereCollider (Is Trigger = true).
    /// Shows the dialogue canvas and unlocks the cursor when the player enters the zone;
    /// hides it and re-locks the cursor when they leave.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class NPCProximityTrigger : MonoBehaviour
    {
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private PlayerController playerController;

        private void Awake()
        {
            if (dialogueCanvas != null)
                dialogueCanvas.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (dialogueCanvas != null)
                dialogueCanvas.gameObject.SetActive(true);

            playerController?.SetDialogueMode(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (dialogueCanvas != null)
                dialogueCanvas.gameObject.SetActive(false);

            playerController?.SetDialogueMode(false);
        }
    }
}
