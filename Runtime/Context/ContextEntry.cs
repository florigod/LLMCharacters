using System;

namespace LLMCharacters
{
    [Serializable]
    public struct ContextEntry
    {
        public string key;
        public string value;
        public DateTime timestamp;

        public ContextEntry(string key, string value)
        {
            this.key = key;
            this.value = value;
            this.timestamp = DateTime.UtcNow;
        }
    }
}
