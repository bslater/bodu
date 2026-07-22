// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.Encoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains encoding and line-handling robustness tests for <see cref="Utf8DotEnvReader" /> — BOM stripping, line-ending
/// variants, and UTF-8 multibyte values — mirroring the classic interop defect classes.
/// </summary>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is stripped so the first key is not corrupted.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingByteOrderMark_ShouldStripIt()
    {
        byte[] bytes = [0xEF, 0xBB, 0xBF, .. "HOST=localhost\n"u8];
        var reader = new Utf8DotEnvReader(bytes);
        var names = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == DotEnvTokenType.PropertyName)
                names.Add(reader.GetString());
        }

        CollectionAssert.AreEqual(new List<string> { "HOST" }, names);
    }

    /// <summary>
    /// Verifies that CRLF line endings are handled equivalently to LF.
    /// </summary>
    [TestMethod]
    public void Read_WhenCrlfLineEndings_ShouldReadEntries()
    {
        List<string> tokens = Transcribe("A=1\r\nB=2\r\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:A", "String:1", "Name:B", "String:2", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that lone carriage-return (classic Mac) line endings are handled.
    /// </summary>
    [TestMethod]
    public void Read_WhenCarriageReturnOnlyLineEndings_ShouldReadEntries()
    {
        List<string> tokens = Transcribe("A=1\rB=2\r");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:A", "String:1", "Name:B", "String:2", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a final entry without a trailing newline is read.
    /// </summary>
    [TestMethod]
    public void Read_WhenNoTrailingNewline_ShouldReadFinalEntry()
    {
        List<string> tokens = Transcribe("A=1\nB=2");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:A", "String:1", "Name:B", "String:2", "EndObject" },
            tokens);
    }

    /// <summary>
    /// Verifies that a UTF-8 multibyte value is decoded correctly.
    /// </summary>
    [TestMethod]
    public void Read_WhenUnicodeValue_ShouldDecodeMultibyte()
    {
        List<string> tokens = Transcribe("GREETING=\"café ☕\"\n");

        CollectionAssert.AreEqual(
            new List<string> { "StartObject", "Name:GREETING", "String:café ☕", "EndObject" },
            tokens);
    }
}
