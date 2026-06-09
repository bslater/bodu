// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderSurrogateTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Tests for the reader's Unicode-scalar validation: character input (the <see cref="string" />,
/// <see cref="ReadOnlySpan{T}" />, and <see cref="System.IO.TextReader" /> paths) can carry a lone UTF-16 surrogate half
/// that no valid UTF-8 document could contain, so the reader rejects it wherever source text appears while preserving
/// valid surrogate pairs. This keeps the reader aligned with the writer, which performs the equivalent check before
/// emitting keys and string values.
/// </summary>
[TestClass]
public sealed class TomlReaderSurrogateTests
{
    /// <summary>
    /// Verifies that an unpaired high surrogate in a basic string value is rejected with a
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBasicStringHasUnpairedHighSurrogate_ShouldThrowTomlFormatException() =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("s = \"\uD800\""));

    /// <summary>
    /// Verifies that an unpaired low surrogate in a basic string value is rejected with a
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBasicStringHasUnpairedLowSurrogate_ShouldThrowTomlFormatException() =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("s = \"\uDC00\""));

    /// <summary>
    /// Verifies that an unpaired low surrogate in a quoted key is rejected with a <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuotedKeyHasUnpairedLowSurrogate_ShouldThrowTomlFormatException() =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("\"\uDC00\" = 1"));

    /// <summary>
    /// Verifies that an unpaired surrogate in a literal string is rejected with a <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenLiteralStringHasUnpairedSurrogate_ShouldThrowTomlFormatException() =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("s = '\uD800'"));

    /// <summary>
    /// Verifies that an unpaired surrogate in a multi-line basic string is rejected with a
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultilineStringHasUnpairedSurrogate_ShouldThrowTomlFormatException() =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("s = \"\"\"\uD800\"\"\""));

    /// <summary>
    /// Verifies that an unpaired surrogate in a comment is rejected with a <see cref="TomlFormatException" />, because
    /// the document as a whole must be valid UTF-8.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCommentHasUnpairedSurrogate_ShouldThrowTomlFormatException() =>
        Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("# \uD800"));

    /// <summary>
    /// Verifies that the surrogate error carries the 1-based source line on which the unpaired surrogate appears.
    /// </summary>
    [TestMethod]
    public void Parse_WhenUnpairedSurrogateOnSecondLine_ShouldReportLineTwo()
    {
        var ex = Assert.ThrowsExactly<TomlFormatException>(() => Toml.Parse("a = 1\nb = \"\uD800\""));

        Assert.AreEqual(2, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a valid non-BMP scalar (a surrogate pair) in a string value is accepted and preserved.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringHasSurrogatePair_ShouldPreserveScalar()
    {
        var doc = Toml.Parse("s = \"\U0001F600\"");

        Assert.AreEqual("\U0001F600", ((TomlString)doc["s"]).Value);
    }

    /// <summary>
    /// Verifies that a valid non-BMP scalar (a surrogate pair) in a quoted key is accepted and preserved.
    /// </summary>
    [TestMethod]
    public void Parse_WhenQuotedKeyHasSurrogatePair_ShouldPreserveKey()
    {
        var doc = Toml.Parse("\"\U0001F600\" = 1");

        Assert.AreEqual(1, ((TomlInteger)doc["\U0001F600"]).Value);
    }

    /// <summary>
    /// Verifies that <see cref="Toml.TryParse(System.ReadOnlySpan{char}, out TomlTable)" /> reports failure rather than
    /// throwing when the source contains an unpaired surrogate.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenSourceHasUnpairedSurrogate_ShouldReturnFalse()
    {
        var result = Toml.TryParse("s = \"\uD800\"", out var document);

        Assert.IsFalse(result);
        Assert.IsNull(document);
    }
}
