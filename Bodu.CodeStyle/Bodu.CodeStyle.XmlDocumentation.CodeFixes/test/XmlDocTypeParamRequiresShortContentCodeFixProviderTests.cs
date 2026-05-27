// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocTypeParamRequiresShortContentCodeFixProviderTests.cs" company="Bodu Pty. Ltd.">
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
public sealed class XmlDocTypeParamRequiresShortContentCodeFixProviderTests
{
    /// <summary>
    /// Verifies that when a type-level <c>&lt;remarks&gt;</c> block already exists, the fix appends the
    /// relocated prose as a new <c>&lt;para&gt;</c> at the end of the remarks body and shortens the
    /// <c>&lt;typeparam&gt;</c> to its first sentence.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task CodeFix_WhenRemarksExists_ShouldAppendParaToExistingRemarks()
    {
        var source =
            "/// <summary>Sample.</summary>\r\n" +
            "/// <typeparam name=\"T\">The element type. Some additional explanatory prose that should be relocated to a remarks block.</typeparam>\r\n" +
            "/// <remarks>\r\n" +
            "/// <para>Existing remark.</para>\r\n" +
            "/// </remarks>\r\n" +
            "public sealed class Sample<T>\r\n" +
            "{\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "/// <summary>Sample.</summary>\r\n" +
            "/// <typeparam name=\"T\">The element type.</typeparam>\r\n" +
            "/// <remarks>\r\n" +
            "/// <para>Existing remark.</para>\r\n" +
            "/// <para>\r\n" +
            "/// Some additional explanatory prose that should be relocated to a remarks block.\r\n" +
            "/// </para>\r\n" +
            "/// </remarks>\r\n" +
            "public sealed class Sample<T>\r\n" +
            "{\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocTypeParamRequiresShortContentAnalyzer, XmlDocTypeParamRequiresShortContentCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocTypeParamRequiresShortContent)
                .WithSpan(2, 5, 2, 25));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that when no type-level <c>&lt;remarks&gt;</c> block exists, the fix synthesizes a fresh
    /// <c>&lt;remarks&gt;&lt;para&gt;…&lt;/para&gt;&lt;/remarks&gt;</c> block and inserts it immediately
    /// after the <c>&lt;typeparam&gt;</c> line, preserving the canonical summary → typeparam → remarks
    /// ordering.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenRemarksMissing_ShouldInsertNewRemarksAfterTypeParam()
    {
        var source =
            "/// <summary>Sample.</summary>\r\n" +
            "/// <typeparam name=\"T\">The element type. Some additional explanatory prose that should be relocated to a remarks block.</typeparam>\r\n" +
            "public sealed class Sample<T>\r\n" +
            "{\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var expected =
            "/// <summary>Sample.</summary>\r\n" +
            "/// <typeparam name=\"T\">The element type.</typeparam>\r\n" +
            "/// <remarks>\r\n" +
            "/// <para>\r\n" +
            "/// Some additional explanatory prose that should be relocated to a remarks block.\r\n" +
            "/// </para>\r\n" +
            "/// </remarks>\r\n" +
            "public sealed class Sample<T>\r\n" +
            "{\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        var test =
            new CSharpCodeFixTest<XmlDocTypeParamRequiresShortContentAnalyzer, XmlDocTypeParamRequiresShortContentCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocTypeParamRequiresShortContent)
                .WithSpan(2, 5, 2, 25));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    public TestContext TestContext { get; set; } = null!;
}
