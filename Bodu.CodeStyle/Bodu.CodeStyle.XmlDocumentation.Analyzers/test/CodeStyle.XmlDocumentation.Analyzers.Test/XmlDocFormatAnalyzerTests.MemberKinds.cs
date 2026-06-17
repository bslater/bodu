// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatAnalyzerTests.MemberKinds.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Test;

public partial class XmlDocFormatAnalyzerTests
{
    /// <summary>
    /// Verifies that a misformatted comment on a type declaration triggers BODU1001.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenDocOnTypeDeclarationMisformatted_ShouldReport()
    {
        var source =
            "/// <summary>Foo.</summary>\r\n" +
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(1, 1, 2, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a misformatted comment on a method declaration triggers BODU1001.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenDocOnMethodDeclarationMisformatted_ShouldReport()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    public void M() { }\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a short single-line <c>&lt;summary&gt;</c> on a field is accepted as-is and does not
    /// trigger BODU1001 — a field summary is canonical on one line when it fits the width.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenFieldSummaryIsShortSingleLine_ShouldNotReport()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Holds the count.</summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        await CreateTest(source).RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a multi-line <c>&lt;summary&gt;</c> on a field is reported (BODU1001) so it can be
    /// collapsed back onto a single line.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenFieldSummaryIsMultiLine_ShouldReport()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Holds the count.\r\n" +
            "    /// </summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 6, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a misformatted comment on an event declaration triggers BODU1001.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenDocOnEventDeclarationMisformatted_ShouldReport()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Fires when X happens.</summary>\r\n" +
            "    public event System.EventHandler? E;\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that two misformatted comments produce two diagnostics in the same file.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenTwoMisformattedDocsInOneFile_ShouldReportBoth()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo bar.</summary>\r\n" +
            "    public void M() { }\r\n" +
            "    /// <summary>Baz qux.</summary>\r\n" +
            "    public void N() { }\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(5, 5, 6, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that an empty file produces no diagnostics.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenSourceIsEmpty_ShouldReportNothing()
    {
        await CreateTest(string.Empty).RunAsync(TestContext.CancellationTokenSource.Token);
    }
}
