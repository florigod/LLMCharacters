using System;
using System.Globalization;
using System.Text;

namespace LLMCharacters
{
    /// <summary>
    /// JSON string escaping and extraction used by providers that parse streamed responses by hand.
    /// No external JSON dependency, by design.
    /// </summary>
    internal static class JsonUtil
    {
        public static string EscapeJson(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder(s.Length + 8);
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
                        if (c < 0x20)
                            sb.Append($"\\u{(int)c:x4}");
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Finds marker in json and decodes the JSON string value that immediately follows it.
        /// fromEnd: search from last occurrence — needed for Anthropic SSE chunks where "text"
        /// appears after other keys that might match the same marker earlier in the line.
        /// </summary>
        public static string ExtractStringValue(string json, string marker, bool fromEnd = false)
        {
            int markerIdx = fromEnd
                ? json.LastIndexOf(marker, StringComparison.Ordinal)
                : json.IndexOf(marker, StringComparison.Ordinal);
            if (markerIdx < 0) return null;

            int i = markerIdx + marker.Length;
            var result = new StringBuilder();

            while (i < json.Length)
            {
                char c = json[i];

                if (c == '\\' && i + 1 < json.Length)
                {
                    char next = json[i + 1];
                    switch (next)
                    {
                        case '"':  result.Append('"');  break;
                        case '\\': result.Append('\\'); break;
                        case 'n':  result.Append('\n'); break;
                        case 'r':  result.Append('\r'); break;
                        case 't':  result.Append('\t'); break;
                        case 'b':  result.Append('\b'); break;
                        case 'f':  result.Append('\f'); break;
                        case 'u' when i + 5 < json.Length:
                            string hex = json.Substring(i + 2, 4);
                            if (int.TryParse(hex, NumberStyles.HexNumber, null, out int cp))
                                result.Append((char)cp);
                            i += 4; // extra advance for the 4 hex digits
                            break;
                        default:
                            result.Append(next);
                            break;
                    }
                    i += 2;
                }
                else if (c == '"')
                {
                    break; // end of string value
                }
                else
                {
                    result.Append(c);
                    i++;
                }
            }

            return result.ToString();
        }
    }
}
