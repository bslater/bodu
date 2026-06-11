// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8BencodeReaderTests.ValueTextEquals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Reader;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the <see cref="Utf8BencodeReader.ValueTextEquals(ReadOnlySpan{byte})" /> family: raw-byte and UTF-8 text
/// comparison against byte-string and property-name tokens, and rejection on other token kinds.
/// </summary>
public partial class Utf8BencodeReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.ValueTextEquals(ReadOnlySpan{byte})" /> matches the raw content of
    /// a byte-string token and rejects different content.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenComparedToUtf8Bytes_ShouldCompareRawContent()
    {
        var reader = new Utf8BencodeReader(Bytes("4:spam"));
        Assert.IsTrue(reader.Read());

        Assert.IsTrue(reader.ValueTextEquals("spam"u8));
        Assert.IsFalse(reader.ValueTextEquals("eggs"u8));
        Assert.IsFalse(reader.ValueTextEquals("spam!"u8));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.ValueTextEquals(ReadOnlySpan{byte})" /> matches a property-name
    /// token, the lookup pattern converter authors use to dispatch on dictionary keys.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenOnPropertyName_ShouldCompareKeyBytes()
    {
        var reader = new Utf8BencodeReader(Bytes("d3:cow3:mooe"));
        Assert.IsTrue(reader.Read()); // StartDictionary
        Assert.IsTrue(reader.Read()); // PropertyName cow

        Assert.IsTrue(reader.ValueTextEquals("cow"u8));
        Assert.IsFalse(reader.ValueTextEquals("moo"u8));
    }

    /// <summary>
    /// Verifies that the <see cref="ReadOnlySpan{T}" /> of <see langword="char" /> and <see langword="string" />
    /// overloads compare against the UTF-8 encoding of the text, including multi-byte characters.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenComparedToText_ShouldCompareUtf8Encoding()
    {
        // "café" encodes to five UTF-8 bytes (the é is two bytes).
        byte[] data = "5:café"u8.ToArray();
        var reader = new Utf8BencodeReader(data);
        Assert.IsTrue(reader.Read());

        Assert.IsTrue(reader.ValueTextEquals("café".AsSpan()));
        Assert.IsTrue(reader.ValueTextEquals("café"));
        Assert.IsFalse(reader.ValueTextEquals("cafe"));
    }

    /// <summary>
    /// Verifies that the <see langword="string" /> overload returns <see langword="false" /> for a
    /// <see langword="null" /> argument, because Bencode has no null token to match.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenTextIsNull_ShouldReturnFalse()
    {
        var reader = new Utf8BencodeReader(Bytes("4:spam"));
        Assert.IsTrue(reader.Read());

        Assert.IsFalse(reader.ValueTextEquals((string?)null));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8BencodeReader.ValueTextEquals(ReadOnlySpan{byte})" /> on an integer token throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ValueTextEquals_WhenOnIntegerToken_ShouldThrowInvalidOperationException()
    {
        var data = Bytes("i42e");

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new Utf8BencodeReader(data);
            _ = reader.Read();
            _ = reader.ValueTextEquals("42"u8);
        });
    }
}
