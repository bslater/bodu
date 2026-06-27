// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.ValueSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies that <see cref="Utf8TomlReader.ValueSpan" /> exposes the raw bytes of the current value.
/// </summary>
public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that an escape-free basic string exposes its raw content bytes and reports no escapes, so decoding is
    /// a direct transcode.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenEscapeFreeBasicString_ShouldExposeRawContentWithoutEscapes()
    {
        Utf8TomlReader lexer = Create("s = \"plain\"\n");

        Advance(ref lexer, 2);
        Assert.AreEqual(TomlTokenType.String, lexer.TokenType);
        Assert.IsFalse(lexer.HasEscapes);
        Assert.IsTrue(lexer.ValueSpan.SequenceEqual("plain"u8));
        Assert.AreEqual("plain", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a basic string with escapes exposes the raw escaped bytes while <see cref="Utf8TomlReader.GetString" />
    /// resolves them.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenEscapedBasicString_ShouldExposeRawBytesAndDecodeOnDemand()
    {
        Utf8TomlReader lexer = Create("s = \"a\\nb\"\n");

        Advance(ref lexer, 2);
        Assert.IsTrue(lexer.HasEscapes);
        Assert.IsTrue(lexer.ValueSpan.SequenceEqual("a\\nb"u8));
        Assert.AreEqual("a\nb", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a literal string exposes its content verbatim with no escape processing.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenLiteralString_ShouldExposeContentVerbatim()
    {
        Utf8TomlReader lexer = Create(@"s = 'C:\Users\node'" + "\n");

        Advance(ref lexer, 2);
        Assert.IsFalse(lexer.HasEscapes);
        Assert.AreEqual(@"C:\Users\node", lexer.GetString());
    }

    /// <summary>
    /// Verifies that a number token exposes its raw text — including underscores — while the typed accessor returns
    /// the decoded value.
    /// </summary>
    [TestMethod]
    public void ValueSpan_WhenUnderscoredInteger_ShouldExposeRawTextAndDecodeValue()
    {
        Utf8TomlReader lexer = Create("i = 1_000_000\n");

        Advance(ref lexer, 2);
        Assert.IsTrue(lexer.ValueSpan.SequenceEqual("1_000_000"u8));
        Assert.AreEqual(1_000_000L, lexer.GetInt64());
    }

}
