// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrderingKatTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Bodu.CodeStyle.Ordering.Analyzers;
using Bodu.CodeStyle.Ordering.Analyzers.Diagnostics;
using Bodu.CodeStyle.TestData;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.Ordering.CodeFixes.Test.Kats;

[TestClass]
public sealed class OrderingKatTests
{
    /// <summary>
    /// Returns the known-answer fixtures for the member-ordering analyzer.
    /// </summary>
    public static IEnumerable<object[]> Kats() => BoduCodeStyleKats.Data(CodeStyleKatArea.MemberOrdering);

    /// <summary>
    /// Verifies that each member-ordering KAT reports the expected BODUORD001 diagnostics and that the code
    /// fix produces the expected reordered source.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Kats), DynamicDataSourceType.Method, DynamicDataDisplayName = nameof(GetDisplayName))]
    public async Task Ordering_ShouldMatchKatExpectation(CodeStyleKat kat)
    {
        if (kat is null) throw new ArgumentNullException(nameof(kat));

        string input = NormalizeToCrlf(kat.Input);
        string expected = NormalizeToCrlf(kat.Expected);

        CSharpCodeFixTest<MemberOrderAnalyzer, MemberOrderCodeFixProvider, MSTestVerifier> test =
            new CSharpCodeFixTest<MemberOrderAnalyzer, MemberOrderCodeFixProvider, MSTestVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                CompilerDiagnostics = CompilerDiagnostics.None,
            };

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

        foreach (CodeStyleKatDiagnostic expectedDiagnostic in kat.Diagnostics)
        {
            if (!string.Equals(expectedDiagnostic.Id, CodeStyleKatConstants.MemberOrderingDiagnosticId, StringComparison.Ordinal))
            {
                continue;
            }

            DiagnosticResult result = new DiagnosticResult(DiagnosticDescriptors.MemberOrdering);
            if (expectedDiagnostic.Line.HasValue && expectedDiagnostic.Column.HasValue)
            {
                result = result.WithLocation(expectedDiagnostic.Line.Value, expectedDiagnostic.Column.Value);
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

    private static string NormalizeToCrlf(string text)
    {
        string normalized = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        return normalized.EndsWith("\r\n", StringComparison.Ordinal) ? normalized : normalized + "\r\n";
    }
}
