// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentationSnippetCompileTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bodu.Text.Ini;

/// <summary>
/// Compiles the INI documentation guide examples that are explicitly opted in, so a code sample cannot silently
/// drift from the public API it documents — the class of error (a renamed or removed member in a shown snippet) that
/// prose review misses.
/// </summary>
/// <remarks>
/// A fenced <c>csharp</c> block is compiled only when the line immediately preceding its opening fence is the sentinel
/// <c>&lt;!-- compile --&gt;</c>. Each opted-in block is compiled as the body of a method against the same assemblies
/// this test references, so an opted-in block must be method-body statements that resolve against those references.
/// Illustrative fragments stay unmarked and are not compiled.
/// </remarks>
[TestClass]
public sealed class DocumentationSnippetCompileTests
{
    /// <summary>
    /// The sentinel that opts a fenced <c>csharp</c> block into compilation.
    /// </summary>
    private const string CompileSentinel = "<!-- compile -->";

    /// <summary>
    /// The guide paths this test covers, relative to the repository root. A directory contributes every markdown file
    /// directly beneath it.
    /// </summary>
    private static readonly string[] GuidePaths =
    [
        "docs/guides/formats/ini.md",
    ];

    /// <summary>
    /// Guide file names inside <see cref="GuidePaths" /> that another project's guard covers instead, because their
    /// snippets need a package this test does not reference.
    /// </summary>
    private static readonly string[] ExcludedFileNames =
    [

    ];

    /// <summary>
    /// Verifies that every opted-in INI guide snippet compiles against the current public API, with at least one
    /// snippet marked so the guard is demonstrably wired.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void IniGuideSnippets_WhenMarkedForCompile_ShouldCompile()
    {
        string? root = FindRepositoryRoot();
        if (root is null)
        {
            Assert.Inconclusive("The repository root was not found from the test base directory.");
            return;
        }

        List<MetadataReference> references = LoadTrustedPlatformReferences();
        var failures = new StringBuilder();
        int marked = 0;

        foreach (string file in EnumerateGuideFiles(root))
        {
            foreach (string snippet in ExtractMarkedSnippets(File.ReadAllLines(file)))
            {
                marked++;
                List<Diagnostic> errors = Compile(snippet, references);
                if (errors.Count > 0)
                {
                    failures.AppendLine($"{Path.GetFileName(file)} — snippet starting \"{FirstCodeLine(snippet)}\":");
                    foreach (Diagnostic error in errors)
                        failures.AppendLine($"  {error.Id}: {error.GetMessage()}");
                }
            }
        }

        Assert.IsGreaterThan(0, marked, $"No '{CompileSentinel}' snippets were found under the covered guide paths; the compile guard is not wired to any example.");
        Assert.AreEqual(0, failures.Length, $"Documentation snippets failed to compile:{Environment.NewLine}{failures}");
    }

    /// <summary>
    /// Returns the snippet's first line that carries code, for naming a failing snippet in the assertion message.
    /// </summary>
    /// <param name="snippet">The snippet body.</param>
    /// <returns>The first non-blank, non-comment line, trimmed, or the empty string.</returns>
    private static string FirstCodeLine(string snippet) =>
        snippet.Split('\n')
            .Select(static line => line.Trim())
            .FirstOrDefault(static line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            ?? string.Empty;

    /// <summary>
    /// Walks up from the test base directory to the repository root, identified by <c>bodu.slnx</c>.
    /// </summary>
    /// <returns>The repository root, or <see langword="null" /> when it cannot be located.</returns>
    private static string? FindRepositoryRoot()
    {
        for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "bodu.slnx")))
                return dir.FullName;
        }

        return null;
    }

    /// <summary>
    /// Expands <see cref="GuidePaths" /> into the markdown files to scan.
    /// </summary>
    /// <param name="root">The repository root.</param>
    /// <returns>The markdown files that exist.</returns>
    private static IEnumerable<string> EnumerateGuideFiles(string root)
    {
        foreach (string relative in GuidePaths)
        {
            string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

            if (Directory.Exists(path))
            {
                foreach (string file in Directory.EnumerateFiles(path, "*.md"))
                {
                    if (!ExcludedFileNames.Contains(Path.GetFileName(file)))
                        yield return file;
                }
            }
            else if (File.Exists(path))
            {
                yield return path;
            }
        }
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
    /// Compiles a snippet as the body of a method and returns the error diagnostics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A guide snippet usually opens with the <c>using</c> directives a reader would need. Those are lifted out of the
    /// snippet and placed with the wrapper's own directives, rather than dropped, so a snippet that declares an alias
    /// (<c>using SysComplex = System.Numerics.Complex;</c>) or a static import compiles as written. A <c>using</c>
    /// <i>statement</i> — <c>using var</c>, or <c>using (…)</c> — is left in the body where it belongs.
    /// </para>
    /// <para>
    /// The snippet is then tried two ways: as the body of a method, which most guide examples are, and as
    /// namespace-level declarations, which is what an example introducing a type looks like. Whichever reading
    /// produces fewer errors is reported, so a POCO or converter shown in a guide is held to the same standard as a
    /// sequence of statements.
    /// </para>
    /// </remarks>
    /// <param name="snippet">The snippet body (method-body statements, optionally preceded by using directives).</param>
    /// <param name="references">The metadata references to compile against.</param>
    /// <returns>The error-severity diagnostics, empty when the snippet compiles.</returns>
    private static List<Diagnostic> Compile(string snippet, List<MetadataReference> references)
    {
        var hoisted = new List<string>();
        var body = new List<string>();
        foreach (string line in snippet.Split('\n'))
        {
            if (IsUsingDirective(line.Trim()))
                hoisted.Add(line.Trim());
            else
                body.Add(line);
        }

        snippet = string.Join(Environment.NewLine, body);
        string imports =
            string.Concat(hoisted) +
            "using System;" +
            "using System.Collections.Generic;" +
            "using System.IO;" +
            "using System.Linq;" +
            "using System.Threading;" +
            "using System.Threading.Tasks;" +
            "using Bodu.Text.Serialization;" +
            "using Bodu.Text.Ini;" +
            "using Bodu.Text.Ini.Document;" +
            "using Bodu.Text.Ini.Nodes;" +
            "using Bodu.Text.Ini.Reader;" +
            "using Bodu.Text.Ini.Writer;" +
            string.Empty;

        // Most snippets are statements; some are a type the guide is introducing (a POCO, a converter). Try the
        // statement reading first and fall back to the declaration reading, reporting whichever fits better.
        List<Diagnostic> asStatements = CompileSource(
            imports + "namespace Bodu.DocSnippets { internal static class Snippet { internal static async Task RunAsync() {"
            + Environment.NewLine + snippet + Environment.NewLine + "} } }",
            references);

        if (asStatements.Count == 0)
            return asStatements;

        List<Diagnostic> asDeclarations = CompileSource(
            imports + "namespace Bodu.DocSnippets {" + Environment.NewLine + snippet + Environment.NewLine + "}",
            references);

        return asDeclarations.Count < asStatements.Count ? asDeclarations : asStatements;
    }

    /// <summary>
    /// Compiles one candidate source text and returns its error diagnostics.
    /// </summary>
    /// <param name="source">The complete compilation unit.</param>
    /// <param name="references">The metadata references to compile against.</param>
    /// <returns>The error-severity diagnostics, empty when the source compiles.</returns>
    private static List<Diagnostic> CompileSource(string source, List<MetadataReference> references)
    {
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
    /// Determines whether a line is a <c>using</c> <i>directive</i> — a plain, static, or alias import — as opposed to
    /// a <c>using</c> statement, which must stay in the method body.
    /// </summary>
    /// <param name="line">The trimmed source line.</param>
    /// <returns><see langword="true" /> when the line is a using directive; otherwise <see langword="false" />.</returns>
    private static bool IsUsingDirective(string line)
    {
        if (!line.StartsWith("using ", StringComparison.Ordinal) || !line.EndsWith(";", StringComparison.Ordinal))
            return false;

        // "using var x = ...;" and "using (...)" are statements that happen to end in a semicolon.
        string rest = line["using ".Length..].TrimStart();
        if (rest.StartsWith("var ", StringComparison.Ordinal) || rest.StartsWith("(", StringComparison.Ordinal))
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(
            line,
            @"^using\s+(static\s+)?[A-Za-z_][A-Za-z0-9_.]*(\s*=\s*[A-Za-z_][A-Za-z0-9_.<>,\[\]\s]*)?\s*;$");
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
