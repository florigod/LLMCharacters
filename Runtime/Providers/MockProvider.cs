using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Development provider that requires no API key or network connection.
    /// Streams a configurable template response word by word to simulate
    /// the typewriter effect and verify the full SDK pipeline.
    ///
    /// Swap AnthropicProvider for MockProvider in the Inspector to develop
    /// UI, context injection, and conversation flow at zero cost.
    /// </summary>
    public class MockProvider : MonoBehaviour, ILLMProvider
    {
        [TextArea(2, 5)]
        [Tooltip("Response template. {characterName} and {world_hint} are replaced at runtime.")]
        public string responseTemplate =
            "[MOCK] Greetings, traveler. I am {characterName}. {world_hint}How may I assist you today?";

        [Range(0f, 0.15f)]
        [Tooltip("Seconds between each word token. Simulates streaming latency.")]
        public float tokenDelay = 0.05f;

        public async Task SendAsync(
            List<Message> messages,
            string systemPrompt,
            LLMConfig config,
            Action<string> onToken,
            Action<string> onComplete,
            CancellationToken cancellationToken)
        {
            string response = responseTemplate
                .Replace("{characterName}", ParseCharacterName(systemPrompt))
                .Replace("{world_hint}", ParseWorldHint(systemPrompt));

            string[] words = response.Split(' ');
            var full = new StringBuilder();

            for (int i = 0; i < words.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string token = (i == 0 ? "" : " ") + words[i];
                full.Append(token);
                onToken?.Invoke(token);

                if (tokenDelay > 0f)
                    await Task.Delay((int)(tokenDelay * 1000f), cancellationToken);
            }

            onComplete?.Invoke(full.ToString().TrimStart());
        }

        private static string ParseCharacterName(string systemPrompt)
        {
            const string marker = "You are ";
            int start = systemPrompt.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "NPC";
            start += marker.Length;
            int end = systemPrompt.IndexOf(",", start, StringComparison.Ordinal);
            return end > start ? systemPrompt.Substring(start, end - start) : "NPC";
        }

        private static string ParseWorldHint(string systemPrompt)
        {
            const string marker = "## Current Context\n";
            int start = systemPrompt.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return "";
            start += marker.Length;
            int end = systemPrompt.IndexOf("\n\n", start, StringComparison.Ordinal);
            if (end < 0) end = systemPrompt.Length;

            string section = systemPrompt.Substring(start, end - start).Trim();
            if (string.IsNullOrEmpty(section)) return "";

            // Return just the first entry as a contextual hint
            string firstLine = section.Split('\n')[0].TrimStart('-', ' ');
            return firstLine + ". ";
        }
    }
}
