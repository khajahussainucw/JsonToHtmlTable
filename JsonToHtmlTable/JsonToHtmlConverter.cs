using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace JsonToHtmlTable
{
    /// <summary>
    /// Converts JSON (a string, a <see cref="JsonDocument"/>, or a <see cref="JsonElement"/>)
    /// into an HTML table string. Supports arbitrarily nested objects and arrays.
    /// </summary>
    /// <example>
    /// <code>
    /// string json = "{\"name\":\"Khaja\",\"skills\":[\"C#\",\"SQL\"]}";
    /// string html = JsonToHtmlConverter.Convert(json);
    /// </code>
    /// </example>
    public static class JsonToHtmlConverter
    {
        /// <summary>
        /// Parses <paramref name="json"/> and converts it to an HTML table string.
        /// </summary>
        /// <param name="json">A JSON document as a string. Must be valid JSON.</param>
        /// <param name="options">Optional rendering options. Defaults to <see cref="HtmlTableOptions.Default"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">Thrown when the input is not valid JSON.</exception>
        public static string Convert(string json, HtmlTableOptions? options = null)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));

            using var doc = JsonDocument.Parse(json);
            return Convert(doc.RootElement, options);
        }

        /// <summary>
        /// Converts a <see cref="JsonDocument"/> to an HTML table string.
        /// </summary>
        public static string Convert(JsonDocument document, HtmlTableOptions? options = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Convert(document.RootElement, options);
        }

        /// <summary>
        /// Converts a <see cref="JsonElement"/> to an HTML table string.
        /// </summary>
        public static string Convert(JsonElement element, HtmlTableOptions? options = null)
        {
            var opts = options ?? HtmlTableOptions.Default;
            var sb = new StringBuilder(256);
            RenderValue(sb, element, opts, depth: 0);
            return sb.ToString();
        }

        private static void RenderValue(StringBuilder sb, JsonElement element, HtmlTableOptions opts, int depth)
        {
            if (depth > opts.MaxDepth)
            {
                sb.Append("<em>[max depth reached]</em>");
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    RenderObject(sb, element, opts, depth);
                    break;
                case JsonValueKind.Array:
                    RenderArray(sb, element, opts, depth);
                    break;
                case JsonValueKind.String:
                    sb.Append(Encode(element.GetString()));
                    break;
                case JsonValueKind.Number:
                    sb.Append(Encode(element.GetRawText()));
                    break;
                case JsonValueKind.True:
                    sb.Append("true");
                    break;
                case JsonValueKind.False:
                    sb.Append("false");
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    sb.Append(Encode(opts.NullText));
                    break;
            }
        }

        private static void RenderObject(StringBuilder sb, JsonElement obj, HtmlTableOptions opts, int depth)
        {
            AppendTableOpen(sb, opts);
            sb.Append("<thead><tr><th>")
              .Append(Encode(opts.KeyHeader))
              .Append("</th><th>")
              .Append(Encode(opts.ValueHeader))
              .Append("</th></tr></thead><tbody>");

            foreach (var prop in obj.EnumerateObject())
            {
                sb.Append("<tr><td>").Append(Encode(prop.Name)).Append("</td><td>");
                RenderValue(sb, prop.Value, opts, depth + 1);
                sb.Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }

        private static void RenderArray(StringBuilder sb, JsonElement array, HtmlTableOptions opts, int depth)
        {
            // Empty array → empty table shell.
            if (array.GetArrayLength() == 0)
            {
                AppendTableOpen(sb, opts);
                sb.Append("<tbody></tbody></table>");
                return;
            }

            // Check if the array is uniformly objects — render as grid if requested.
            if (opts.RenderArrayOfObjectsAsGrid && IsArrayOfObjects(array))
            {
                RenderArrayAsGrid(sb, array, opts, depth);
                return;
            }

            // Otherwise: single-column table, one row per item.
            AppendTableOpen(sb, opts);
            sb.Append("<tbody>");
            foreach (var item in array.EnumerateArray())
            {
                sb.Append("<tr><td>");
                RenderValue(sb, item, opts, depth + 1);
                sb.Append("</td></tr>");
            }
            sb.Append("</tbody></table>");
        }

        private static void RenderArrayAsGrid(StringBuilder sb, JsonElement array, HtmlTableOptions opts, int depth)
        {
            // Build the column set as the union of keys, preserving first-seen order.
            var columns = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in array.EnumerateArray())
            {
                foreach (var prop in item.EnumerateObject())
                {
                    if (seen.Add(prop.Name)) columns.Add(prop.Name);
                }
            }

            AppendTableOpen(sb, opts);
            sb.Append("<thead><tr>");
            foreach (var col in columns)
            {
                sb.Append("<th>").Append(Encode(col)).Append("</th>");
            }
            sb.Append("</tr></thead><tbody>");

            foreach (var item in array.EnumerateArray())
            {
                sb.Append("<tr>");
                foreach (var col in columns)
                {
                    sb.Append("<td>");
                    if (item.TryGetProperty(col, out var cell))
                    {
                        RenderValue(sb, cell, opts, depth + 1);
                    }
                    else
                    {
                        sb.Append(Encode(opts.NullText));
                    }
                    sb.Append("</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</tbody></table>");
        }

        private static bool IsArrayOfObjects(JsonElement array)
        {
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) return false;
            }
            return true;
        }

        private static void AppendTableOpen(StringBuilder sb, HtmlTableOptions opts)
        {
            if (string.IsNullOrEmpty(opts.TableCssClass))
            {
                sb.Append("<table>");
            }
            else
            {
                sb.Append("<table class=\"").Append(Encode(opts.TableCssClass)).Append("\">");
            }
        }

        private static string Encode(string? value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return WebUtility.HtmlEncode(value);
        }
    }
}
