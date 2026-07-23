using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LLMCharacters.Samples
{
    /// <summary>
    /// Godmode world context injector: free-form text typed here is appended to
    /// WorldContext's prose and appears in every NPC's system prompt on their next turn.
    /// </summary>
    public class WorldContextInjector : MonoBehaviour
    {
        [SerializeField] private WorldContext worldContext;

        [Header("UI References")]
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button setButton;
        [SerializeField] private TMP_Text injectedText;

        private void Start()
        {
            if (setButton != null)
                setButton.onClick.AddListener(OnSetClicked);

            if (input != null)
                input.onSubmit.AddListener(_ => OnSetClicked());

            if (injectedText != null)
                injectedText.text = "";
        }

        private void OnDestroy()
        {
            if (setButton != null)
                setButton.onClick.RemoveListener(OnSetClicked);
        }

        private void OnSetClicked()
        {
            if (worldContext == null || input == null) return;

            string text = input.text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            worldContext.AppendProse(text);

            if (injectedText != null)
                injectedText.text = worldContext.GetProse();

            input.text = "";
            input.Select();
        }
    }
}
