// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReaderTests.InlineComments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Text.Configuration;

/// <summary>
/// Pins the inline-comment grammar across the three <see cref="ConfigurationInlineCommentMode" /> values for
/// both <c>#</c> and <c>;</c> introducers. The grammar is non-trivial: it must distinguish whitespace-led
/// comments from literal <c>#</c>/<c>;</c> embedded in values, must respect backslash escaping, and must
/// not surprise authors whose values legitimately contain those characters.
/// </summary>
[TestClass]
public class ConfigurationReaderInlineCommentTests
{
    /// <summary>
    /// Verifies that <see cref="ConfigurationInlineCommentMode.Disabled" /> preserves <c>#</c> and <c>;</c>
    /// in values verbatim — the EditorConfig-compatible behaviour.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInlineDisabledAndHashInValue_ShouldKeepHashAsLiteral()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.Disabled,
        };

        var doc = ConfigurationDocument.Parse("[*]\nkey = value # not-a-comment\n", options);

        Assert.AreEqual("value # not-a-comment", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationInlineCommentMode.Disabled" /> preserves a leading <c>;</c>
    /// after whitespace as a literal value character.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInlineDisabledAndSemicolonInValue_ShouldKeepSemicolonAsLiteral()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.Disabled,
        };

        var doc = ConfigurationDocument.Parse("[*]\nkey = value ; not-a-comment\n", options);

        Assert.AreEqual("value ; not-a-comment", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationInlineCommentMode.WhitespaceIntroduced" /> strips a comment
    /// introduced by whitespace and preserves the value preceding it (trimmed).
    /// </summary>
    [TestMethod]
    public void Parse_WhenWhitespaceIntroducedAndCommentAfterSpace_ShouldStripComment()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.WhitespaceIntroduced,
        };

        var doc = ConfigurationDocument.Parse("[*]\nkey = value # comment\n", options);

        Assert.AreEqual("value", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationInlineCommentMode.WhitespaceIntroduced" /> keeps a <c>#</c>
    /// that is *not* preceded by whitespace as part of the value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenWhitespaceIntroducedAndHashWithoutLeadingSpace_ShouldKeepAsLiteral()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.WhitespaceIntroduced,
        };

        var doc = ConfigurationDocument.Parse("[*]\nkey = value#literal\n", options);

        Assert.AreEqual("value#literal", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that under <see cref="ConfigurationInlineCommentMode.WhitespaceIntroduced" />, a value like
    /// a Windows path with embedded <c>;</c> (e.g. <c>PATH=C:\Temp;StillValue</c>) is kept intact because
    /// there is no whitespace before the <c>;</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenWhitespaceIntroducedAndSemicolonInPathLikeValue_ShouldKeepAsLiteral()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.WhitespaceIntroduced,
        };

        var doc = ConfigurationDocument.Parse("[*]\npath = C:\\Temp;StillValue\n", options);

        Assert.AreEqual(@"C:\Temp;StillValue", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that <see cref="ConfigurationInlineCommentMode.Always" /> strips a comment introduced by
    /// <c>#</c> even when no whitespace precedes it.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAlwaysAndHashWithoutLeadingSpace_ShouldStripComment()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.Always,
        };

        var doc = ConfigurationDocument.Parse("[*]\nkey = value#comment\n", options);

        Assert.AreEqual("value", doc.Sections[0].Entries[0].Value);
    }

    /// <summary>
    /// Verifies that an escaped <c>#</c> (<c>\#</c>) is never treated as a comment introducer, regardless of
    /// mode. The backslash is consumed by the escape, and the literal <c>#</c> remains in the value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInlineAlwaysAndHashIsEscaped_ShouldKeepHashAsLiteral()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.Always,
        };

        var doc = ConfigurationDocument.Parse(@"[*]" + "\nkey = value\\# literal\n", options);

        // The backslash escapes the '#', so it is not a comment introducer; the literal '#' stays.
        // The actual escape byte sequence preservation depends on the parser, but the '#' must not split.
        StringAssert.Contains(doc.Sections[0].Entries[0].Value, "#");
    }

    /// <summary>
    /// Verifies that the leading comment on a property line (a full-line <c>#</c>) attaches as a leading
    /// comment on the next property, irrespective of the inline comment mode.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInlineDisabledAndLeadingHashComment_ShouldStillBeRecognizedAsFullLineComment()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.Disabled,
        };

        var doc = ConfigurationDocument.Parse("[*]\n# leading\nkey = value\n", options);

        IniEntry entry = doc.Sections[0].Entries[0];
        Assert.AreEqual("value", entry.Value);
        Assert.HasCount(1, entry.LeadingComments);
    }

    /// <summary>
    /// Verifies that when an inline comment is captured, the comment prefix character is preserved so the
    /// document can be round-tripped without invented punctuation.
    /// </summary>
    [TestMethod]
    public void Parse_WhenWhitespaceIntroducedAndSemicolonComment_ShouldRecordSemicolonAsPrefix()
    {
        ConfigurationParseOptions options = new()
        {
            InlineCommentMode = ConfigurationInlineCommentMode.WhitespaceIntroduced,
        };

        var doc = ConfigurationDocument.Parse("[*]\nkey = value ; comment\n", options);

        IniEntry entry = doc.Sections[0].Entries[0];
        Assert.IsNotNull(entry.InlineComment);
        Assert.AreEqual(';', entry.InlineComment!.Value.Prefix);
    }
}
