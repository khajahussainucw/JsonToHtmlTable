using System;

namespace JsonToHtmlTable
{
    /// <summary>
    /// Options that control how <see cref="JsonToHtmlConverter"/> renders JSON as an HTML table.
    /// All properties have sensible defaults — pass <see cref="HtmlTableOptions.Default"/>
    /// or simply omit the argument for the standard rendering.
    /// </summary>
    public sealed class HtmlTableOptions
    {
        /// <summary>
        /// CSS class added to every &lt;table&gt; element. Defaults to <c>"json-to-html-table"</c>.
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
        /// Text used to represent a JSON <c>null</c> value in a cell. Defaults to an empty string.
        /// </summary>
        public string NullText { get; set; } = string.Empty;

        /// <summary>
        /// When true, an array of homogeneous objects is rendered as a single table with one
        /// column per discovered key (the typical "data grid" look). When false, every array
        /// item is rendered as its own inner table. Defaults to <c>true</c>.
        /// </summary>
        public bool RenderArrayOfObjectsAsGrid { get; set; } = true;

        /// <summary>
        /// Maximum recursion depth for nested structures. Protects against pathological or
        /// cyclic-looking JSON. Defaults to 64.
        /// </summary>
        public int MaxDepth { get; set; } = 64;

        /// <summary>
        /// A read-only set of default options. Use this when you want the standard rendering
        /// without allocating a new options object.
        /// </summary>
        public static HtmlTableOptions Default { get; } = new HtmlTableOptions();
    }
}
