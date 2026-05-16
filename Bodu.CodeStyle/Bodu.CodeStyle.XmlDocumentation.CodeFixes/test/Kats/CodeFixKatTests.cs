// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CodeFixKatTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Bodu.CodeStyle.TestData;
using Bodu.CodeStyle.XmlDocumentation;
using Bodu.CodeStyle.XmlDocumentation.Analyzers;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Configuration;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes.Test.Kats;

[TestClass]
public sealed class CodeFixKatTests
{
    /// <summary>
    /// Returns the code-fix, configuration, and safety KATs that the currently-implemented code fix can run.
    /// KATs that require future features (preprocessor-aware layout detection) are filtered with
    /// <see cref="Assert.Inconclusive(string)" /> at run time.
    /// </summary>
    public static IEnumerable<object[]> Kats()
    {
        foreach (var row in BoduCodeStyleKats.Data(CodeStyleKatArea.XmlDocumentationCodeFix))
        {
            yield return row;
        }

        foreach (var row in BoduCodeStyleKats.Data(CodeStyleKatArea.Configuration))
        {
            yield return row;
        }

        foreach (var row in BoduCodeStyleKats.Data(CodeStyleKatArea.Safety))
        {
            yield return row;
        }
    }

    /// <summary>
    /// Verifies that each code-fix KAT applies the analyzer + code fix and produces the expected source.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Kats), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetDisplayName))]
    public async Task CodeFix_ShouldMatchKatExpectation(CodeStyleKat kat)
    {
        if (kat is null) throw new ArgumentNullException(nameof(kat));

        var input = NormalizeToCrlf(kat.Input);
        var expected = NormalizeToCrlf(kat.Expected);

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        // Use the KAT-supplied file path when non-default so the analyzer's generated-file filename heuristics
        // can fire. For the default "Test0.cs" we rely on the testing kit's TestCode/FixedCode auto-wiring.
        if (string.Equals(kat.FilePath, "Test0.cs", StringComparison.Ordinal))
        {
            test.TestCode = input;
            test.FixedCode = expected;
        }
        else
        {
            test.TestState.Sources.Add((kat.FilePath, input));
            test.FixedState.Sources.Add((kat.FilePath, expected));
        }

        if (!string.IsNullOrEmpty(kat.AnalyzerConfig))
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", kat.AnalyzerConfig!));
            test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", kat.AnalyzerConfig!));
        }

        foreach (CodeStyleKatAdditionalFile file in kat.Files)
        {
            test.TestState.AdditionalFiles.Add((file.Path, file.Content));
            test.FixedState.AdditionalFiles.Add((file.Path, file.Content));
        }

        XmlDocFormatOptions options = ResolveOptions(kat);
        foreach (DiagnosticResult expectedDiagnostic in ComputeExpectedDiagnostics(input, options, kat.FilePath))
        {
            test.ExpectedDiagnostics.Add(expectedDiagnostic);
        }

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Returns the MSTest display name for the supplied KAT row.
    /// </summary>
    public static string GetDisplayName(MethodInfo methodInfo, object?[] data) =>
        BoduCodeStyleKats.GetDisplayName(methodInfo, data!);

    private static XmlDocFormatOptions ResolveOptions(CodeStyleKat kat)
    {
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        foreach (CodeStyleKatAdditionalFile file in kat.Files)
        {
            if (System.IO.Path.GetFileName(file.Path).Equals("bodu.xmldocstyle.json", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    options = Bodu.CodeStyle.XmlDocumentation.Configuration.XmlDocConfigJsonReader.Read(file.Content);
                }
                catch
                {
                    // Fall back to defaults.
                }
            }
        }

        if (!string.IsNullOrEmpty(kat.AnalyzerConfig))
        {
            var max = ParseMaxLineLength(kat.AnalyzerConfig!);
            if (max.HasValue)
            {
                options = options.WithMaxLineLength(max.Value);
            }
        }

        return options;
    }

    private static int? ParseMaxLineLength(string analyzerConfig)
    {
        foreach (var line in analyzerConfig.Split('\n'))
        {
            var trimmed = line.Trim();
            const string Key = "bodu_xmldoc_max_line_length";
            var eq = trimmed.IndexOf('=');
            if (eq <= 0) continue;
            var keyPart = trimmed.Substring(0, eq).Trim();
            if (!string.Equals(keyPart, Key, StringComparison.Ordinal)) continue;
            var valuePart = trimmed.Substring(eq + 1).Trim();
            if (int.TryParse(valuePart, out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }

        return null;
    }

    private static IEnumerable<DiagnosticResult> ComputeExpectedDiagnostics(string source, XmlDocFormatOptions options, string filePath)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(SourceText.From(source), path: filePath);

        if (IsGeneratedFilePath(filePath))
        {
            yield break;
        }

        SyntaxNode root = tree.GetRoot();
        var formatter = new XmlDocFormatter();

        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                continue;
            }

            var baseIndent = ResolveBaseIndent(trivia);
            var context = new XmlDocFormatContext(baseIndent, "\r\n", XmlDocMemberKindHint.Unknown);
            XmlDocFormatResult result = formatter.FormatTrivia(trivia.ToFullString(), context, options);
            if (!result.Changed)
            {
                continue;
            }

            FileLinePositionSpan span = trivia.SyntaxTree!.GetLineSpan(trivia.FullSpan);
            ImmutableArray<XmlDocFormattingChange> changes = result.Changes;
            if (changes.IsDefaultOrEmpty)
            {
                yield return new DiagnosticResult(DiagnosticDescriptors.XmlDocCrossCutting)
                    .WithSpan(
                        span.StartLinePosition.Line + 1,
                        span.StartLinePosition.Character + 1,
                        span.EndLinePosition.Line + 1,
                        span.EndLinePosition.Character + 1);
                continue;
            }

            foreach (XmlDocFormattingChange change in changes)
            {
                DiagnosticDescriptor descriptor = DiagnosticDescriptors.ForTag(change.TagName);
                yield return new DiagnosticResult(descriptor)
                    .WithSpan(
                        span.StartLinePosition.Line + 1,
                        span.StartLinePosition.Character + 1,
                        span.EndLinePosition.Line + 1,
                        span.EndLinePosition.Character + 1);
            }
        }
    }

    private static bool IsGeneratedFilePath(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        var fileName = System.IO.Path.GetFileName(filePath);
        return fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase) ||
               fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveBaseIndent(SyntaxTrivia trivia)
    {
        SyntaxToken token = trivia.Token;
        SyntaxTriviaList leading = token.LeadingTrivia;
        var index = leading.IndexOf(trivia);
        if (index <= 0) return string.Empty;

        SyntaxTrivia previous = leading[index - 1];
        if (!previous.IsKind(SyntaxKind.WhitespaceTrivia)) return string.Empty;

        return previous.ToFullString();
    }

    private static string NormalizeToCrlf(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        return normalized.EndsWith("\r\n", StringComparison.Ordinal) ? normalized : normalized + "\r\n";
    }

    public TestContext TestContext { get; set; }
}
