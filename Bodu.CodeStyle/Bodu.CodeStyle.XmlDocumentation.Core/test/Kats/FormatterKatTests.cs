// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatterKatTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using Bodu.CodeStyle.TestData;
using Bodu.CodeStyle.XmlDocumentation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.CodeStyle.XmlDocumentation.Test.Kats;

[TestClass]
public sealed class FormatterKatTests
{
    /// <summary>
    /// Returns the known-answer fixtures for the XML documentation formatter.
    /// </summary>
    public static IEnumerable<object[]> Kats() => BoduCodeStyleKats.Data(CodeStyleKatArea.XmlDocumentationFormatting);

    /// <summary>
    /// Verifies that each formatter KAT produces the expected output, honours the <c>ExpectedChanged</c> flag,
    /// recognises malformed-XML inputs, and is idempotent on a second pass.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Kats), DynamicDataDisplayName = nameof(GetDisplayName))]
    public void FormatTrivia_ShouldMatchKatExpectation(CodeStyleKat kat)
    {
        if (kat is null) throw new ArgumentNullException(nameof(kat));

        (var baseIndent, var canonicalInput) = SplitBaseIndent(NormalizeToCrlf(kat.Input));
        (_, var canonicalExpected) = SplitBaseIndent(NormalizeToCrlf(kat.Expected));

        var formatter = new XmlDocFormatter();
        var context = new XmlDocFormatContext(baseIndent, "\r\n", XmlDocMemberKindHint.Unknown);
        XmlDocFormatOptions options = XmlDocFormatPolicyDefaults.CreateBoduDefaults();

        XmlDocFormatResult result = formatter.FormatTrivia(canonicalInput, context, options);

        if (kat.ExpectedFailure == CodeStyleKatFailureKind.MalformedXml)
        {
            Assert.IsFalse(result.Changed, $"KAT {kat.Id}: malformed XML should be left unchanged.");
            Assert.AreEqual(canonicalInput, result.FormattedText, $"KAT {kat.Id}: malformed XML should pass through verbatim.");
            return;
        }

        Assert.AreEqual(canonicalExpected, result.FormattedText, $"KAT {kat.Id}: formatted output differs from expected.");
        Assert.AreEqual(kat.ExpectedChanged, result.Changed, $"KAT {kat.Id}: Changed flag mismatch.");

        XmlDocFormatResult secondPass = formatter.FormatTrivia(result.FormattedText, context, options);
        Assert.AreEqual(result.FormattedText, secondPass.FormattedText, $"KAT {kat.Id}: formatter is not idempotent.");
        Assert.IsFalse(secondPass.Changed, $"KAT {kat.Id}: second pass should report Changed=false.");
    }

    /// <summary>
    /// Returns a display name for MSTest dynamic data output, surfacing the KAT identifier in the test runner.
    /// </summary>
    public static string GetDisplayName(MethodInfo methodInfo, object?[] data) =>
        BoduCodeStyleKats.GetDisplayName(methodInfo, data!);

    private static string NormalizeToCrlf(string text)
    {
        var normalized = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        return normalized.EndsWith("\r\n", StringComparison.Ordinal) ? normalized : normalized + "\r\n";
    }

    private static (string BaseIndent, string Canonical) SplitBaseIndent(string text)
    {
        var end = 0;
        while (end < text.Length && (text[end] == ' ' || text[end] == '\t'))
        {
            end++;
        }

        if (end == 0)
        {
            return (string.Empty, text);
        }

        var indent = text.Substring(0, end);
        var canonical = text.Substring(end);
        return (indent, canonical);
    }
}
