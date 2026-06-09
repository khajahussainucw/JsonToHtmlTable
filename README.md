# JsonToHtmlTable

[![NuGet](https://img.shields.io/nuget/v/JsonToHtmlTable.svg)](https://www.nuget.org/packages/JsonToHtmlTable)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Take any JSON, get back an HTML `<table>`. That's it.

If you've ever needed to render an API response in an internal admin page, drop a CDR export into an email, or just visualize a config file without writing yet another `foreach`, this library is for you. Pass JSON in, you get a clean HTML table back — nested objects become nested tables, arrays of records become grids, everything is HTML-encoded so you can't accidentally ship an XSS bug.

**The output is always a bare `<table>...</table>` fragment** — no document wrapper, no `<style>` block, no `<caption>`. You drop it into whatever page or surface you want. The library does conversion, not page layout.

It's built on the built-in `System.Text.Json`, so installing it doesn't drag a `Newtonsoft.Json` version into your project. It targets `netstandard2.0`, which is the polite way of saying it runs basically everywhere — old .NET Framework apps, modern .NET 10, Xamarin, Unity, the works.

## Install

```bash
dotnet add package JsonToHtmlTable
```

## The 30-second example

```csharp
using JsonToHtmlTable;

string json = """
{
    "subscriberId": "SUB-100245",
    "msisdn": "+91-9876500000",
    "planName": "Postpaid Pro 599",
    "isActive": true,
    "balance": 412.75,
    "tags": ["VoLTE", "5G", "Roaming"],
    "address": { "circle": "APAC-IN", "billingZone": "ZN-7" }
}
""";

string html = JsonToHtmlConverter.Convert(json);
// → <table class="json-to-html-table">...</table>
```

You get back a `<table>` with the simple fields as rows, the `tags` array as a nested mini-table, and `address` recursively rendered as its own table inside a cell. Drop it into a `<div>`, style it however you want.

## Got an array of records? It just becomes a grid

This is where the library actually earns its keep. Throw an array of similarly-shaped objects at it and you get the data-grid view you'd expect — one column per field, one row per record:

```csharp
string cdrs = """
[
    { "callId": "C-001", "originator": "9876500000", "destination": "9123400000", "durationSec": 73, "billedAmount": 1.21 },
    { "callId": "C-002", "originator": "9876500001", "destination": "9123400022", "durationSec": 14, "billedAmount": 0.30 },
    { "callId": "C-003", "originator": "9876500099", "destination": "9123400088", "durationSec": 412, "billedAmount": 6.85 }
]
""";

string html = JsonToHtmlConverter.Convert(cdrs);
```

If some records have keys others don't (it happens — partial CDRs, optional fields), the columns will be the **union** of all keys found, and missing cells fall back to your `NullText`.

## Skip the manual serialization

You don't have to serialize a POCO first — there's a helper that does it in one step:

```csharp
var plan = new
{
    PlanId = "PL-PRO-599",
    Name = "Postpaid Pro",
    Price = 599,
    DataGB = 75,
    VoiceMinutes = -1,        // unlimited
    Features = new[] { "5G", "VoLTE", "IDD" }
};

string html = JsonToHtmlConverter.ConvertObject(plan);
```

It uses `System.Text.Json` under the hood, and you can pass your own `JsonSerializerOptions` if you've got custom naming conventions or converters.

## Big payload? Stream it

Building a huge string in memory is wasteful when you're just going to write it to a file or HTTP response anyway. There are `TextWriter` overloads for exactly that:

```csharp
using var file = File.CreateText("cdrs.html");
JsonToHtmlConverter.Convert(largeCdrJson, file);
```

Same conversion, no intermediate string allocation.

## Don't trust the input? Use `TryConvert`

Real applications get bad JSON from real users. Instead of wrapping calls in `try`/`catch`:

```csharp
if (JsonToHtmlConverter.TryConvert(payloadFromUser, out var html))
{
    return html;
}
return "<p>Sorry, that doesn't look like valid JSON.</p>";
```

Returns `false` for invalid JSON, null, or empty strings. Never throws.

## "My JSON has comments and trailing commas"

Hand-written JSON often does. That's not strictly valid, but you probably still want it to work. Opt in:

```csharp
var html = JsonToHtmlConverter.Convert(messyJson, new HtmlTableOptions
{
    ParseOptions = new JsonDocumentOptions
    {
        CommentHandling     = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    }
});
```

The default is strict parsing — lenient mode is a deliberate choice you make.

## "I want visible borders without writing CSS"

By default, the output is a bare `<table class="json-to-html-table">…</table>` with no inline styling — designed for pages that already have a stylesheet. If you're dropping the output somewhere CSS won't reach (an HTML email, a Notion page, a quick test file), turn on `InlineStyles`:

```csharp
var html = JsonToHtmlConverter.Convert(json, new HtmlTableOptions { InlineStyles = true });
```

That emits inline `style="..."` attributes on `<table>`, `<th>`, and `<td>` with borders, padding, and a header background. Enough to look correct anywhere — no external CSS needed.

## What it handles

| You give it...                 | You get back...                                                        |
| ------------------------------ | ---------------------------------------------------------------------- |
| An object                      | A two-column `Key` / `Value` table                                     |
| An array of objects            | A grid — one column per key, one row per item                          |
| An array of primitives         | A single-column table                                                  |
| An array of arrays (a matrix)  | Each inner array as a nested table inside its row                      |
| A mixed-type array             | Single-column fallback; objects render as inner tables                 |
| Anything nested                | Recursively rendered — there is no depth that breaks it (up to `MaxDepth`) |
| A top-level primitive          | Just the encoded value (no table wrapper)                              |
| Strings with HTML in them      | Encoded — safe to drop into a page (no XSS)                            |
| Unicode (Hindi, Japanese, etc.)| Preserved as-is                                                        |
| Big numbers, decimals, scientific notation | Raw JSON text, no precision loss                           |
| `null` or missing fields       | Whatever `NullText` says (empty string by default)                     |

## All options

```csharp
var html = JsonToHtmlConverter.Convert(json, new HtmlTableOptions
{
    TableCssClass        = "table table-striped",
    KeyHeader            = "Field",
    ValueHeader          = "Data",
    PrimitiveArrayHeader = "Tag",
    NullText             = "—",
    ShowRowNumbers       = true,
    KeyTransform         = key => System.Text.RegularExpressions.Regex
                              .Replace(key, "([a-z])([A-Z])", "$1 $2"),   // msisdnNumber → "msisdn Number"
    InlineStyles         = true,
    MaxDepth             = 32,
    ParseOptions         = new JsonDocumentOptions
    {
        AllowTrailingCommas = true,
        CommentHandling     = JsonCommentHandling.Skip,
    }
});
```

| Option                       | Default                  | What it does                                                              |
| ---------------------------- | ------------------------ | ------------------------------------------------------------------------- |
| `TableCssClass`              | `"json-to-html-table"`   | The `class` attribute on every `<table>`. Set to `null` to drop it entirely. |
| `KeyHeader` / `ValueHeader`  | `"Key"` / `"Value"`      | Headers for the two-column object view.                                   |
| `PrimitiveArrayHeader`       | `"Value"`                | Header for arrays of primitives. Set to `null` to skip the header row.    |
| `NullText`                   | `""`                     | What to render for JSON `null` and missing grid cells.                    |
| `RenderArrayOfObjectsAsGrid` | `true`                   | Turn off if you'd rather render each record as its own little table.       |
| `ShowRowNumbers`             | `false`                  | Adds a leading index column to grids and primitive arrays.                |
| `RowNumberHeader`            | `"#"`                    | What to call the index column.                                            |
| `KeyTransform`               | `null`                   | `Func<string, string>` to rewrite property names at render time.          |
| `InlineStyles`               | `false`                  | Inline `style="..."` on every table/th/td so it looks right without CSS.  |
| `MaxDepth`                   | `64`                     | A safety net for pathological JSON. You shouldn't need to touch this.     |
| `ParseOptions`               | strict                   | `JsonDocumentOptions` — opt into comments / trailing commas / larger depth. |

## API surface

There's intentionally not much. One static class, a handful of overloads, one options bag:

```csharp
public static class JsonToHtmlConverter
{
    // From a JSON string
    public static string Convert(string json, HtmlTableOptions? options = null);
    public static void   Convert(string json, TextWriter writer, HtmlTableOptions? options = null);

    // From System.Text.Json types
    public static string Convert(JsonDocument document, HtmlTableOptions? options = null);
    public static string Convert(JsonElement element,  HtmlTableOptions? options = null);
    public static void   Convert(JsonElement element,  TextWriter writer, HtmlTableOptions? options = null);

    // From a POCO
    public static string ConvertObject(
        object? value,
        HtmlTableOptions? options = null,
        JsonSerializerOptions? serializerOptions = null);

    // Non-throwing
    public static bool TryConvert(string? json, out string html, HtmlTableOptions? options = null);
}
```

## A note on safety

Everything user-supplied gets run through `System.Text.Encodings.Web.HtmlEncoder` with `UnicodeRanges.All`. That means:

- Dangerous characters (`<`, `>`, `&`, `"`, `'`) are always escaped.
- International characters (Hindi, Arabic, Japanese, emoji, etc.) pass through unchanged so your output stays readable.
- You can drop the output into any HTML document without an XSS audit.

Property names and CSS class values get the same treatment. Even if someone hands you JSON like `{ "<script>": "trouble" }`, it ends up as text, not a payload.

## Tested

Every public behavior is covered by xUnit tests — objects, arrays, nesting, HTML encoding, lenient parsing, large numbers, Unicode, edge cases, the lot. They run in under half a second.

![All tests passing](docs/tests-passing.png)

You can run them yourself any time:

```bash
dotnet test
```

## Contributing

Pull requests welcome. Bug reports even more so. Before you submit a PR, make sure `dotnet test` is green. If you're adding a feature, add a test for it.

## License

[MIT](LICENSE) — use it for anything, just keep the copyright notice.
