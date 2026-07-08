using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Connects to a local Ollama server (ollama.com) and streams the response
    /// as newline-delimited JSON (NDJSON) — one full JSON object per line, not
    /// Server-Sent Events like AnthropicProvider.
    ///
    /// Requires Ollama installed and already running locally, with the target
    /// model already pulled (e.g. `ollama pull llama3.2`), before Play mode
    /// starts. The SDK does not install, launch, or manage the model — that is
    /// the developer's responsibility. Ollama needs no API key, so this provider
    /// has no credential field — its only connection setting is the server URL.
    ///
    /// Uses System.Net.Http.HttpClient — not supported on WebGL builds.
    /// </summary>
    public class OllamaProvider : MonoBehaviour, ILLMProvider
    {
        private static readonly HttpClient _http = new();

        [Tooltip("Ollama chat endpoint. Change host/port if Ollama runs on a different machine or container.")]
        [SerializeField] private string baseUrl = "http://localhost:11434/api/chat";

        public async Task SendAsync(
            List<Message> messages,
            string systemPrompt,
            LLMConfig config,
            Action<string> onToken,
            Action<string> onComplete,
            CancellationToken cancellationToken)
        {
            string requestJson = BuildRequestJson(messages, systemPrompt, config);

            var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(config.timeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    linkedCts.Token);
            }
            catch (HttpRequestException ex)
            {
                // Connection refused / host unreachable — the most common failure
                // mode is simply "Ollama isn't running", so say that up front.
                throw new Exception(
                    $"Could not reach Ollama at {baseUrl}. Is it running? Start it with " +
                    $"'ollama serve' and make sure the model is pulled " +
                    $"(e.g. 'ollama pull {config.model}'). Details: {ex.Message}");
            }
            catch (TaskCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException($"Request timed out after {config.timeoutSeconds}s.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                throw new Exception($"Ollama API {(int)response.StatusCode}: {body}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var full = new StringBuilder();

            while (!reader.EndOfStream)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                string line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;

                if (line.Contains("\"done\":true")) break;

                string token = JsonUtil.ExtractStringValue(line, "\"content\":\"");
                if (string.IsNullOrEmpty(token)) continue;

                full.Append(token);
                onToken?.Invoke(token);
            }

            onComplete?.Invoke(full.ToString());
        }

        // ── JSON request builder ────────────────────────────────────────────

        private static string BuildRequestJson(List<Message> messages, string systemPrompt, LLMConfig config)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append($"\"model\":\"{JsonUtil.EscapeJson(config.model)}\",");
            sb.Append("\"stream\":true,");

            sb.Append("\"messages\":[");
            sb.Append('{');
            sb.Append("\"role\":\"system\",");
            sb.Append($"\"content\":\"{JsonUtil.EscapeJson(systemPrompt)}\"");
            sb.Append('}');
            for (int i = 0; i < messages.Count; i++)
            {
                sb.Append(',');
                sb.Append('{');
                sb.Append($"\"role\":\"{JsonUtil.EscapeJson(messages[i].role)}\",");
                sb.Append($"\"content\":\"{JsonUtil.EscapeJson(messages[i].content)}\"");
                sb.Append('}');
            }
            sb.Append("],");

            // Ollama's equivalents of Anthropic's top-level temperature/max_tokens
            // live under "options" as "temperature"/"num_predict".
            sb.Append("\"options\":{");
            sb.Append($"\"temperature\":{config.temperature.ToString("F2", CultureInfo.InvariantCulture)},");
            sb.Append($"\"num_predict\":{config.maxTokens}");
            sb.Append('}');

            sb.Append('}');
            return sb.ToString();
        }
    }
}
