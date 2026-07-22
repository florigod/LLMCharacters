using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LLMCharacters
{
    public interface ILLMProvider
    {
        /// <summary>
        /// Streams a response token by token. onToken and onComplete fire on the main thread.
        /// </summary>
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
