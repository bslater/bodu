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
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 5, 4, 1)
                );

        await test.RunAsync(TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; }
}
