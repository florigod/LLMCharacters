using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Shared world state readable by all NPCs. Any game system can write here;
    /// every NPC reads it when building its system prompt. Lowest specificity by
    /// default (0) so more specific layers can override individual keys.
    /// Create a single WorldContext asset and reference it from every NPC.
    /// </summary>
    [CreateAssetMenu(fileName = "WorldContext", menuName = "LLM Characters/World Context")]
    public class WorldContext : ContextProviderBase
    {
        // specificity defaults to 0 (the base field's default) — the broadest layer.
    }
}
