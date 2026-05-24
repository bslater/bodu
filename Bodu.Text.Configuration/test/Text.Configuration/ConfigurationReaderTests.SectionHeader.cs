// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReaderTests.SectionHeader.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Text.Configuration;

/// <summary>
/// Pins the section-header grammar matrix. The parser scans back from end-of-line for the closing <c>]</c>,
/// which deliberately allows <c>]</c> inside the section name (matching EditorConfig section conventions).
/// What is permitted *after* the closing <c>]</c> — trailing whitespace, trailing words, an inline comment —
/// is controlled by <see cref="ConfigurationParseOptions.SectionHeaderMode" />.
/// </summary>
[TestClass]
public class ConfigurationReaderSectionHeaderTests
{
    /// <summary>
    /// Verifies that under the default lenient header mode, trailing non-whitespace after the closing
    /// <c>]</c> is absorbed into the section name (because the parser scans back for the final <c>]</c>).
    /// </summary>
    [TestMethod]
    public void Parse_WhenLenientAndTrailingText_ShouldNotEmitDiagnostic()
    {
        ConfigurationParseOptions options = new()
        {
            SectionHeaderMode = ConfigurationSectionHeaderMode.Lenient,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        // Should not throw.
        IniDocument doc = ConfigurationDocument.Parse("[*.cs] trailing\nkey = value\n", options);
        Assert.HasCount(1, doc.Sections);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationSectionHeaderMode.Strict" />, trailing non-whitespace
    /// after the closing <c>]</c> emits
    /// <see cref="ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStrictAndTrailingText_ShouldEmitTrailingContentDiagnostic()
    {
        ConfigurationParseOptions options = new()
        {
            SectionHeaderMode = ConfigurationSectionHeaderMode.Strict,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse("[*.cs] trailing\n", options);
        });

        Assert.IsNotNull(ex.Diagnostic);
        Assert.AreEqual(ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader, ex.Diagnostic!.Code);
    }

    /// <summary>
    /// Verifies that even strict mode permits trailing whitespace after the closing <c>]</c> — whitespace
    /// is not considered trailing "content".
    /// </summary>
    [TestMethod]
    public void Parse_WhenStrictAndTrailingWhitespaceOnly_ShouldNotEmitDiagnostic()
    {
        ConfigurationParseOptions options = new()
        {
            SectionHeaderMode = ConfigurationSectionHeaderMode.Strict,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        IniDocument doc = ConfigurationDocument.Parse("[*.cs]   \nkey = value\n", options);
        Assert.HasCount(1, doc.Sections);
        Assert.AreEqual("*.cs", doc.Sections[0].Name);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationSectionHeaderMode.AllowTrailingInlineComment" />, a
    /// trailing <c>#</c>-introduced comment after the closing <c>]</c> is accepted (and discarded).
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowTrailingInlineCommentAndHashComment_ShouldAccept()
    {
        ConfigurationParseOptions options = new()
        {
            SectionHeaderMode = ConfigurationSectionHeaderMode.AllowTrailingInlineComment,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        IniDocument doc = ConfigurationDocument.Parse("[*.cs] # comment\nkey = value\n", options);
        Assert.HasCount(1, doc.Sections);
        Assert.AreEqual("*.cs", doc.Sections[0].Name);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationSectionHeaderMode.AllowTrailingInlineComment" />, a
    /// trailing <c>;</c>-introduced comment after the closing <c>]</c> is accepted (and discarded).
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowTrailingInlineCommentAndSemicolonComment_ShouldAccept()
    {
        ConfigurationParseOptions options = new()
        {
            SectionHeaderMode = ConfigurationSectionHeaderMode.AllowTrailingInlineComment,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        IniDocument doc = ConfigurationDocument.Parse("[*.cs] ; comment\nkey = value\n", options);
        Assert.HasCount(1, doc.Sections);
        Assert.AreEqual("*.cs", doc.Sections[0].Name);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationSectionHeaderMode.AllowTrailingInlineComment" />,
    /// trailing non-comment content still produces the trailing-content diagnostic.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAllowTrailingInlineCommentAndTrailingText_ShouldEmitDiagnostic()
    {
        ConfigurationParseOptions options = new()
        {
            SectionHeaderMode = ConfigurationSectionHeaderMode.AllowTrailingInlineComment,
            DiagnosticMode = ConfigurationDiagnosticMode.Throw,
        };

        Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse("[*.cs] trailing\n", options);
        });
    }

    /// <summary>
    /// Verifies the existing behaviour that a section name containing <c>]</c> is preserved verbatim — the
    /// parser scans back for the final <c>]</c>, allowing patterns such as <c>[abc]def]</c>. This test pins
    /// the lenient default's contract.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLenientAndSectionNameContainsBracket_ShouldUseFinalClosingBracket()
    {
        ConfigurationParseOptions options = new()
        {
            SectionHeaderMode = ConfigurationSectionHeaderMode.Lenient,
        };

        IniDocument doc = ConfigurationDocument.Parse("[abc]def]\nkey = value\n", options);

        Assert.HasCount(1, doc.Sections);
        Assert.AreEqual("abc]def", doc.Sections[0].Name);
    }

    /// <summary>
    /// Verifies that the EditorConfig-compatible preset selects the strict header mode so that malformed
    /// trailing content surfaces as a diagnostic rather than silently extending the section name.
    /// </summary>
    [TestMethod]
    public void Parse_WhenEditorConfigCompatibleAndTrailingText_ShouldEmitTrailingContentDiagnostic()
    {
        ConfigurationParseOptions options = ConfigurationParseOptions.EditorConfigCompatible;

        ConfigurationParseException ex = Assert.ThrowsExactly<ConfigurationParseException>(() =>
        {
            _ = ConfigurationDocument.Parse("[*.cs] trailing\n", options);
        });

        Assert.AreEqual(ConfigurationDiagnosticCode.TrailingContentAfterSectionHeader, ex.Diagnostic!.Code);
    }
}
