using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using JsonToHtmlTable;
using Xunit;
using Xunit.Abstractions;

namespace JsonToHtmlTable.Tests;

/// <summary>
/// These aren't really "tests" — they're a way to *see* what the library produces.
/// One HTML file is generated per JSON fixture in <c>TestData/</c>. Drop a new
/// <c>.json</c> file in there and a matching <c>.html</c> will appear under
/// <c>Output/</c> the next time you run the tests.
///
/// The library itself only ever emits a bare <c>&lt;table&gt;...&lt;/table&gt;</c> fragment.
/// To make the saved files openable in a browser, this helper wraps the fragment in a
/// minimal HTML shell <em>here in the sample code</em> — never inside the library.
/// </summary>
public class SampleOutputs
{
    private readonly ITestOutputHelper _output;

    // [CallerFilePath] is resolved at compile time, so these paths work regardless of
    // where bin/ ends up.
    private static readonly string TestProjectDir =
        Path.GetDirectoryName(GetThisFilePath())!;

    private static readonly string TestDataDir = Path.Combine(TestProjectDir, "TestData");
    private static readonly string OutputDir   = Path.Combine(TestProjectDir, "Output");

    private static string GetThisFilePath([CallerFilePath] string path = "") => path;

    public SampleOutputs(ITestOutputHelper output)
    {
        _output = output;
        Directory.CreateDirectory(OutputDir);
    }

    /// <summary>
    /// Discovers every JSON fixture in TestData/ at test-discovery time, so xUnit
    /// generates one theory case per file automatically.
    /// </summary>
    public static IEnumerable<object[]> AllFixtures()
    {
        if (!Directory.Exists(TestDataDir)) yield break;

        foreach (var path in Directory.GetFiles(TestDataDir, "*.json"))
        {
            yield return new object[] { Path.GetFileName(path) };
        }
    }

    [Theory]
    [MemberData(nameof(AllFixtures))]
    public void Sample_RenderEveryFixture(string fixtureFileName)
    {
        var json = File.ReadAllText(Path.Combine(TestDataDir, fixtureFileName));

        var options = new HtmlTableOptions
        {
            InlineStyles   = true,
            ShowRowNumbers = true,
            KeyTransform   = PrettifyCamelCase,
            NullText       = "—",
            // Use lenient parsing so messy fixtures (trailing commas, comments) still render.
            ParseOptions   = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling     = JsonCommentHandling.Skip,
            },
        };

        WriteSample(fixtureFileName, json, options);
    }

    /// <summary>
    /// One extra sample showing what the bare library output looks like with zero options —
    /// useful for comparing "styled" vs "what consumers actually get by default".
    /// </summary>
    [Fact]
    public void Sample_Subscriber_Default()
    {
        WriteSample(
            "Subscriber-Default.json",  // → Subscriber-Default.html
            JsonFiles.Read("Subscriber.json"),
            options: null);
    }

    // ---------- Helpers ----------

    private void WriteSample(string fixtureFileName, string json, HtmlTableOptions? options)
    {
        // The file contains exactly what JsonToHtmlConverter.Convert returns — no wrapper,
        // no <html>, no <title>, no <h2>. So you're inspecting the real library output.
        // Modern browsers render a bare <table> just fine when you open the file directly.
        var html = JsonToHtmlConverter.Convert(json, options);

        var path = Path.Combine(OutputDir, Path.ChangeExtension(fixtureFileName, ".html"));
        File.WriteAllText(path, html);

        _output.WriteLine($"Wrote sample: {path}");
    }

    private static string PrettifyCamelCase(string key) =>
        // userName  → "user Name", msisdnNumber → "msisdn Number"
        Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
}
