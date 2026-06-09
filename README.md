# JsonToHtmlTable

[![NuGet](https://img.shields.io/nuget/v/JsonToHtmlTable.svg)](https://www.nuget.org/packages/JsonToHtmlTable)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Convert any JSON — objects, arrays, deeply nested structures — into a clean HTML table string from C#.

Built on `System.Text.Json`. No `Newtonsoft.Json` dependency forced on consumers. Targets `netstandard2.0`, so it works on .NET Framework 4.6.1+, .NET Core 2.0+, and every modern .NET (5/6/7/8/9/10).

## Install

```bash
dotnet add package JsonToHtmlTable
```

## Quick start

```csharp
using JsonToHtmlTable;

string json = @"{
    ""name"": ""Khaja"",
    ""skills"": [""C#"", ""SQL""],
    ""address"": { ""city"": ""Hyderabad"", ""pin"": 500001 }
}";

string html = JsonToHtmlConverter.Convert(json);
// → <table class="json-to-html-table">...</table>
```

## What it handles

| Input                          | Output                                                                 |
| ------------------------------ | ---------------------------------------------------------------------- |
| Object                         | Two-column table: `Key` / `Value`                                      |
| Array of objects               | Grid: one column per discovered key, one row per item                  |
| Array of primitives            | Single-column table, one row per item                                  |
| Nested objects/arrays          | Rendered recursively as inner `<table>` inside the cell                |
| Strings, numbers, booleans     | HTML-escaped text                                                      |
| `null` / missing properties    | Configurable (empty by default)                                        |

## Customizing

```csharp
var options = new HtmlTableOptions
{
    TableCssClass = "my-table table-striped",
    KeyHeader = "Field",
    ValueHeader = "Data",
    NullText = "—",
    RenderArrayOfObjectsAsGrid = true,
    MaxDepth = 32,
};

string html = JsonToHtmlConverter.Convert(json, options);
```

| Option                       | Default                  | Purpose                                                                 |
| ---------------------------- | ------------------------ | ----------------------------------------------------------------------- |
| `TableCssClass`              | `"json-to-html-table"`   | Class attribute on every `<table>`. Set to `null` to omit.              |
| `KeyHeader`                  | `"Key"`                  | Header text for the key column when rendering an object.                |
| `ValueHeader`                | `"Value"`                | Header text for the value column when rendering an object.              |
| `NullText`                   | `""`                     | Text used for JSON `null` and missing grid cells.                       |
| `RenderArrayOfObjectsAsGrid` | `true`                   | When `true`, an array of objects becomes a single grid table.           |
| `MaxDepth`                   | `64`                     | Recursion guard against pathological JSON.                              |

## API

```csharp
public static class JsonToHtmlConverter
{
    public static string Convert(string json, HtmlTableOptions? options = null);
    public static string Convert(JsonDocument document, HtmlTableOptions? options = null);
    public static string Convert(JsonElement element, HtmlTableOptions? options = null);
}
```

All output is HTML-encoded — safe to embed in any document.

## License

[MIT](LICENSE) © Khaja Hussain
