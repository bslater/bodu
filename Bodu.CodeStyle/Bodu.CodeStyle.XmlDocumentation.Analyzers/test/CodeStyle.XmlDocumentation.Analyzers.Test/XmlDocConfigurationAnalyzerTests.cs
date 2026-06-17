// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigurationAnalyzerTests.cs" company="Bodu Pty. Ltd.">
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

[TestClass]
public sealed class XmlDocConfigurationAnalyzerTests
{
    /// <summary>
    /// Verifies that a malformed <c>bodu.xmldocstyle.json</c> additional file produces a visible
    /// <c>BODU0001</c> diagnostic rather than being silently ignored.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenConfigJsonIsMalformed_ShouldReportBodu0001()
    {
        var test = new CSharpAnalyzerTest<XmlDocConfigurationAnalyzer, MSTestVerifier>
        {
            TestCode = "public sealed class Sample { }\r\n",
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.AdditionalFiles.Add(("bodu.xmldocstyle.json", "{ not valid json"));
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocConfigInvalid)
                .WithLocation("bodu.xmldocstyle.json", 1, 1)
                .WithArguments("Invalid JSON document for bodu.xmldocstyle.json."));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    /// <summary>
    /// Verifies that a valid <c>bodu.xmldocstyle.json</c> additional file produces no configuration diagnostic.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenConfigJsonIsValid_ShouldNotReport()
    {
        var test = new CSharpAnalyzerTest<XmlDocConfigurationAnalyzer, MSTestVerifier>
        {
            TestCode = "public sealed class Sample { }\r\n",
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        };

        test.TestState.AdditionalFiles.Add(("bodu.xmldocstyle.json", "{ \"maxLineLength\": 100 }"));

        await test.RunAsync(TestContext.CancellationTokenSource.Token);
    }

    public TestContext TestContext { get; set; } = null!;
}
