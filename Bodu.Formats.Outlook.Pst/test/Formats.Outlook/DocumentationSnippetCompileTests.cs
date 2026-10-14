// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentationSnippetCompileTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Compiles the PST documentation examples that are explicitly opted in, so a code sample cannot silently drift
/// from the public API it documents.
/// </summary>
/// <remarks>
/// A fenced <c>csharp</c> block is compiled only when the line immediately preceding its opening fence is the sentinel
/// <c>&lt;!-- compile --&gt;</c>. Each opted-in block is compiled as the body of a method against the same assemblies
/// this test references. Illustrative fragments stay unmarked and are not compiled.
/// </remarks>
[TestClass]
public sealed partial class DocumentationSnippetCompileTests
{
    /// <summary>
    /// The sentinel that opts a fenced <c>csharp</c> block into compilation.
    /// </summary>
    private const string CompileSentinel = "<!-- compile -->";

    /// <summary>
    /// The documentation directories, relative to the repository root, whose opted-in snippets this test compiles.
    /// </summary>
    private static readonly string[] s_snippetDirectories =
    [
        Path.Combine("docs", "docs", "io-pst"),
    ];

    /// <summary>
    /// Verifies that every opted-in PST documentation snippet compiles against the current public API, with at least
    /// one snippet marked so the guard is demonstrably wired.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IoPstDocSnippets_WhenMarkedForCompile_ShouldCompile()
    {
        IReadOnlyList<string>? files = FindMarkedSnippetFiles();
        if (files is null)
        {
            Assert.Inconclusive("The repository root was not found from the test base directory.");
            return;
        }

        List<MetadataReference> references = LoadTrustedPlatformReferences();
        var failures = new StringBuilder();
        int marked = 0;

        foreach (string file in files)
        {
            foreach (string snippet in ExtractMarkedSnippets(File.ReadAllLines(file)))
            {
                marked++;
                List<Diagnostic> errors = Compile(snippet, references);
                if (errors.Count > 0)
                {
                    failures.AppendLine($"{Path.GetFileName(file)}:");
                    foreach (Diagnostic error in errors)
                        failures.AppendLine($"  {error.Id}: {error.GetMessage()}");
                }
            }
        }

        Assert.IsGreaterThan(0, marked, $"No '{CompileSentinel}' snippets were found; the compile guard is not wired to any example.");
        Assert.AreEqual(0, failures.Length, $"Documentation snippets failed to compile:{Environment.NewLine}{failures}");
    }

    /// <summary>
    /// Walks up from the test base directory to the repository root (identified by <c>bodu.slnx</c>) and returns the
    /// Markdown files of every directory in <see cref="s_snippetDirectories" /> beneath it.
    /// </summary>
    /// <returns>
    /// The full paths of the Markdown files, in directory then name order, or <see langword="null" /> when the repository
    /// root cannot be located.
    /// </returns>
    private static IReadOnlyList<string>? FindMarkedSnippetFiles()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "bodu.slnx")))
            {
                string root = dir.FullName;

                return
                [
                    .. s_snippetDirectories
                        .Select(relative => Path.Combine(root, relative))
                        .Where(Directory.Exists)
                        .SelectMany(static docs => Directory.EnumerateFiles(docs, "*.md").Order(StringComparer.Ordinal)),
                ];
            }
        }

        return null;
    }

    /// <summary>
    /// Yields each fenced <c>csharp</c> block whose opening fence is immediately preceded by the
    /// <see cref="CompileSentinel" />.
    /// </summary>
    /// <param name="lines">The markdown file's lines.</param>
    /// <returns>The opted-in snippet bodies.</returns>
    private static IEnumerable<string> ExtractMarkedSnippets(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() != "```csharp")
                continue;

            bool marked = i > 0 && lines[i - 1].Trim() == CompileSentinel;
            int start = i + 1;
            int end = start;
            while (end < lines.Length && lines[end].Trim() != "```")
                end++;

            if (marked)
                yield return string.Join(Environment.NewLine, lines[start..end]);

            i = end;
        }
    }

    /// <summary>
    /// Compiles a snippet as the body of a method and returns the error diagnostics. Leading <c>using</c> directive
    /// lines are dropped first - documentation snippets often show them for context, but the wrapper method already
    /// imports the covered namespaces.
    /// </summary>
    /// <param name="snippet">The snippet body (method-body statements, optionally preceded by using directives).</param>
    /// <param name="references">The metadata references to compile against.</param>
    /// <returns>The error-severity diagnostics, empty when the snippet compiles.</returns>
    private static List<Diagnostic> Compile(string snippet, List<MetadataReference> references)
    {
        snippet = string.Join(
            Environment.NewLine,
            snippet.Split('\n').Where(static line => !System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^using [A-Za-z0-9_.]+;$")));

        string source =
            "using System;" +
            "using System.Collections.Generic;" +
            "using System.IO;" +
            "using System.Linq;" +
            "using System.Threading;" +
            "using System.Threading.Tasks;" +
            "using System.Text;" +
            "using Bodu.IO.Pst;" +
            "using Bodu.Formats.Outlook;" +
            "namespace Bodu.DocSnippets { internal static class Snippet { internal static async Task RunAsync() {" +
            Environment.NewLine + snippet + Environment.NewLine +
            "} } }";

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "Bodu.DocSnippets",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return compilation.GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
    }

    /// <summary>
    /// Builds the metadata references from the runtime's trusted platform assemblies, which include this test's
    /// referenced Bodu assemblies.
    /// </summary>
    /// <returns>The metadata references for snippet compilation.</returns>
    private static List<MetadataReference> LoadTrustedPlatformReferences()
    {
        string trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        return trusted
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(static path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToList();
    }
}
