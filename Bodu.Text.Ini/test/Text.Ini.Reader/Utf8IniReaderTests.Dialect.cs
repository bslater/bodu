// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8IniReaderTests.Dialect.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Contains dialect and robustness tests for <see cref="Utf8IniReader" /> pinning the INI-specific interop divergences:
/// BOM/line-ending/encoding handling, the <c>=</c>-only delimiter, literal (unstripped) values and inline comments,
/// the <c>#</c> comment toggle, and the malformed-header/entry rejections.
/// </summary>
public partial class Utf8IniReaderTests
{
    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is stripped so the first section or key is not corrupted.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingByteOrderMark_ShouldStripIt()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. "[db]\nhost=x\n"u8];
        var reader = new Utf8IniReader(bytes);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(IniTokenType.SectionHeader, reader.TokenType);
        Assert.AreEqual("db", reader.GetString());
    }

    /// <summary>
    /// Verifies that CRLF and lone-CR line endings are handled equivalently to LF.
    /// </summary>
    [TestMethod]
    public void Read_WhenMixedLineEndings_ShouldReadEntries()
    {
        List<string> tokens = Transcribe("a=1\r\nb=2\rc=3\n");

        CollectionAssert.AreEqual(
            new List<string> { "Name:a", "String:1", "Name:b", "String:2", "Name:c", "String:3" },
            tokens);
    }

    /// <summary>
    /// Verifies that a UTF-8 multibyte section name and value are decoded correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnicode_ShouldDecodeMultibyte()
    {
        List<string> tokens = Transcribe("[café]\ndrink=☕\n");

        CollectionAssert.AreEqual(new List<string> { "Section:café", "Name:drink", "String:☕" }, tokens);
    }

    /// <summary>
    /// Verifies that only the first <c>=</c> splits an entry, so a value may contain <c>=</c> and <c>:</c>.
    /// </summary>
    [TestMethod]
    public void Read_WhenValueContainsEqualsAndColon_ShouldKeepLiteral()
    {
        List<string> tokens = Transcribe("conn=a=b:c\n");

        CollectionAssert.AreEqual(new List<string> { "Name:conn", "String:a=b:c" }, tokens);
    }

    /// <summary>
    /// Verifies that a colon is not accepted as a key/value delimiter (the dialect is <c>=</c>-only).
    /// </summary>
    [TestMethod]
    public void Read_WhenColonDelimiter_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("host: localhost\n");
        });
    }

    /// <summary>
    /// Verifies that an inline <c>;</c> is not stripped: the value retains everything after the assignment.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineSemicolon_ShouldKeepItInValue()
    {
        List<string> tokens = Transcribe("k=v ; not a comment\n");

        CollectionAssert.AreEqual(new List<string> { "Name:k", "String:v ; not a comment" }, tokens);
    }

    /// <summary>
    /// Verifies that surrounding quotes are preserved literally rather than stripped (the configparser dialect).
    /// </summary>
    [TestMethod]
    public void Read_WhenQuotedValue_ShouldPreserveQuotes()
    {
        List<string> tokens = Transcribe("k=\"a b\"\n");

        CollectionAssert.AreEqual(new List<string> { "Name:k", "String:\"a b\"" }, tokens);
    }

    /// <summary>
    /// Verifies that values that look like interpolation are preserved verbatim (no expansion is performed).
    /// </summary>
    [TestMethod]
    public void Read_WhenValueLooksLikeInterpolation_ShouldPreserveVerbatim()
    {
        List<string> tokens = Transcribe("p=${HOME}/%PATH%\n");

        CollectionAssert.AreEqual(new List<string> { "Name:p", "String:${HOME}/%PATH%" }, tokens);
    }

    /// <summary>
    /// Verifies that disabling <c>#</c> comments treats a leading <c>#</c> as part of a key rather than a comment.
    /// </summary>
    [TestMethod]
    public void Read_WhenHashCommentsDisabled_ShouldTreatHashAsKey()
    {
        List<string> tokens = Transcribe("#k=v\n", new IniReaderOptions { DisallowHashComments = true });

        CollectionAssert.AreEqual(new List<string> { "Name:#k", "String:v" }, tokens);
    }

    /// <summary>
    /// Verifies that an unterminated section header throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnterminatedSection_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("[db\nhost=x\n");
        });
    }

    /// <summary>
    /// Verifies that an empty section name throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptySectionName_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("[]\nhost=x\n");
        });
    }

    /// <summary>
    /// Verifies that an empty key throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyKey_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Transcribe("=value\n");
        });
    }
}
