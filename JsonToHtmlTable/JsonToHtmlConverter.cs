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
    /// or any CLR object) into an HTML <c>&lt;table&gt;</c> fragment. Supports arbitrarily
    /// nested objects and arrays. All text is HTML-encoded — safe to embed in any document.
    /// </summary>
    /// <remarks>
    /// The output is always a table fragment (starts with <c>&lt;table&gt;</c>, ends with
    /// <c>&lt;/table&gt;</c>) — never a full HTML document. Wrap it yourself if you need a page.
    /// </remarks>
    public static class JsonToHtmlConverter
    {
        // Allow international characters through while still XSS-safe.
        private static readonly HtmlEncoder s_encoder = HtmlEncoder.Create(UnicodeRanges.All);

        // Inline styles used when HtmlTableOptions.InlineStyles is true.
        private const string TableStyle = "border-collapse:collapse;margin:.5rem 0;font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;font-size:14px";
        private const string ThStyle    = "border:1px solid #ccc;padding:.4rem .6rem;text-align:left;background:#f4f4f4;font-weight:600";
        private const string TdStyle    = "border:1px solid #ccc;padding:.4rem .6rem;text-align:left;vertical-align:top";

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
            WriteValue(writer, element, opts, depth: 0);
            return sb.ToString();
        }

        /// <summary>
        /// Writes the HTML for <paramref name="element"/> directly to <paramref name="writer"/>.
        /// </summary>
        public static void Convert(JsonElement element, TextWriter writer, HtmlTableOptions? options = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var opts = options ?? HtmlTableOptions.Default;
            WriteValue(writer, element, opts, depth: 0);
        }

        // ---------- CLR object input ----------

        /// <summary>
        /// Serializes <paramref name="value"/> to JSON using <see cref="JsonSerializer"/> and
        /// renders it as an HTML table. A convenience for callers who already have a POCO.
        /// </summary>
        /// <exception cref="JsonException">
        /// Thrown when <paramref name="value"/> contains cyclic references (without an
        /// appropriate <see cref="System.Text.Json.Serialization.ReferenceHandler"/>),
        /// or when serialization otherwise fails.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown when the type of <paramref name="value"/> (or one of its members) has no
        /// compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>.
        /// </exception>
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
        /// Returns <c>true</c> on success; <c>false</c> if the input was null, empty,
        /// not valid JSON, or otherwise unsupported, in which case <paramref name="html"/>
        /// is set to an empty string.
        /// </summary>
        /// <remarks>
        /// This method swallows recoverable input-shape exceptions
        /// (<see cref="JsonException"/>, <see cref="ArgumentException"/>,
        /// <see cref="NotSupportedException"/>) and reports them as <c>false</c>.
        /// Fatal exceptions such as <see cref="OutOfMemoryException"/> propagate as normal.
        /// </remarks>
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
            catch (Exception ex) when (
                ex is JsonException ||
                ex is ArgumentException ||
                ex is NotSupportedException)
            {
                html = string.Empty;
                return false;
            }
        }

        // ---------- Core rendering ----------

        private static void WriteValue(TextWriter w, JsonElement element, HtmlTableOptions opts, int depth)
        {
            if (depth > opts.MaxDepth)
            {
                w.Write("<em>[max depth reached]</em>");
                return;
            }

            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    WriteObject(w, element, opts, depth);
                    break;
                case JsonValueKind.Array:
                    WriteArray(w, element, opts, depth);
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

        private static void WriteObject(TextWriter w, JsonElement obj, HtmlTableOptions opts, int depth)
        {
            WriteTableOpen(w, opts);
            w.Write("<thead><tr>");
            WriteTh(w, opts, opts.KeyHeader);
            WriteTh(w, opts, opts.ValueHeader);
            w.Write("</tr></thead><tbody>");

            foreach (var prop in obj.EnumerateObject())
            {
                w.Write("<tr>");
                WriteTd(w, opts);
                WriteEncoded(w, TransformKey(prop.Name, opts));
                w.Write("</td>");
                WriteTd(w, opts);
                WriteValue(w, prop.Value, opts, depth + 1);
                w.Write("</td></tr>");
            }

            w.Write("</tbody></table>");
        }

        private static void WriteArray(TextWriter w, JsonElement array, HtmlTableOptions opts, int depth)
        {
            if (array.GetArrayLength() == 0)
            {
                WriteTableOpen(w, opts);
                w.Write("<tbody></tbody></table>");
                return;
            }

            if (opts.RenderArrayOfObjectsAsGrid && IsArrayOfObjects(array))
            {
                WriteArrayAsGrid(w, array, opts, depth);
                return;
            }

            // Single-column table for primitives or mixed arrays.
            WriteTableOpen(w, opts);
            var header = opts.PrimitiveArrayHeader;
            if (opts.ShowRowNumbers || !string.IsNullOrEmpty(header))
            {
                w.Write("<thead><tr>");
                if (opts.ShowRowNumbers) WriteTh(w, opts, opts.RowNumberHeader);
                WriteTh(w, opts, header ?? string.Empty);
                w.Write("</tr></thead>");
            }
            w.Write("<tbody>");
            int rowIndex = 1;
            foreach (var item in array.EnumerateArray())
            {
                w.Write("<tr>");
                if (opts.ShowRowNumbers)
                {
                    WriteTd(w, opts);
                    w.Write(rowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    w.Write("</td>");
                }
                WriteTd(w, opts);
                WriteValue(w, item, opts, depth + 1);
                w.Write("</td></tr>");
                rowIndex++;
            }
            w.Write("</tbody></table>");
        }

        private static void WriteArrayAsGrid(TextWriter w, JsonElement array, HtmlTableOptions opts, int depth)
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

            WriteTableOpen(w, opts);
            w.Write("<thead><tr>");
            if (opts.ShowRowNumbers) WriteTh(w, opts, opts.RowNumberHeader);
            foreach (var col in columns) WriteTh(w, opts, TransformKey(col, opts));
            w.Write("</tr></thead><tbody>");

            int rowIndex = 1;
            foreach (var item in array.EnumerateArray())
            {
                w.Write("<tr>");
                if (opts.ShowRowNumbers)
                {
                    WriteTd(w, opts);
                    w.Write(rowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    w.Write("</td>");
                }
                foreach (var col in columns)
                {
                    WriteTd(w, opts);
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

        // ---------- Element emitters ----------

        private static void WriteTableOpen(TextWriter w, HtmlTableOptions opts)
        {
            w.Write("<table");
            if (!string.IsNullOrEmpty(opts.TableCssClass))
            {
                w.Write(" class=\"");
                WriteEncoded(w, opts.TableCssClass);
                w.Write("\"");
            }
            if (opts.InlineStyles)
            {
                w.Write(" style=\"");
                w.Write(TableStyle);
                w.Write("\"");
            }
            w.Write(">");
        }

        private static void WriteTh(TextWriter w, HtmlTableOptions opts, string text)
        {
            w.Write("<th");
            if (opts.InlineStyles)
            {
                w.Write(" style=\"");
                w.Write(ThStyle);
                w.Write("\"");
            }
            w.Write(">");
            WriteEncoded(w, text);
            w.Write("</th>");
        }

        private static void WriteTd(TextWriter w, HtmlTableOptions opts)
        {
            w.Write("<td");
            if (opts.InlineStyles)
            {
                w.Write(" style=\"");
                w.Write(TdStyle);
                w.Write("\"");
            }
            w.Write(">");
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
