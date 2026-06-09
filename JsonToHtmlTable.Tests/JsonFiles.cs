using System;
using System.IO;

namespace JsonToHtmlTable.Tests;

/// <summary>
/// Loads JSON fixture files from <c>TestData/</c> (copied next to the test DLL at build time).
/// Keeps test bodies readable and lets you edit sample JSON without touching C# source.
/// </summary>
internal static class JsonFiles
{
    private static readonly string Root =
        Path.Combine(AppContext.BaseDirectory, "TestData");

    public static string Read(string filename)
    {
        var path = Path.Combine(Root, filename);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Test fixture not found: {path}. " +
                "Make sure the file is in JsonToHtmlTable.Tests/TestData/ and the csproj copies *.json to output.",
                path);
        }
        return File.ReadAllText(path);
    }
}
