using System;

namespace LLMCharacters
{
    [Serializable]
    public struct Message
    {
        public string role;    // "user" or "assistant"
        public string content;

        public Message(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
}
