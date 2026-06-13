// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.Comments.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that a full-line comment preceding a key/value pair is ignored.
    /// </summary>
    [TestMethod]
    public void Read_WhenFullLineComment_ShouldIgnoreComment()
    {
        TomlDocumentReader reader = Create("# a leading comment\nv = 1\n");

        ExpectSingleValue(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that a comment following a value on the same line is ignored.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineComment_ShouldIgnoreComment()
    {
        TomlDocumentReader reader = Create("v = 1 # trailing comment\n");

        ExpectSingleValue(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a document consisting only of comments and blank lines surfaces an empty root table.
    /// </summary>
    [TestMethod]
    public void Read_WhenDocumentIsCommentsOnly_ShouldSurfaceEmptyRoot()
    {
        TomlDocumentReader reader = Create("# only\n\n# comments\n");

        ExpectStartTable(ref reader);
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that blank lines interspersed between key/value pairs are tolerated.
    /// </summary>
    [TestMethod]
    public void Read_WhenBlankLinesBetweenPairs_ShouldSurfaceBothPairs()
    {
        TomlDocumentReader reader = Create("a = 1\n\n\nb = 2\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.Integer);
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.Integer);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that a disallowed control character within a comment is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenCommentContainsControlCharacter_ShouldThrowTomlFormatException()
    {
        // Embed a NUL (U+0000) in the comment; TOML forbids control characters other than tab in comments.
        var source = "v = 1 # bad" + (char)0x00 + "comment\n";

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create(source);
        });
    }

    /// <summary>
    /// Verifies that a bare carriage return inside a comment (not part of a CRLF pair) is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenCommentContainsBareCarriageReturn_ShouldThrowTomlFormatException()
    {
        var source = "v = 1 # bad\rcomment\n";

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create(source);
        });
    }
}
