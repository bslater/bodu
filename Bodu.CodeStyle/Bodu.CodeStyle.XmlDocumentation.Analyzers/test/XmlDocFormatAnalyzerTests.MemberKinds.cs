// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatAnalyzerTests.MemberKinds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// Verifies that a misformatted comment on a type declaration triggers BODUXML001.
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
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(1, 1, 2, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a misformatted comment on a method declaration triggers BODUXML001.
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
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a misformatted comment on a field declaration triggers BODUXML001.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenDocOnFieldDeclarationMisformatted_ShouldReport()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a misformatted comment on an event declaration triggers BODUXML001.
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
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
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
            "    /// <summary>Foo.</summary>\r\n" +
            "    public int X;\r\n" +
            "    /// <summary>Bar.</summary>\r\n" +
            "    public int Y;\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 5, 4, 1)
                );
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
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
