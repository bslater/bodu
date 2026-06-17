// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatCodeFixProviderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Bodu.CodeStyle.XmlDocumentation.Analyzers;
using Bodu.CodeStyle.XmlDocumentation.Analyzers.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.CodeFixes.Test;

[TestClass]
public partial class XmlDocFormatCodeFixProviderTests
{
    /// <summary>
    /// Verifies that when a single documentation comment has two misformatted tags (so the analyzer emits two
    /// per-tag diagnostics at the same trivia), Fix All reformats the whole comment once without producing
    /// overlapping or conflicting replacements.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenTwoTagsMisformattedInOneComment_FixAll_ShouldReformatOnce()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    /// <remarks>Bar.</remarks>\r\n" +
            "    public void M() { }\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    /// <remarks>\r\n" +
            "    /// Bar.\r\n" +
            "    /// </remarks>\r\n" +
            "    public void M() { }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary).WithSpan(3, 5, 5, 1));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocRemarks).WithSpan(3, 5, 5, 1));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that the code fix replaces a single-line summary with the canonical multi-line form.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task CodeFix_WhenSummarySingleLine_ShouldFormatToMultiline()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocSummary)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    public TestContext TestContext { get; set; }
}
