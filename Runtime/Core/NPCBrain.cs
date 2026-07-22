using System;
using System.Collections.Generic;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Main entry point. Attach to the NPC's GameObject and wire up references in the Inspector.
    /// Call SendPlayerMessage() from your UI or input system to start a turn.
    /// All output goes through StreamHandler events — this class never touches UI directly.
    /// </summary>
    public class NPCBrain : MonoBehaviour
    {
        [Header("Character")]
        [SerializeField] private NPCPersonality personality;

        [Tooltip("Context layers for this NPC. Order doesn't matter — precedence is driven by each " +
                 "provider's Specificity. Typically: WorldContext + optional group contexts + this NPC's own NPCContext.")]
        [SerializeField] private List<ContextProviderBase> contextProviders = new();

        [Header("Connection")]
        [SerializeField] private LLMConfig config;
        [SerializeField] private StreamHandler streamHandler;

        // Raised when a full response is ready / on error — subscribe for custom integrations.
        public event Action<string> OnResponseComplete;
        public event Action<string> OnError;

        /// <summary>Display name of this NPC, or empty if no personality is assigned.</summary>
        public string CharacterName => personality != null ? personality.characterName : "";

        private ConversationManager _conversation;
        private PromptBuilder _promptBuilder;
        private ContextAggregator _contextAggregator;
        private NpcLogger _logger;

        private string _pendingSystemPrompt;
        private string _pendingUserInput;
        private DateTime _requestStartTime;

        private void Awake()
        {
            _conversation = new ConversationManager(config != null ? config.maxHistoryMessages : 10);
            _promptBuilder = new PromptBuilder();
            _contextAggregator = new ContextAggregator();
            _logger = new NpcLogger(CharacterName);

            if (streamHandler != null)
            {
                streamHandler.OnResponseComplete += HandleAssistantResponse;
                streamHandler.OnError += HandleError;
            }
        }

        private void OnDestroy()
        {
            _logger?.LogEnd();

            if (streamHandler != null)
            {
                streamHandler.OnResponseComplete -= HandleAssistantResponse;
                streamHandler.OnError -= HandleError;
            }
        }

        /// <summary>Start a conversation turn. Safe to call from UI buttons or any MonoBehaviour.</summary>
        public async void SendPlayerMessage(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput)) return;

            if (!ValidateSetup()) return;

            AggregatedContext context = _contextAggregator.Aggregate(contextProviders);
            string systemPrompt = _promptBuilder.Build(personality, context.Prose, context.Entries);

            _pendingSystemPrompt = systemPrompt;
            _pendingUserInput = userInput;
            _requestStartTime = DateTime.Now;

            _conversation.AddUserMessage(userInput);
            List<Message> history = _conversation.GetHistory();

            await streamHandler.SendAsync(history, systemPrompt, config);
        }

        public void ClearHistory() => _conversation.Clear();

        private void HandleAssistantResponse(string fullResponse)
        {
            long ms = (long)(DateTime.Now - _requestStartTime).TotalMilliseconds;
            string logModel = streamHandler.IsUsingMockProvider ? "mock" : config.model;
            _logger?.LogTurn(logModel, _pendingSystemPrompt, _pendingUserInput, fullResponse, ms);

            _conversation.AddAssistantMessage(fullResponse);
            OnResponseComplete?.Invoke(fullResponse);
        }

        private void HandleError(string error)
        {
            OnError?.Invoke(error);
        }

        private bool ValidateSetup()
        {
            if (personality == null)
            {
                Debug.LogError("[LLM Characters] NPCBrain: NPCPersonality is not assigned.", this);
                return false;
            }
            if (config == null)
            {
                Debug.LogError("[LLM Characters] NPCBrain: LLMConfig is not assigned.", this);
                return false;
            }
            if (streamHandler == null)
            {
                Debug.LogError("[LLM Characters] NPCBrain: StreamHandler is not assigned.", this);
                return false;
            }
            return true;
        }
    }
}
