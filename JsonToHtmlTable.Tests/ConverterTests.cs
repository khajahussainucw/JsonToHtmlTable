using System.IO;
using System.Text.Json;
using JsonToHtmlTable;
using Xunit;

namespace JsonToHtmlTable.Tests;

public class ConverterTests
{
    // ---------- Object rendering ----------

    [Fact]
    public void Object_RendersAsTwoColumnTable()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("SubscriberMinimal.json"));

        Assert.Contains("<table class=\"json-to-html-table\">", html);
        Assert.Contains("<th>Key</th><th>Value</th>", html);
        Assert.Contains("<tr><td>subscriberId</td><td>SUB-100</td></tr>", html);
        Assert.Contains("<tr><td>planName</td><td>Prepaid Lite</td></tr>", html);
    }

    [Fact]
    public void Object_FullSubscriber_RendersNestedTagsAndAddress()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("Subscriber.json"));

        Assert.Contains("<td>subscriberId</td><td>SUB-100245</td>", html);
        Assert.Contains("<td>isActive</td><td>true</td>", html);
        // tags array becomes an inner table
        Assert.Contains("<td>tags</td><td><table", html);
        Assert.Contains(">5G<", html);
        // address object becomes an inner table
        Assert.Contains("<td>address</td><td><table", html);
        Assert.Contains("<td>circle</td><td>APAC-IN</td>", html);
    }

    [Fact]
    public void EmptyObject_RendersEmptyBody()
    {
        var html = JsonToHtmlConverter.Convert("{}");
        Assert.Contains("<tbody></tbody>", html);
    }

    // ---------- Array rendering ----------

    [Fact]
    public void ArrayOfObjects_RendersAsGrid()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("CdrsSmall.json"));

        Assert.Contains("<th>callId</th><th>durationSec</th>", html);
        Assert.Contains("<tr><td>C-1</td><td>42</td></tr>", html);
        Assert.Contains("<tr><td>C-2</td><td>17</td></tr>", html);
    }

    [Fact]
    public void ArrayOfObjects_FullCdrSet_RendersAllRecords()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("Cdrs.json"));

        Assert.Contains("<th>callId</th>", html);
        Assert.Contains("<th>billedAmount</th>", html);
        Assert.Contains(">C-001<", html);
        Assert.Contains(">C-002<", html);
        Assert.Contains(">C-003<", html);
        Assert.Contains(">6.85<", html);
    }

    [Fact]
    public void ArrayOfObjects_WithDifferentKeys_UsesUnionOfKeys()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("CdrsPartial.json"));

        Assert.Contains("<th>callId</th><th>billedAmount</th>", html);
        Assert.Contains("<tr><td>C-1</td><td></td></tr>", html);
        Assert.Contains("<tr><td></td><td>2.5</td></tr>", html);
    }

    [Fact]
    public void ArrayOfPrimitives_RendersAsSingleColumnTable()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("Tags.json"));

        Assert.Contains("<th>Value</th>", html);
        Assert.Contains("<tr><td>5G</td></tr>", html);
        Assert.Contains("<tr><td>VoLTE</td></tr>", html);
        Assert.Contains("<tr><td>Roaming</td></tr>", html);
    }

    [Fact]
    public void EmptyArray_RendersEmptyBody()
    {
        var html = JsonToHtmlConverter.Convert("[]");
        Assert.Contains("<tbody></tbody>", html);
    }

    [Fact]
    public void MixedArray_FallsBackToSingleColumn()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("MixedArray.json"));

        Assert.Contains("<tr><td>1</td></tr>", html);
        Assert.Contains("<tr><td>5G</td></tr>", html);
        // The object item gets rendered as an inner table.
        Assert.Contains("<table", html);
    }

    [Fact]
    public void ArrayOfArrays_RendersMatrixLike()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("Matrix.json"));

        // Outer table contains 3 rows, each cell holds an inner <table>.
        Assert.Contains("<tr><td><table", html);
        // All nine values present.
        for (var v = 1; v <= 9; v++)
        {
            Assert.Contains($">{v}<", html);
        }
    }

    // ---------- Top-level primitives ----------

    [Fact]
    public void TopLevelString_RendersEncodedValueOnly()
    {
        var html = JsonToHtmlConverter.Convert("\"hello world\"");
        Assert.Equal("hello world", html);
    }

    [Fact]
    public void TopLevelNumber_RendersAsRawText()
    {
        var html = JsonToHtmlConverter.Convert("123.45");
        Assert.Equal("123.45", html);
    }

    [Fact]
    public void TopLevelBoolean_RendersAsLiteral()
    {
        Assert.Equal("true", JsonToHtmlConverter.Convert("true"));
        Assert.Equal("false", JsonToHtmlConverter.Convert("false"));
    }

    [Fact]
    public void TopLevelNull_UsesNullText()
    {
        var html = JsonToHtmlConverter.Convert("null", new HtmlTableOptions { NullText = "—" });
        Assert.Equal("—", html);
    }

    // ---------- Nested ----------

    [Fact]
    public void NestedObject_RendersInnerTable()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("NestedPlan.json"));

        Assert.Contains("<tr><td>plan</td><td><table", html);
        Assert.Contains("<tr><td>name</td><td>Pro 599</td></tr>", html);
    }

    [Fact]
    public void DeeplyNested_RendersAllLevels()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("DeeplyNested.json"));
        Assert.Contains("leaf", html);
    }

    [Fact]
    public void ExceedingMaxDepth_TruncatesGracefully()
    {
        var json = "{\"a\":{\"b\":{\"c\":\"x\"}}}";
        var html = JsonToHtmlConverter.Convert(json, new HtmlTableOptions { MaxDepth = 1 });
        Assert.Contains("[max depth reached]", html);
    }

    // ---------- HTML safety / encoding ----------

    [Fact]
    public void StringValues_AreHtmlEncoded_NoXss()
    {
        var json = "{\"x\":\"<script>alert(1)</script>\"}";
        var html = JsonToHtmlConverter.Convert(json);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void PropertyNames_AreHtmlEncoded()
    {
        var json = "{\"<bad>\":1}";
        var html = JsonToHtmlConverter.Convert(json);
        Assert.DoesNotContain("<bad>", html);
        Assert.Contains("&lt;bad&gt;", html);
    }

    [Fact]
    public void SingleQuoteIsEncoded()
    {
        var html = JsonToHtmlConverter.Convert("{\"note\":\"O'Brien\"}");
        // HtmlEncoder escapes single quotes to numeric entity.
        Assert.DoesNotContain("O'Brien", html);
        Assert.Contains("O", html);
        Assert.Contains("Brien", html);
    }

    [Fact]
    public void UnicodeStrings_ArePreserved()
    {
        // Japanese: "Roaming" loanword (ローミング) — verifies UnicodeRanges.All passes through.
        var json = "{\"label\":\"\\u30ED\\u30FC\\u30DF\\u30F3\\u30B0\"}";
        var html = JsonToHtmlConverter.Convert(json);
        Assert.Contains("ローミング", html);
    }

    // ---------- Numbers / precision ----------

    [Fact]
    public void Numbers_PreserveRawPrecision()
    {
        var html = JsonToHtmlConverter.Convert("{\"billedAmount\":19.999}");
        Assert.Contains(">19.999<", html);
    }

    [Fact]
    public void LargeInt64_PreservedExactly()
    {
        var html = JsonToHtmlConverter.Convert("{\"imsi\":405854212345678}");
        Assert.Contains(">405854212345678<", html);
    }

    [Fact]
    public void ScientificNotation_PreservedAsRawText()
    {
        var html = JsonToHtmlConverter.Convert("{\"x\":1.5e10}");
        Assert.Contains(">1.5e10<", html);
    }

    [Fact]
    public void Booleans_RenderAsLowercase()
    {
        var html = JsonToHtmlConverter.Convert("{\"isActive\":true,\"isThrottled\":false}");
        Assert.Contains("<td>true</td>", html);
        Assert.Contains("<td>false</td>", html);
    }

    [Fact]
    public void Null_UsesConfiguredNullText()
    {
        var html = JsonToHtmlConverter.Convert(
            "{\"endTime\":null}",
            new HtmlTableOptions { NullText = "N/A" });
        Assert.Contains("<td>N/A</td>", html);
    }

    // ---------- Options ----------

    [Fact]
    public void TableCssClass_Null_OmitsClassAttribute()
    {
        var html = JsonToHtmlConverter.Convert(
            "{\"a\":1}",
            new HtmlTableOptions { TableCssClass = null });
        Assert.StartsWith("<table>", html);
    }

    [Fact]
    public void CustomHeaders_AreApplied()
    {
        var html = JsonToHtmlConverter.Convert(
            "{\"a\":1}",
            new HtmlTableOptions { KeyHeader = "Field", ValueHeader = "Data" });
        Assert.Contains("<th>Field</th><th>Data</th>", html);
    }

    [Fact]
    public void ShowRowNumbers_AddsIndexColumnToGrid()
    {
        var html = JsonToHtmlConverter.Convert(
            "[{\"callId\":\"C-1\"},{\"callId\":\"C-2\"}]",
            new HtmlTableOptions { ShowRowNumbers = true });
        Assert.Contains("<th>#</th>", html);
        Assert.Contains("<tr><td>1</td><td>C-1</td></tr>", html);
        Assert.Contains("<tr><td>2</td><td>C-2</td></tr>", html);
    }

    [Fact]
    public void ShowRowNumbers_AddsIndexColumnToPrimitiveArray()
    {
        var html = JsonToHtmlConverter.Convert(
            "[\"5G\",\"VoLTE\"]",
            new HtmlTableOptions { ShowRowNumbers = true });
        Assert.Contains("<tr><td>1</td><td>5G</td></tr>", html);
        Assert.Contains("<tr><td>2</td><td>VoLTE</td></tr>", html);
    }

    [Fact]
    public void KeyTransform_RewritesPropertyNames()
    {
        var html = JsonToHtmlConverter.Convert(
            "{\"subscriberId\":\"S-1\"}",
            new HtmlTableOptions { KeyTransform = k => k.ToUpperInvariant() });
        Assert.Contains("<td>SUBSCRIBERID</td>", html);
    }

    [Fact]
    public void KeyTransform_AppliesToGridColumnHeaders()
    {
        var html = JsonToHtmlConverter.Convert(
            "[{\"msisdn\":\"+91-9000000000\"}]",
            new HtmlTableOptions { KeyTransform = k => k + "!" });
        Assert.Contains("<th>msisdn!</th>", html);
    }

    [Fact]
    public void PrimitiveArrayHeader_CanBeOmitted()
    {
        var html = JsonToHtmlConverter.Convert(
            "[1,2,3]",
            new HtmlTableOptions { PrimitiveArrayHeader = null, ShowRowNumbers = false });
        Assert.DoesNotContain("<thead>", html);
    }

    [Fact]
    public void RenderArrayOfObjectsAsGrid_False_RendersEachItemAsRow()
    {
        var html = JsonToHtmlConverter.Convert(
            "[{\"a\":1},{\"a\":2}]",
            new HtmlTableOptions { RenderArrayOfObjectsAsGrid = false });
        Assert.DoesNotContain("<th>a</th>", html);
        Assert.Contains("<tr><td><table", html);
    }

    // ---------- Output is always a bare table fragment ----------

    [Fact]
    public void Output_AlwaysStartsWithTableTag()
    {
        var html = JsonToHtmlConverter.Convert("{\"a\":1}");
        Assert.StartsWith("<table", html);
    }

    [Fact]
    public void Output_AlwaysEndsWithClosingTableTag()
    {
        var html = JsonToHtmlConverter.Convert("{\"a\":1}");
        Assert.EndsWith("</table>", html);
    }

    [Fact]
    public void Output_NeverContainsDocumentChrome()
    {
        var html = JsonToHtmlConverter.Convert(JsonFiles.Read("Subscriber.json"));
        Assert.DoesNotContain("<!DOCTYPE", html);
        Assert.DoesNotContain("<html", html);
        Assert.DoesNotContain("<head", html);
        Assert.DoesNotContain("<body", html);
        Assert.DoesNotContain("<style", html);
        Assert.DoesNotContain("<title", html);
        Assert.DoesNotContain("<caption", html);
    }

    // ---------- InlineStyles ----------

    [Fact]
    public void InlineStyles_OmittedByDefault()
    {
        var html = JsonToHtmlConverter.Convert("{\"a\":1}");
        Assert.DoesNotContain("style=", html);
    }

    [Fact]
    public void InlineStyles_AddsStyleAttributesToTableAndCells()
    {
        var html = JsonToHtmlConverter.Convert(
            "{\"a\":1}",
            new HtmlTableOptions { InlineStyles = true });

        Assert.Contains("<table", html);
        Assert.Contains("border-collapse:collapse", html);
        Assert.Contains("<th style=\"", html);
        Assert.Contains("<td style=\"", html);
        Assert.Contains("border:1px solid", html);
    }

    [Fact]
    public void InlineStyles_AppliesRecursivelyToNestedTables()
    {
        var html = JsonToHtmlConverter.Convert(
            JsonFiles.Read("NestedPlan.json"),
            new HtmlTableOptions { InlineStyles = true });

        // Inner table also gets inline styles.
        var firstStyle = html.IndexOf("style=\"border-collapse");
        var lastStyle = html.LastIndexOf("style=\"border-collapse");
        Assert.NotEqual(firstStyle, lastStyle); // at least two style attributes => outer + inner table
    }

    // ---------- Lenient parsing ----------

    [Fact]
    public void DefaultParse_RejectsTrailingCommas()
    {
        Assert.ThrowsAny<JsonException>(() =>
            JsonToHtmlConverter.Convert(JsonFiles.Read("TrailingCommas.json")));
    }

    [Fact]
    public void OptIn_AllowsTrailingCommas()
    {
        var html = JsonToHtmlConverter.Convert(
            JsonFiles.Read("TrailingCommas.json"),
            new HtmlTableOptions
            {
                ParseOptions = new JsonDocumentOptions { AllowTrailingCommas = true }
            });
        Assert.Contains(">1<", html);
        Assert.Contains(">3<", html);
    }

    [Fact]
    public void OptIn_SkipsComments()
    {
        var html = JsonToHtmlConverter.Convert(
            JsonFiles.Read("WithComments.json"),
            new HtmlTableOptions
            {
                ParseOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip }
            });
        Assert.Contains("<td>callId</td>", html);
        Assert.Contains("<td>C-1</td>", html);
    }

    // ---------- Input overloads ----------

    [Fact]
    public void Convert_JsonDocument_Works()
    {
        using var doc = JsonDocument.Parse("{\"msisdn\":\"+91-9000000000\"}");
        var html = JsonToHtmlConverter.Convert(doc);
        Assert.Contains("<td>msisdn</td>", html);
    }

    [Fact]
    public void Convert_JsonElement_Works()
    {
        using var doc = JsonDocument.Parse("{\"msisdn\":\"+91-9000000000\"}");
        var html = JsonToHtmlConverter.Convert(doc.RootElement);
        Assert.Contains("<td>msisdn</td>", html);
    }

    [Fact]
    public void Convert_ToTextWriter_WritesSameOutput()
    {
        var json = "{\"a\":1}";
        var direct = JsonToHtmlConverter.Convert(json);

        using var sw = new StringWriter();
        JsonToHtmlConverter.Convert(json, sw);

        Assert.Equal(direct, sw.ToString());
    }

    // ---------- POCO ----------

    [Fact]
    public void ConvertObject_SerializesAndRenders()
    {
        var plan = new
        {
            PlanId = "PL-001",
            Name = "Prepaid Lite",
            Features = new[] { "5G", "VoLTE" }
        };
        var html = JsonToHtmlConverter.ConvertObject(plan);

        Assert.Contains("PlanId", html);
        Assert.Contains("PL-001", html);
        Assert.Contains("5G", html);
    }

    // ---------- TryConvert ----------

    [Fact]
    public void TryConvert_ValidJson_ReturnsTrue()
    {
        var ok = JsonToHtmlConverter.TryConvert("{\"a\":1}", out var html);
        Assert.True(ok);
        Assert.Contains("<td>a</td>", html);
    }

    [Fact]
    public void TryConvert_InvalidJson_ReturnsFalseAndEmptyHtml()
    {
        var ok = JsonToHtmlConverter.TryConvert("{not json", out var html);
        Assert.False(ok);
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void TryConvert_Null_ReturnsFalse()
    {
        var ok = JsonToHtmlConverter.TryConvert(null, out var html);
        Assert.False(ok);
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void TryConvert_EmptyString_ReturnsFalse()
    {
        var ok = JsonToHtmlConverter.TryConvert(string.Empty, out var html);
        Assert.False(ok);
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void TryConvert_WhitespaceOnly_ReturnsFalse()
    {
        var ok = JsonToHtmlConverter.TryConvert("   \t\n", out var html);
        Assert.False(ok);
        Assert.Equal(string.Empty, html);
    }

    // ---------- Guards ----------

    [Fact]
    public void Convert_NullString_Throws()
    {
        Assert.Throws<System.ArgumentNullException>(() =>
            JsonToHtmlConverter.Convert((string)null!));
    }

    [Fact]
    public void Convert_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => JsonToHtmlConverter.Convert("{not json"));
    }

    // ---------- Fixture infrastructure sanity check ----------

    [Fact]
    public void JsonFiles_Reads_AllFixtures()
    {
        // Catch any forgotten copy-to-output or renamed fixture early with a clear error.
        Assert.NotEmpty(JsonFiles.Read("Subscriber.json"));
        Assert.NotEmpty(JsonFiles.Read("SubscriberMinimal.json"));
        Assert.NotEmpty(JsonFiles.Read("Cdrs.json"));
        Assert.NotEmpty(JsonFiles.Read("CdrsSmall.json"));
        Assert.NotEmpty(JsonFiles.Read("CdrsPartial.json"));
        Assert.NotEmpty(JsonFiles.Read("Tags.json"));
        Assert.NotEmpty(JsonFiles.Read("MixedArray.json"));
        Assert.NotEmpty(JsonFiles.Read("Matrix.json"));
        Assert.NotEmpty(JsonFiles.Read("NestedPlan.json"));
        Assert.NotEmpty(JsonFiles.Read("DeeplyNested.json"));
        Assert.NotEmpty(JsonFiles.Read("TrailingCommas.json"));
        Assert.NotEmpty(JsonFiles.Read("WithComments.json"));
    }
}
