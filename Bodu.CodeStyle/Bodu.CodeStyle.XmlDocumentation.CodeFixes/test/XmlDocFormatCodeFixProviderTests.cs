// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatCodeFixProviderTests.cs" company="PlaceholderCompany">
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

[TestClass]
public partial class XmlDocFormatCodeFixProviderTests
{
    /// <summary>
    /// Verifies that the code fix replaces a single-line summary with the canonical multi-line form.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public async Task CodeFix_WhenSummarySingleLine_ShouldFormatToMultiline()
    {
        string source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>Foo.</summary>\r\n" +
            "    public int X { get; set; }\r\n" +
            "}\r\n";

        string expected =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// Foo.\r\n" +
            "    /// </summary>\r\n" +
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
}
