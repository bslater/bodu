// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnalyzerKatTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Bodu.CodeStyle.TestData;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Test.Kats;

[TestClass]
public sealed class AnalyzerKatTests
{
    /// <summary>
    /// Returns the known-answer fixtures for the XML documentation analyzer (KATs whose Id is
    /// supported by the currently-implemented analyzer surface).
    /// </summary>
    public static IEnumerable<object[]> Kats()
    {
        foreach (object[] row in BoduCodeStyleKats.Data(CodeStyleKatArea.XmlDocumentationAnalyzer))
        {
            yield return row;
        }
    }

    /// <summary>
    /// Verifies that each analyzer KAT produces exactly the expected diagnostics under
    /// <see cref="XmlDocFormatAnalyzer" />, including configured additional files and editor-config overrides.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Kats), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetDisplayName))]
    public async Task Analyze_ShouldMatchKatExpectation(CodeStyleKat kat)
    {
        if (kat is null) throw new ArgumentNullException(nameof(kat));

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test =
            new CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier>
            {
                TestCode = NormalizeToCrlf(kat.Input),
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        if (!string.IsNullOrEmpty(kat.AnalyzerConfig))
        {
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", kat.AnalyzerConfig!));
        }

        foreach (CodeStyleKatAdditionalFile file in kat.Files)
        {
            test.TestState.AdditionalFiles.Add((file.Path, file.Content));
        }

        foreach (CodeStyleKatDiagnostic expected in kat.Diagnostics)
        {
            DiagnosticDescriptor descriptor = ResolveDescriptor(expected.Id);
            if (descriptor is null)
            {
                Assert.Inconclusive($"KAT {kat.Id} references unsupported diagnostic id '{expected.Id}'.");
                return;
            }

            DiagnosticResult result = new DiagnosticResult(descriptor);
            if (expected.Line.HasValue && expected.Column.HasValue)
            {
                result = result.WithLocation(expected.Line.Value, expected.Column.Value);
            }

            test.ExpectedDiagnostics.Add(result);
        }

        await test.RunAsync();
    }

    /// <summary>
    /// Returns the MSTest display name for the supplied KAT row.
    /// </summary>
    public static string GetDisplayName(MethodInfo methodInfo, object?[] data) =>
        BoduCodeStyleKats.GetDisplayName(methodInfo, data!);

    private static DiagnosticDescriptor ResolveDescriptor(string id) =>
        id switch
        {
            CodeStyleKatConstants.XmlDocumentationDiagnosticId => DiagnosticDescriptors.XmlDocumentationFormatting,
            _ => null!,
        };

    private static string NormalizeToCrlf(string text)
    {
        string normalized = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        return normalized.EndsWith("\r\n", StringComparison.Ordinal) ? normalized : normalized + "\r\n";
    }
}
