using System;
using System.Globalization;
using System.Text;

namespace LLMCharacters
{
    /// <summary>
    /// Minimal JSON string escaping/extraction shared by providers that build
    /// requests and parse streamed responses by hand (no external JSON library
    /// dependency, per SDK design).
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
        /// Finds `marker` in <paramref name="json"/> and decodes the JSON string
        /// value that immediately follows it (handles \", \\, \n, \r, \t, \b, \f,
        /// \uXXXX). Returns null if the marker isn't found.
        /// </summary>
        /// <param name="fromEnd">
        /// Search from the last occurrence instead of the first. Anthropic's SSE
        /// chunks put the text field at the end of the delta object, after other
        /// keys that could contain the same substring earlier in the line.
        /// </param>
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
