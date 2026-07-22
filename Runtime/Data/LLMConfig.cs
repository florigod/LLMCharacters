using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Provider-agnostic generation parameters. Credentials and endpoints live on each
    /// provider component, not here — swapping providers doesn't require touching this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "LLMConfig", menuName = "LLM Characters/LLM Config")]
    public class LLMConfig : ScriptableObject
    {
        [Tooltip("Model ID. Meaning depends on the active provider — e.g. " +
                 "claude-haiku-4-5-20251001 for Anthropic, llama3.2 for Ollama.")]
        public string model = "claude-haiku-4-5-20251001";

        [Range(0f, 1f)]
        [Tooltip("Response creativity. 0 = deterministic, 1 = very creative.")]
        public float temperature = 0.7f;

        [Tooltip("Maximum tokens the NPC can produce per response.")]
        public int maxTokens = 300;

        [Tooltip("HTTP request timeout in seconds.")]
        public int timeoutSeconds = 30;

        [Tooltip("Maximum conversation turns kept in the sliding history window.")]
        public int maxHistoryMessages = 10;
    }
}
