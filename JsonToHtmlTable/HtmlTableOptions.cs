using System;
using System.Text.Json;

namespace JsonToHtmlTable
{
    /// <summary>
    /// Options that control how <see cref="JsonToHtmlConverter"/> renders JSON as an HTML table.
    /// All properties have sensible defaults — pass <see cref="HtmlTableOptions.Default"/>
    /// or simply omit the argument for the standard rendering.
    /// </summary>
    /// <remarks>
    /// The library only ever emits an HTML <c>&lt;table&gt;</c> fragment. Document chrome —
    /// titles, stylesheets, headings — is the caller's responsibility.
    /// </remarks>
    public sealed class HtmlTableOptions
    {
        /// <summary>
        /// CSS class added to every <c>&lt;table&gt;</c> element. Defaults to <c>"json-to-html-table"</c>.
        /// Set to <c>null</c> or empty to omit the class attribute entirely.
        /// </summary>
        public string? TableCssClass { get; set; } = "json-to-html-table";

        /// <summary>
        /// Column header used for object keys when rendering an object as a two-column table.
        /// Defaults to <c>"Key"</c>.
        /// </summary>
        public string KeyHeader { get; set; } = "Key";

        /// <summary>
        /// Column header used for object values when rendering an object as a two-column table.
        /// Defaults to <c>"Value"</c>.
        /// </summary>
        public string ValueHeader { get; set; } = "Value";

        /// <summary>
        /// Column header used when rendering an array of primitives (single-column table).
        /// Defaults to <c>"Value"</c>. Set to <c>null</c> or empty to omit the header row entirely.
        /// </summary>
        public string? PrimitiveArrayHeader { get; set; } = "Value";

        /// <summary>
        /// Text used to represent a JSON <c>null</c> value or a missing grid cell. Defaults to an empty string.
        /// </summary>
        public string NullText { get; set; } = string.Empty;

        /// <summary>
        /// When true, an array of homogeneous objects is rendered as a single table with one
        /// column per discovered key (the typical "data grid" look). When false, every array
        /// item is rendered as its own inner table. Defaults to <c>true</c>.
        /// </summary>
        public bool RenderArrayOfObjectsAsGrid { get; set; } = true;

        /// <summary>
        /// When true, adds a leading row-number column to grid tables (arrays of objects)
        /// and to single-column tables for arrays of primitives. Defaults to <c>false</c>.
        /// </summary>
        public bool ShowRowNumbers { get; set; }

        /// <summary>
        /// Header text for the row-number column when <see cref="ShowRowNumbers"/> is enabled.
        /// Defaults to <c>"#"</c>.
        /// </summary>
        public string RowNumberHeader { get; set; } = "#";

        /// <summary>
        /// Optional transform applied to every JSON property name before it is rendered.
        /// Useful for turning <c>userName</c> into <c>"User Name"</c> or <c>user_name</c> into <c>"User name"</c>.
        /// Receives the original key, returns the display text. When <c>null</c>, keys are rendered as-is.
        /// </summary>
        public Func<string, string>? KeyTransform { get; set; }

        /// <summary>
        /// When true, emits minimal inline <c>style</c> attributes on every <c>&lt;table&gt;</c>,
        /// <c>&lt;th&gt;</c>, and <c>&lt;td&gt;</c> so the table renders with visible borders and
        /// padding without requiring any external CSS. Useful for emails, copy/paste into rich-text
        /// surfaces, and quick previews. Defaults to <c>false</c>.
        /// </summary>
        public bool InlineStyles { get; set; }

        /// <summary>
        /// Maximum recursion depth for nested structures. Protects against pathological or
        /// cyclic-looking JSON. Defaults to <c>64</c>.
        /// </summary>
        public int MaxDepth { get; set; } = 64;

        /// <summary>
        /// Options forwarded to <see cref="JsonDocument.Parse(string, JsonDocumentOptions)"/>
        /// when the input is provided as a raw JSON string. Use this to opt into lenient
        /// parsing (allow comments, trailing commas) for real-world JSON that isn't strictly
        /// to spec. Defaults to strict parsing.
        /// </summary>
        public JsonDocumentOptions ParseOptions { get; set; } = default;

        /// <summary>
        /// A read-only set of default options. Use this when you want the standard rendering
        /// without allocating a new options object.
        /// </summary>
        public static HtmlTableOptions Default { get; } = new HtmlTableOptions();
    }
}
