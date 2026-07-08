using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Assembles the system prompt from: character identity/personality, the
    /// accumulated scene-knowledge prose across context layers, the resolved
    /// key/value context (already merged by specificity in ContextAggregator),
    /// and configurable response format rules. Sections are only included when non-empty.
    /// </summary>
    public class PromptBuilder
    {
        public string Build(
            NPCPersonality personality,
            string sceneProse,
            IReadOnlyDictionary<string, ContextEntry> contextEntries)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"You are {personality.characterName}, a character in a video game.");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(personality.personalityPrompt))
            {
                sb.AppendLine("## Personality");
                sb.AppendLine(personality.personalityPrompt.Trim());
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(personality.backgroundStory))
            {
                sb.AppendLine("## Background");
                sb.AppendLine(personality.backgroundStory.Trim());
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(sceneProse))
            {
                sb.AppendLine("## Scene Knowledge");
                sb.AppendLine(sceneProse.Trim());
                sb.AppendLine();
            }

            if (contextEntries != null && contextEntries.Count > 0)
            {
                sb.AppendLine("## Current Context");
                foreach (var kv in contextEntries)
                    sb.AppendLine($"- {kv.Key}: {kv.Value.value}");
                sb.AppendLine();
            }

            sb.AppendLine("## Response Rules");
            sb.AppendLine($"- Respond in 1-{personality.maxResponseSentences} sentences, staying in character at all times.");
            sb.AppendLine("- Never break character or acknowledge being an AI.");
            sb.AppendLine("- Incorporate the provided context naturally when relevant.");
            sb.AppendLine("- STRICT: Only state facts explicitly provided in Personality, Background, Scene Knowledge, or Current Context. Nothing else exists.");
            sb.AppendLine("- STRICT: Do not describe, narrate, or assume any actions, intentions, or behaviors of the player. React only to their exact words.");
            sb.AppendLine("- STRICT: You have NO information about your own physical appearance (clothing, hat, hair, eyes, height, etc.) unless it is explicitly written in your Background or Scene Knowledge. If asked about any appearance detail not provided, say you don't know or deflect in character — never invent a description.");
            sb.AppendLine("- STRICT: You have NO information about the player (name, appearance, history, actions) unless explicitly provided in context. Never invent or assume anything about them.");

            if (!personality.allowAsteriskActions)
                sb.AppendLine("- Do not use asterisk action descriptions (e.g. *smiles*, *nods*).");

            if (!personality.allowEmojis)
                sb.AppendLine("- Do not use emojis.");

            if (!string.IsNullOrWhiteSpace(personality.additionalResponseRules))
            {
                foreach (string line in personality.additionalResponseRules.Split('\n'))
                {
                    string rule = line.Trim().TrimStart('-', ' ');
                    if (!string.IsNullOrEmpty(rule))
                        sb.AppendLine($"- {rule}");
                }
            }

            string result = sb.ToString().TrimEnd();

            Debug.Log($"[LLM Characters] System prompt assembled:\n{result}");

            return result;
        }
    }
}
