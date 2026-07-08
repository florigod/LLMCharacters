using UnityEngine;

namespace LLMCharacters
{
    [CreateAssetMenu(fileName = "NPCPersonality", menuName = "LLM Characters/NPC Personality")]
    public class NPCPersonality : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("The NPC's name, injected into the system prompt.")]
        public string characterName = "NPC";

        [TextArea(3, 8)]
        [Tooltip("Personality description: tone, speech style, quirks, goals.")]
        public string personalityPrompt = "";

        [TextArea(3, 8)]
        [Tooltip("Background story that may influence how the NPC responds.")]
        public string backgroundStory = "";

        [Header("Response Format")]
        [Range(1, 5)]
        [Tooltip("Maximum number of sentences per response. Default 3.")]
        public int maxResponseSentences = 3;

        [Tooltip("Allow the NPC to use *action descriptions* (e.g. '*smiles warmly*'). " +
                 "Disable for clean dialogue-only responses.")]
        public bool allowAsteriskActions = true;

        [Tooltip("Allow the NPC to use emojis. Keep off for most RPG / serious tone scenarios.")]
        public bool allowEmojis = false;

        [TextArea(2, 6)]
        [Tooltip("Additional response rules appended as bullet points. One rule per line. " +
                 "Examples: 'Respond only in Spanish.' / 'Keep all language family-friendly.' " +
                 "/ 'Speak in archaic English (thee, thou, dost).'")]
        public string additionalResponseRules = "";
    }
}
