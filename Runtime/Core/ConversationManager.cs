using System.Collections.Generic;

namespace LLMCharacters
{
    /// <summary>
    /// Maintains conversation history as a sliding window.
    /// Older message pairs (user + assistant) are dropped first when the
    /// window is full, preserving the most recent context for the LLM.
    /// </summary>
    public class ConversationManager
    {
        private readonly List<Message> _history = new();
        private int _maxMessages;

        public ConversationManager(int maxMessages = 10)
        {
            _maxMessages = maxMessages > 0 ? maxMessages : 10;
        }

        public void Configure(int maxMessages) =>
            _maxMessages = maxMessages > 0 ? maxMessages : 10;

        public void AddUserMessage(string content) => Add("user", content);
        public void AddAssistantMessage(string content) => Add("assistant", content);

        private void Add(string role, string content)
        {
            _history.Add(new Message(role, content));
            Trim();
        }

        private void Trim()
        {
            // Remove oldest pairs (user + assistant) to stay within the window.
            while (_history.Count > _maxMessages)
            {
                int removeCount = _history.Count >= 2 ? 2 : 1;
                _history.RemoveRange(0, removeCount);
            }
        }

        public List<Message> GetHistory() => new List<Message>(_history);

        public int MessageCount => _history.Count;

        public void Clear() => _history.Clear();
    }
}
