using System.Collections.Generic;

namespace LLMCharacters
{
    /// <summary>
    /// Implemented by context assets consumed by NPCBrain. Higher Specificity wins
    /// on key collisions; prose is always accumulated across all layers, never overwritten.
    /// </summary>
    public interface IContextProvider
    {
        /// <summary>Higher = more specific = wins on key collisions. Convention: 0 world, 10 group, 100 individual.</summary>
        int Specificity { get; }

        /// <summary>Free-form knowledge block for this layer, or empty string.</summary>
        string GetProse();

        /// <summary>Key/value facts for this layer, merged by Specificity across all providers.</summary>
        IReadOnlyDictionary<string, ContextEntry> GetEntries();
    }
}
