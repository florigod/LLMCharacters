using UnityEngine;
using UnityEngine.InputSystem;

namespace LLMCharacters.Samples
{
    public class UIToggleController : MonoBehaviour
    {
        [SerializeField] private GameObject worldInjectorPanel;
        [SerializeField] private GameObject weatherText;
        [SerializeField] private GameObject weatherButton;

        private void Update()
        {
            if (PlayerController.IsTypingInField()) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame)
            {
                if (weatherText != null)   weatherText.SetActive(!weatherText.activeSelf);
                if (weatherButton != null) weatherButton.SetActive(!weatherButton.activeSelf);
            }

            if (kb.digit2Key.wasPressedThisFrame)
            {
                if (worldInjectorPanel != null)
                    worldInjectorPanel.SetActive(!worldInjectorPanel.activeSelf);
            }
        }
    }
}
