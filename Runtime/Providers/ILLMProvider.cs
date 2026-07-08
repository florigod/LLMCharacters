using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LLMCharacters
{
    public interface ILLMProvider
    {
        /// <summary>
        /// Sends a conversation to the LLM and streams the response token by token.
        /// </summary>
        /// <param name="messages">Full conversation history (user + assistant turns).</param>
        /// <param name="systemPrompt">The assembled system prompt from PromptBuilder.</param>
        /// <param name="config">Connection config (API key, model, etc.).</param>
        /// <param name="onToken">Called on the main thread for each streamed token.</param>
        /// <param name="onComplete">Called on the main thread with the full response when done.</param>
        /// <param name="cancellationToken">Token to cancel an in-flight request.</param>
        Task SendAsync(
            List<Message> messages,
            string systemPrompt,
            LLMConfig config,
            Action<string> onToken,
            Action<string> onComplete,
            CancellationToken cancellationToken
        );
    }
}
