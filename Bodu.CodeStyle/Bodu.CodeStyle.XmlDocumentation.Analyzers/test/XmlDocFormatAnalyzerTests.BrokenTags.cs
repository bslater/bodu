// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocFormatAnalyzerTests.BrokenTags.cs" company="PlaceholderCompany">
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
    /// Verifies that prose spanning multiple <c>///</c> lines inside a single block tag is recognised as
    /// non-canonical and triggers BODUXML001.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenSummaryProseSplitAcrossLines_ShouldReport()
    {
        string source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>The thing.\r\n" +
            "    /// More text.</summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 8, 5, 1)
                .WithArguments(1));

        await test.RunAsync();
    }

    /// <summary>
    /// Verifies that an inline tag whose attributes are split across <c>///</c> lines is recognised as
    /// non-canonical and triggers BODUXML001.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenInlineTagAttributesSplitAcrossLines_ShouldReport()
    {
        string source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <summary>\r\n" +
            "    /// See <see cref=\"Sample\"\r\n" +
            "    ///      langword=\"null\" /> for details.\r\n" +
            "    /// </summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(DiagnosticDescriptors.XmlDocumentationFormatting)
                .WithSpan(3, 8, 7, 1)
                .WithArguments(1));

        await test.RunAsync();
    }

    /// <summary>
    /// Verifies that a doc comment with a tag whose name is split mid-token across <c>///</c> lines does NOT
    /// produce a diagnostic — the malformed-XML path leaves the comment untouched.
    /// </summary>
    [TestMethod]
    public async Task Analyze_WhenBlockTagNameSplitMidName_ShouldNotReport()
    {
        string source =
            "public sealed class Sample\r\n" +
            "{\r\n" +
            "    /// <sum\r\n" +
            "    /// mary>foo</summary>\r\n" +
            "    public int X;\r\n" +
            "}\r\n";

        CSharpAnalyzerTest<XmlDocFormatAnalyzer, MSTestVerifier> test = CreateTest(source);

        await test.RunAsync();
    }
}
