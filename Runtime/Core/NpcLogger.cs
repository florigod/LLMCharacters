using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace LLMCharacters
{
    /// <summary>
    /// Writes per-turn JSONL logs to Application.persistentDataPath/LLMCharacters/logs/.
    /// Token counts and costs are estimates (~4 chars/token, Haiku 4.5 pricing).
    /// The file path is printed to Console when the session starts.
    /// </summary>
    public sealed class NpcLogger
    {
        public string FilePath { get; }

        private readonly string _npcName;
        private int _turnCount;
        private int _totalEstInputTokens;
        private int _totalEstOutputTokens;

        public NpcLogger(string npcName)
        {
            _npcName = string.IsNullOrEmpty(npcName) ? "NPC" : npcName;

            string dir = Path.Combine(Application.persistentDataPath, "LLMCharacters", "logs");
            Directory.CreateDirectory(dir);

            string safeName = _npcName.Replace(' ', '_');
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            FilePath = Path.Combine(dir, $"{safeName}_{timestamp}.jsonl");

            Debug.Log($"[LLM Characters] Logger ready → {FilePath}");
        }

        /// <summary>Log one completed turn. Call after the full response is received.</summary>
        public void LogTurn(
            string model,
            string systemPrompt,
            string userMessage,
            string response,
            long durationMs)
        {
            int estInput  = Mathf.Max(1, (systemPrompt.Length + userMessage.Length) / 4);
            int estOutput = Mathf.Max(1, response.Length / 4);
            _turnCount++;
            _totalEstInputTokens  += estInput;
            _totalEstOutputTokens += estOutput;

            double cost = estInput * 0.000001 + estOutput * 0.000005;

            var sb = new StringBuilder();
            sb.Append('{');
            WriteString(sb, "type", "turn");              sb.Append(',');
            sb.Append($"\"turn\":{_turnCount},");
            WriteString(sb, "timestamp", DateTime.UtcNow.ToString("O")); sb.Append(',');
            WriteString(sb, "npc", _npcName);             sb.Append(',');
            WriteString(sb, "model", model);              sb.Append(',');
            WriteString(sb, "system_prompt", systemPrompt); sb.Append(',');
            WriteString(sb, "user", userMessage);         sb.Append(',');
            WriteString(sb, "assistant", response);       sb.Append(',');
            sb.Append($"\"duration_ms\":{durationMs},");
            sb.Append($"\"est_input_tokens\":{estInput},");
            sb.Append($"\"est_output_tokens\":{estOutput},");
            sb.Append($"\"est_cost_usd_haiku\":{cost.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
            sb.Append('}');
            Append(sb.ToString());

            Debug.Log(
                $"[LLM Characters] Turn {_turnCount} | " +
                $"~{estInput} in / ~{estOutput} out tokens | " +
                $"{durationMs}ms | ~${cost:F5} (Haiku est.)");
        }

        /// <summary>Write a session summary entry. Called from OnDestroy.</summary>
        public void LogEnd()
        {
            if (_turnCount == 0) return;

            double totalCost = _totalEstInputTokens * 0.000001 + _totalEstOutputTokens * 0.000005;
            var sb = new StringBuilder();
            sb.Append('{');
            WriteString(sb, "type", "session_end");       sb.Append(',');
            WriteString(sb, "timestamp", DateTime.UtcNow.ToString("O")); sb.Append(',');
            sb.Append($"\"total_turns\":{_turnCount},");
            sb.Append($"\"total_est_input_tokens\":{_totalEstInputTokens},");
            sb.Append($"\"total_est_output_tokens\":{_totalEstOutputTokens},");
            sb.Append($"\"total_est_cost_usd_haiku\":{totalCost.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)}");
            sb.Append('}');
            Append(sb.ToString());

            Debug.Log(
                $"[LLM Characters] Session ended | {_turnCount} turns | " +
                $"~{_totalEstInputTokens} in / ~{_totalEstOutputTokens} out tokens | " +
                $"~${totalCost:F4} total (Haiku est.) | {FilePath}");
        }

        private void Append(string line)
        {
            try
            {
                File.AppendAllText(FilePath, line + "\n", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLM Characters] Logger write failed: {ex.Message}");
            }
        }

        private static void WriteString(StringBuilder sb, string key, string value)
        {
            sb.Append('"');
            sb.Append(key);
            sb.Append("\":\"");
            AppendEscaped(sb, value);
            sb.Append('"');
        }

        private static void AppendEscaped(StringBuilder sb, string s)
        {
            if (s == null) return;
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    case '\b': sb.Append("\\b");  break;
                    case '\f': sb.Append("\\f");  break;
                    default:
                        if (c < 0x20) sb.Append($"\\u{(int)c:x4}");
                        else sb.Append(c);
                        break;
                }
            }
        }
    }
}
