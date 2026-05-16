// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatCodeFixProviderTests.Preservation.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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

public partial class XmlDocFormatCodeFixProviderTests
{
    /// <summary>
    /// Verifies that attributes preceding a documented member survive the code fix.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenMemberHasAttribute_ShouldPreserveAttribute()
    {
        string source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    [System.Obsolete]\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        string expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    [System.Obsolete]\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier> test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 8, 4, 1)
                .WithArguments(1));

        await test.RunAsync();
    }

    /// <summary>
    /// Verifies that multiple misformatted doc comments in the same file are each fixed when Fix All runs.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenMultipleDocsMisformatted_ShouldFixAll()
    {
        string source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    public int X;\r\n" +
            "    /// <summary>Bar.</summary>\r\n" +
            "    public int Y;\r\n" +
            "}\r\n";

        string expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    public int X;\r\n" +
            "    /// <summary>\r\n" +
            "    /// Bar.\r\n" +
            "    /// </summary>\r\n" +
            "    public int Y;\r\n" +
            "}\r\n";

        CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier> test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = expected,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 8, 4, 1)
                .WithArguments(1));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(5, 8, 6, 1)
                .WithArguments(1));

        await test.RunAsync();
    }

    /// <summary>
    /// Verifies that the code fix does not change a canonical doc comment when run on a file that already
    /// satisfies the policy.
    /// </summary>
    [TestMethod]
    public async Task CodeFix_WhenAlreadyCanonical_ShouldMakeNoChanges()
    {
        string source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier> test =
            new CSharpCodeFixTest<XmlDocFormatAnalyzer, XmlDocFormatCodeFixProvider, MSTestVerifier>
            {
                TestCode = source,
                FixedCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

        await test.RunAsync();
    }
}
