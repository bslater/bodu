// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.ValueTextEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that an escape-free string token compares equal to matching UTF-8, string, and char-span text, and
    /// unequal otherwise.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenEscapeFreeString_ShouldCompareWithoutDecoding()
    {
        Utf8TomlReader reader = Create("s = \"value\"\n");

        Advance(ref reader, 2);
        Assert.IsTrue(reader.ValueTextEquals("value"u8));
        Assert.IsTrue(reader.ValueTextEquals("value"));
        Assert.IsTrue(reader.ValueTextEquals("value".AsSpan()));
        Assert.IsFalse(reader.ValueTextEquals("other"u8));
        Assert.IsFalse(reader.ValueTextEquals("valu"));
        Assert.IsFalse(reader.ValueTextEquals("values".AsSpan()));
    }

    /// <summary>
    /// Verifies that an escaped string token compares against its decoded text, not its raw bytes.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenEscapedString_ShouldCompareDecodedText()
    {
        Utf8TomlReader reader = Create("s = \"a\\nb\"\n");

        Advance(ref reader, 2);
        Assert.IsTrue(reader.ValueTextEquals("a\nb"));
        Assert.IsTrue(reader.ValueTextEquals("a\nb"u8));
        Assert.IsFalse(reader.ValueTextEquals("a\\nb"));
    }

    /// <summary>
    /// Verifies that a key token participates in text comparison, enabling allocation-free key matching.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenKeyToken_ShouldCompareKeyText()
    {
        Utf8TomlReader reader = Create("host = 1\n");

        Advance(ref reader, 1);
        Assert.IsTrue(reader.ValueTextEquals("host"u8));
        Assert.IsFalse(reader.ValueTextEquals("port"u8));
    }

    /// <summary>
    /// Verifies that multi-byte UTF-8 content compares correctly against the equivalent UTF-16 text.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenMultiByteContent_ShouldCompareAcrossEncodings()
    {
        Utf8TomlReader reader = Create("s = \"héllo\"\n");

        Advance(ref reader, 2);
        Assert.IsTrue(reader.ValueTextEquals("héllo"));
        Assert.IsTrue(reader.ValueTextEquals("héllo".AsSpan()));
        Assert.IsFalse(reader.ValueTextEquals("hello"));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> string compares as empty, matching
    /// <see cref="System.Text.Json.Utf8JsonReader.ValueTextEquals(string)" />.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenTextIsNull_ShouldCompareAsEmpty()
    {
        Utf8TomlReader reader = Create("s = \"\"\nx = \"a\"\n");

        Advance(ref reader, 2);
        Assert.IsTrue(reader.ValueTextEquals((string?)null));

        Advance(ref reader, 2);
        Assert.IsFalse(reader.ValueTextEquals((string?)null));
    }

    /// <summary>
    /// Verifies that comparing on a non-text token throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenCurrentTokenIsInteger_ShouldThrowInvalidOperationException()
    {
        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlReader reader = Create("a = 1\n");
            Advance(ref reader, 2);
            _ = reader.ValueTextEquals("1"u8);
        });
    }
}
