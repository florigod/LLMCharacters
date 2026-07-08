using System.Collections.Generic;

namespace LLMCharacters
{
    /// <summary>
    /// A source of context that contributes to an NPC's system prompt. Implement
    /// this to add custom context sources (quest logs, relationships, inventory,
    /// factions, ...) — NPCBrain consumes an ordered set of these.
    ///
    /// Precedence is resolved by <see cref="Specificity"/>, not by list order:
    /// when two providers define the same entry key, the higher Specificity wins.
    /// Providers at equal Specificity are treated as peers — a same-key collision
    /// between them is a design error and is logged. Prose from <see cref="GetProse"/>
    /// is accumulated across all providers (never overwritten).
    /// </summary>
    public interface IContextProvider
    {
        /// <summary>
        /// Higher = more specific = wins on key collisions.
        /// Convention: 0 = world/global, 10 = group/location, 20 = type/archetype, 100 = individual NPC.
        /// </summary>
        int Specificity { get; }

        /// <summary>Free-form knowledge block for this layer, or empty. Accumulated, never overwritten.</summary>
        string GetProse();

        /// <summary>Key/value facts for this layer. Merged across layers by Specificity.</summary>
        IReadOnlyDictionary<string, ContextEntry> GetEntries();
    }
}
