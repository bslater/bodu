// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatAnalyzerTests.PerTag.cs" company="Bodu Pty. Ltd.">
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
    /// Verifies that a misformatted <c>&lt;remarks&gt;</c> tag triggers <c>BODU1002</c>.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenRemarksMisformatted_ShouldReportBODU1002()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    /// <remarks>Long-form notes.</remarks>\r\n" +
            "    public void M() { }\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocRemarks)
                .WithSpan(3, 5, 7, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a misformatted self-closing <c>&lt;see /&gt;</c> (no trailing space before <c>/&gt;</c>)
    /// triggers <c>BODU1016</c> when the policy demands a trailing space, and does not falsely implicate the
    /// enclosing summary tag.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenSelfClosingSeeMissingTrailingSpace_ShouldReportBODU1016()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// See <see cref=\"Sample\"/> for details.\r\n" +
            "    /// </summary>\r\n" +
            "    public void M() { }\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSee)
                .WithSpan(3, 5, 6, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that the cross-cutting <c>BODU1040</c> descriptor exists with the expected identifier and
    /// is enabled by default. Cross-cutting changes are typically rare in well-formed input, so this test
    /// focuses on descriptor metadata rather than exercising the analyzer.
    /// </summary>
    [TestMethod]
    public void Descriptors_CrossCutting_ShouldExposeBODU1040Metadata()
    {
        Assert.AreEqual("BODU1040", DiagnosticDescriptors.XmlDocCrossCutting.Id);
        Assert.AreEqual("Documentation", DiagnosticDescriptors.XmlDocCrossCutting.Category);
        Assert.IsTrue(DiagnosticDescriptors.XmlDocCrossCutting.IsEnabledByDefault);
    }
}
