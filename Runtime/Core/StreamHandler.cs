using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Bridges the ILLMProvider with the rest of the scene via C# events.
    /// Attach to the same GameObject as NPCBrain. Assign the provider component
    /// (AnthropicProvider or MockProvider) in the Inspector.
    ///
    /// DialogueUI and other components subscribe to the events exposed here.
    /// </summary>
    public class StreamHandler : MonoBehaviour
    {
        [Tooltip("The provider component that handles API calls. Must implement ILLMProvider.")]
        [SerializeField] private MonoBehaviour providerComponent;

        public event Action OnRequestStarted;
        public event Action<string> OnTokenReceived;
        public event Action<string> OnResponseComplete;
        public event Action<string> OnError;

        private CancellationTokenSource _cts;

        private ILLMProvider Provider
        {
            get
            {
                if (providerComponent is ILLMProvider provider) return provider;
                Debug.LogError("[LLM Characters] StreamHandler: assigned component does not implement ILLMProvider.");
                return null;
            }
        }

        public bool IsUsingMockProvider => providerComponent is MockProvider;

        public async Task SendAsync(List<Message> messages, string systemPrompt, LLMConfig config)
        {
            if (Provider == null) return;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            OnRequestStarted?.Invoke();

            try
            {
                await Provider.SendAsync(
                    messages,
                    systemPrompt,
                    config,
                    token => OnTokenReceived?.Invoke(token),
                    fullResponse => OnResponseComplete?.Invoke(fullResponse),
                    _cts.Token
                );
            }
            catch (OperationCanceledException)
            {
                // Request was cancelled — not an error.
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLM Characters] StreamHandler error: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public void Cancel() => _cts?.Cancel();

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
