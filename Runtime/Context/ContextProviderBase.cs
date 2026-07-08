using System.Collections.Generic;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Base ScriptableObject for key/value context stores. Extend this to create
    /// shareable context assets at any layer — world, location/group, type/archetype,
    /// or individual. Multiple NPCs can reference the same asset to share state:
    /// because a ScriptableObject is a single instance by reference, a write via
    /// Set() is immediately visible to every NPC that reads it.
    /// </summary>
    public abstract class ContextProviderBase : ScriptableObject, IContextProvider
    {
        [SerializeField]
        [Tooltip("Higher = more specific = wins on key collisions. Convention: " +
                 "0 world, 10 group/location, 20 type/archetype, 100 individual NPC.")]
        protected int specificity;

        public int Specificity => specificity;

        private readonly Dictionary<string, ContextEntry> _entries = new();

        public void Set(string key, string value) =>
            _entries[key] = new ContextEntry(key, value);

        public string Get(string key) =>
            _entries.TryGetValue(key, out var entry) ? entry.value : null;

        public IReadOnlyDictionary<string, ContextEntry> GetEntries() => _entries;

        public void Remove(string key) => _entries.Remove(key);

        public void Clear() => _entries.Clear();

        /// <summary>Prose knowledge for this layer. Empty by default; override to provide one.</summary>
        public virtual string GetProse() => "";

        // Runtime entries are populated by game systems via Set(); clear them so
        // stale data doesn't leak across Play Mode sessions in the Editor.
        protected virtual void OnDisable() => _entries.Clear();
    }
}
