using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace JsonToHtmlTable
{
    /// <summary>
    /// Converts JSON (a string, a <see cref="JsonDocument"/>, a <see cref="JsonElement"/>,
    /// or any CLR object) into an HTML table string. Supports arbitrarily nested objects
    /// and arrays. All text is HTML-encoded — safe to embed in any document.
    /// </summary>
    /// <example>
    /// <code>
    /// string json = "{\"name\":\"Khaja\",\"skills\":[\"C#\",\"SQL\"]}";
    /// string html = JsonToHtmlConverter.Convert(json);
    /// </code>
    /// </example>
    public static class JsonToHtmlConverter
    {
        // Allow international characters through while still XSS-safe.
        private static readonly HtmlEncoder s_encoder = HtmlEncoder.Create(UnicodeRanges.All);

        private const string DefaultStyles =
            "body{font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;margin:1.5rem;color:#222}" +
            "table.json-to-html-table{border-collapse:collapse;margin:.5rem 0;font-size:14px}" +
            "table.json-to-html-table th,table.json-to-html-table td{border:1px solid #ccc;padding:.4rem .6rem;text-align:left;vertical-align:top}" +
            "table.json-to-html-table th{background:#f4f4f4;font-weight:600}" +
            "table.json-to-html-table caption{font-weight:600;padding:.4rem 0;text-align:left}";

        // ---------- string input ----------

        /// <summary>
        /// Parses <paramref name="json"/> and returns an HTML table string.
        /// </summary>
        /// <exception cref="ArgumentNullException">When <paramref name="json"/> is null.</exception>
        /// <exception cref="JsonException">When the input is not valid JSON.</exception>
        public static string Convert(string json, HtmlTableOptions? options = null)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var opts = options ?? HtmlTableOptions.Default;
            using var doc = JsonDocument.Parse(json, opts.ParseOptions);
            return Convert(doc.RootElement, opts);
        }

        /// <summary>
        /// Parses <paramref name="json"/> and writes the HTML directly to <paramref name="writer"/>.
        /// Avoids building a large intermediate string for big inputs.
        /// </summary>
        public static void Convert(string json, TextWriter writer, HtmlTableOptions? options = null)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var opts = options ?? HtmlTableOptions.Default;
            using var doc = JsonDocument.Parse(json, opts.ParseOptions);
            Convert(doc.RootElement, writer, opts);
        }

        // ---------- JsonDocument input ----------

        /// <summary>
        /// Converts a <see cref="JsonDocument"/> to an HTML table string.
        /// </summary>
        public static string Convert(JsonDocument document, HtmlTableOptions? options = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            return Convert(document.RootElement, options);
        }

        // ---------- JsonElement input ----------

        /// <summary>
        /// Converts a <see cref="JsonElement"/> to an HTML table string.
        /// </summary>
        public static string Convert(JsonElement element, HtmlTableOptions? options = null)
        {
            var opts = options ?? HtmlTableOptions.Default;
            var sb = new StringBuilder(256);
            using var writer = new StringWriter(sb);
            WriteRoot(writer, element, opts);
            return sb.ToString();
        }

        /// <summary>
        /// Writes the HTML for <paramref name="element"/> directly to <paramref name="writer"/>.
        /// </summary>
        public static void Convert(JsonElement element, TextWriter writer, HtmlTableOptions? options = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var opts = options ?? HtmlTableOptions.Default;
            WriteRoot(writer, element, opts);
        }

        // ---------- CLR object input ----------

        /// <summary>
        /// Serializes <paramref name="value"/> to JSON using <see cref="JsonSerializer"/> and
        /// renders it as an HTML table. A convenience for callers who already have a POCO.
        /// </summary>
        public static string ConvertObject(
            object? value,
            HtmlTableOptions? options = null,
            JsonSerializerOptions? serializerOptions = null)
        {
            var json = JsonSerializer.Serialize(value, serializerOptions);
            return Convert(json, options);
        }

        // ---------- Non-throwing variant ----------

        /// <summary>
        /// Attempts to convert <paramref name="json"/> to an HTML table.
        /// Returns <c>true</c> on success; <c>false</c> if the input was not valid JSON,
        /// in which case <paramref name="html"/> is set to an empty string.
        /// </summary>
        public static bool TryConvert(string? json, out string html, HtmlTableOptions? options = null)
        {
            if (json == null)
            {
                html = string.Empty;
                return false;
            }

            try
            {
                html = Convert(json, options);
                return true;
            }
            catch (JsonException)
            {
                html = string.Empty;
                return false;
            }
        }

        // ---------- Core rendering ----------

        private static void WriteRoot(TextWriter w, JsonElement element, HtmlTableOptions opts)
        {
            if (opts.WrapInHtmlDocument)
            {
                w.Write("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>");
                WriteEncoded(w, opts.DocumentTitle);
                w.Write("</title>");
                if (opts.IncludeDefaultStyles)
                {
                    w.Write("<style>");
                    w.Write(DefaultStyles);
                    w.Write("</style>");
                }
                w.Write("</head><body>");
                WriteValue(w, element, opts, depth: 0, isOutermost: true);
                w.Write("</body></html>");
            }
            else
            {
                WriteValue(w, element, opts, depth: 0, isOutermost: true);
            }
        }

        private static void WriteValue(TextWriter w, JsonElement element, HtmlTableOptions opts, int depth, bool isOutermost = false)
        {
            if (depth > opts.MaxDepth)
            {
                w.Write("<em>[max depth reached]</em>");
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    WriteObject(w, element, opts, depth, isOutermost);
                    break;
                case JsonValueKind.Array:
                    WriteArray(w, element, opts, depth, isOutermost);
                    break;
                case JsonValueKind.String:
                    WriteEncoded(w, element.GetString());
                    break;
                case JsonValueKind.Number:
                    // GetRawText preserves precision exactly as authored.
                    w.Write(element.GetRawText());
                    break;
                case JsonValueKind.True:
                    w.Write("true");
                    break;
                case JsonValueKind.False:
                    w.Write("false");
                    break;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    WriteEncoded(w, opts.NullText);
                    break;
            }
        }

        private static void WriteObject(TextWriter w, JsonElement obj, HtmlTableOptions opts, int depth, bool isOutermost)
        {
            WriteTableOpen(w, opts, isOutermost);
            w.Write("<thead><tr><th>");
            WriteEncoded(w, opts.KeyHeader);
            w.Write("</th><th>");
            WriteEncoded(w, opts.ValueHeader);
            w.Write("</th></tr></thead><tbody>");

            foreach (var prop in obj.EnumerateObject())
            {
                w.Write("<tr><td>");
                WriteEncoded(w, TransformKey(prop.Name, opts));
                w.Write("</td><td>");
                WriteValue(w, prop.Value, opts, depth + 1);
                w.Write("</td></tr>");
            }

            w.Write("</tbody></table>");
        }

        private static void WriteArray(TextWriter w, JsonElement array, HtmlTableOptions opts, int depth, bool isOutermost)
        {
            if (array.GetArrayLength() == 0)
            {
                WriteTableOpen(w, opts, isOutermost);
                w.Write("<tbody></tbody></table>");
                return;
            }

            if (opts.RenderArrayOfObjectsAsGrid && IsArrayOfObjects(array))
            {
                WriteArrayAsGrid(w, array, opts, depth, isOutermost);
                return;
            }

            // Single-column table for primitives or mixed arrays.
            WriteTableOpen(w, opts, isOutermost);
            var header = opts.PrimitiveArrayHeader;
            if (opts.ShowRowNumbers || !string.IsNullOrEmpty(header))
            {
                w.Write("<thead><tr>");
                if (opts.ShowRowNumbers)
                {
                    w.Write("<th>");
                    WriteEncoded(w, opts.RowNumberHeader);
                    w.Write("</th>");
                }
                w.Write("<th>");
                WriteEncoded(w, header ?? string.Empty);
                w.Write("</th></tr></thead>");
            }
            w.Write("<tbody>");
            int rowIndex = 1;
            foreach (var item in array.EnumerateArray())
            {
                w.Write("<tr>");
                if (opts.ShowRowNumbers)
                {
                    w.Write("<td>");
                    w.Write(rowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    w.Write("</td>");
                }
                w.Write("<td>");
                WriteValue(w, item, opts, depth + 1);
                w.Write("</td></tr>");
                rowIndex++;
            }
            w.Write("</tbody></table>");
        }

        private static void WriteArrayAsGrid(TextWriter w, JsonElement array, HtmlTableOptions opts, int depth, bool isOutermost)
        {
            var columns = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in array.EnumerateArray())
            {
                foreach (var prop in item.EnumerateObject())
                {
                    if (seen.Add(prop.Name)) columns.Add(prop.Name);
                }
            }

            WriteTableOpen(w, opts, isOutermost);
            w.Write("<thead><tr>");
            if (opts.ShowRowNumbers)
            {
                w.Write("<th>");
                WriteEncoded(w, opts.RowNumberHeader);
                w.Write("</th>");
            }
            foreach (var col in columns)
            {
                w.Write("<th>");
                WriteEncoded(w, TransformKey(col, opts));
                w.Write("</th>");
            }
            w.Write("</tr></thead><tbody>");

            int rowIndex = 1;
            foreach (var item in array.EnumerateArray())
            {
                w.Write("<tr>");
                if (opts.ShowRowNumbers)
                {
                    w.Write("<td>");
                    w.Write(rowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    w.Write("</td>");
                }
                foreach (var col in columns)
                {
                    w.Write("<td>");
                    if (item.TryGetProperty(col, out var cell))
                    {
                        WriteValue(w, cell, opts, depth + 1);
                    }
                    else
                    {
                        WriteEncoded(w, opts.NullText);
                    }
                    w.Write("</td>");
                }
                w.Write("</tr>");
                rowIndex++;
            }

            w.Write("</tbody></table>");
        }

        private static bool IsArrayOfObjects(JsonElement array)
        {
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) return false;
            }
            return true;
        }

        private static void WriteTableOpen(TextWriter w, HtmlTableOptions opts, bool isOutermost)
        {
            if (string.IsNullOrEmpty(opts.TableCssClass))
            {
                w.Write("<table>");
            }
            else
            {
                w.Write("<table class=\"");
                WriteEncoded(w, opts.TableCssClass);
                w.Write("\">");
            }
            if (isOutermost && !string.IsNullOrEmpty(opts.Caption))
            {
                w.Write("<caption>");
                WriteEncoded(w, opts.Caption);
                w.Write("</caption>");
            }
        }

        private static string TransformKey(string key, HtmlTableOptions opts)
        {
            var transform = opts.KeyTransform;
            return transform == null ? key : transform(key) ?? string.Empty;
        }

        private static void WriteEncoded(TextWriter w, string? value)
        {
            if (value == null || value.Length == 0) return;
            s_encoder.Encode(w, value);
        }
    }
}
