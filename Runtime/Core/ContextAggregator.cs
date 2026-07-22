using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>Result of merging every context layer for one NPC.</summary>
    public readonly struct AggregatedContext
    {
        /// <summary>Accumulated prose from all layers (broadest first).</summary>
        public readonly string Prose;

        /// <summary>Key/value facts with collisions resolved by specificity.</summary>
        public readonly IReadOnlyDictionary<string, ContextEntry> Entries;

        public AggregatedContext(string prose, IReadOnlyDictionary<string, ContextEntry> entries)
        {
            Prose = prose;
            Entries = entries;
        }
    }

    /// <summary>
    /// Merges all context providers into one resolved snapshot. Specificity drives precedence;
    /// equal-specificity collisions on the same key are treated as a design error and logged.
    /// </summary>
    public class ContextAggregator
    {
        public AggregatedContext Aggregate(IReadOnlyList<IContextProvider> providers)
        {
            var resolved = new Dictionary<string, ContextEntry>();

            if (providers == null)
                return new AggregatedContext("", resolved);

            // Sort ascending by specificity so more specific layers overwrite as we go.
            var ordered = new List<IContextProvider>();
            foreach (var p in providers)
                if (p != null) ordered.Add(p);
            ordered.Sort((a, b) => a.Specificity.CompareTo(b.Specificity));

            var winnerSpecificity = new Dictionary<string, int>();
            var prose = new StringBuilder();

            foreach (var provider in ordered)
            {
                string block = provider.GetProse();
                if (!string.IsNullOrWhiteSpace(block))
                {
                    if (prose.Length > 0) prose.Append("\n\n");
                    prose.Append(block.Trim());
                }

                var entries = provider.GetEntries();
                if (entries == null) continue;

                foreach (var kv in entries)
                {
                    if (winnerSpecificity.TryGetValue(kv.Key, out int existing))
                    {
                        if (existing == provider.Specificity)
                        {
                            Debug.LogWarning(
                                $"[LLM Characters] Context key '{kv.Key}' defined twice at equal " +
                                $"specificity {existing}; keeping the first occurrence.");
                            continue;
                        }
                        // existing < current (ascending order) → current layer overrides.
                    }
                    resolved[kv.Key] = kv.Value;
                    winnerSpecificity[kv.Key] = provider.Specificity;
                }
            }

            return new AggregatedContext(prose.ToString(), resolved);
        }
    }
}
