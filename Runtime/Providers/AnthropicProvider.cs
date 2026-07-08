using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Connects to api.anthropic.com/v1/messages and streams the response via
    /// Server-Sent Events (SSE). Uses System.Net.Http.HttpClient — not supported
    /// on WebGL builds. For WebGL, use MockProvider instead.
    ///
    /// One HttpClient instance is shared across all provider instances
    /// (the correct .NET pattern to avoid socket exhaustion).
    /// </summary>
    public class AnthropicProvider : MonoBehaviour, ILLMProvider
    {
        private static readonly HttpClient _http = new();

        private const string ApiUrl = "https://api.anthropic.com/v1/messages";
        private const string AnthropicApiVersion = "2023-06-01";

        [Tooltip("Your Anthropic API key. Keep this out of version control. " +
                 "Lives here (not in LLMConfig) because it is Anthropic-specific.")]
        [SerializeField] private string apiKey = "";

        public async Task SendAsync(
            List<Message> messages,
            string systemPrompt,
            LLMConfig config,
            Action<string> onToken,
            Action<string> onComplete,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("Anthropic API key is not set. Assign it on the AnthropicProvider component.");

            string requestJson = BuildRequestJson(messages, systemPrompt, config);

            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", AnthropicApiVersion);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

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
            catch (TaskCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException($"Request timed out after {config.timeoutSeconds}s.");
            }

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync();
                string msg = ExtractErrorMessage(body);
                throw new Exception($"Anthropic API {(int)response.StatusCode}: {msg}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var full = new StringBuilder();

            while (!reader.EndOfStream)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                string line = await reader.ReadLineAsync();

                // SSE lines: blank lines are separators; event: lines are discarded;
                // we only process data: lines.
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
                    continue;

                string data = line.Substring(6);

                if (data == "[DONE]") break;
                if (data.Contains("\"message_stop\"")) break;

                string token = ExtractTextToken(data);
                if (token == null) continue;

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
            sb.Append($"\"max_tokens\":{config.maxTokens},");
            sb.Append($"\"temperature\":{config.temperature.ToString("F2", CultureInfo.InvariantCulture)},");
            sb.Append($"\"system\":\"{JsonUtil.EscapeJson(systemPrompt)}\",");
            sb.Append("\"stream\":true,");
            sb.Append("\"messages\":[");

            for (int i = 0; i < messages.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('{');
                sb.Append($"\"role\":\"{JsonUtil.EscapeJson(messages[i].role)}\",");
                sb.Append($"\"content\":\"{JsonUtil.EscapeJson(messages[i].content)}\"");
                sb.Append('}');
            }

            sb.Append("]}");
            return sb.ToString();
        }

        // ── SSE / JSON parsers ──────────────────────────────────────────────

        /// <summary>
        /// Extracts the text value from a content_block_delta / text_delta SSE chunk.
        /// Returns null for any other event type (message_start, ping, etc.).
        /// Handles JSON string escape sequences inline.
        /// </summary>
        private static string ExtractTextToken(string data)
        {
            if (!data.Contains("\"content_block_delta\"") || !data.Contains("\"text_delta\""))
                return null;

            // The delta object always appears at the end:
            // {...,"delta":{"type":"text_delta","text":"Hello"}}
            return JsonUtil.ExtractStringValue(data, "\"text\":\"", fromEnd: true);
        }

        private static string ExtractErrorMessage(string errorJson)
        {
            const string marker = "\"message\":\"";
            int start = errorJson.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return errorJson;
            start += marker.Length;
            int end = errorJson.IndexOf('"', start);
            return end > start ? errorJson.Substring(start, end - start) : errorJson;
        }
    }
}
