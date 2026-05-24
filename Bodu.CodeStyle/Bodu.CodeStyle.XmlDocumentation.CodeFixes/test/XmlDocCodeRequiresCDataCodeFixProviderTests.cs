// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocCodeRequiresCDataCodeFixProviderTests.cs" company="Bodu Pty. Ltd.">
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
public sealed class XmlDocCodeRequiresCDataCodeFixProviderTests
{
    /// <summary>
    /// Verifies that the fix wraps a single-line text-only <c>&lt;code&gt;</c> body in a multi-line
    /// <c>&lt;![CDATA[…]]&gt;</c> section: the opener and closer each sit on their own line flush against
    /// the <c>///</c> doc-comment prefix, and the existing body content is placed between them with the
    /// standard <c>/// </c> prefix.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task CodeFix_WhenCodeContainsOnlyText_ShouldWrapBodyInCData()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code>var x = 5;</code>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    /// var x = 5;\r\n" +
            "    ///]]>\r\n" +
            "    /// </code>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocCodeRequiresCDataAnalyzer, XmlDocCodeRequiresCDataCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocCodeRequiresCData)
                .WithSpan(3, 9, 3, 15));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that the fix replaces an empty <c>&lt;code&gt;&lt;/code&gt;</c> element with a multi-line
    /// form containing an empty <c>&lt;![CDATA[…]]&gt;</c> block whose opener and closer each sit on
    /// their own line flush against the <c>///</c> doc-comment prefix.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenCodeIsEmpty_ShouldInsertEmptyCData()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code></code>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    ///]]>\r\n" +
            "    /// </code>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocCodeRequiresCDataAnalyzer, XmlDocCodeRequiresCDataCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocCodeRequiresCData)
                .WithSpan(3, 9, 3, 15));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that the fix rewrites a self-closing <c>&lt;code /&gt;</c> element to a multi-line
    /// non-empty form whose body is an empty <c>&lt;![CDATA[…]]&gt;</c> block, with the opener and
    /// closer each on their own line flush against the <c>///</c> doc-comment prefix.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenCodeIsSelfClosing_ShouldRewriteToEmptyCData()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code />\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    ///]]>\r\n" +
            "    /// </code>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocCodeRequiresCDataAnalyzer, XmlDocCodeRequiresCDataCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocCodeRequiresCData)
                .WithSpan(3, 9, 3, 17));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that the fix removes the stray space between <c>///</c> and <c>&lt;![CDATA[</c> on a
    /// multi-line <c>&lt;code&gt;</c> body.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenCDataHasSpaceAfterDocPrefix_ShouldRemoveSpace()
    {
        var source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code>\r\n" +
            "    /// <![CDATA[\r\n" +
            "    /// var x = 5;\r\n" +
            "    /// ]]>\r\n" +
            "    /// </code>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <code>\r\n" +
            "    ///<![CDATA[\r\n" +
            "    /// var x = 5;\r\n" +
            "    /// ]]>\r\n" +
            "    /// </code>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocCodeRequiresCDataAnalyzer, XmlDocCodeRequiresCDataCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocCodeRequiresCData)
                .WithSpan(4, 9, 6, 12));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    public TestContext TestContext { get; set; } = null!;
}
