// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeTests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodeTests
{

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> on empty input throws
    /// <see cref="BencodeFormatException" /> with the unexpected-end-of-data message.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmptyInput_ShouldThrowExactly()
    {
        BencodeFormatException ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(ReadOnlySpan<byte>.Empty);
        });

        Assert.AreEqual(FormatsResourceStrings.Format_Invalid_BencodeUnexpectedEndOfData, ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> decodes <c>i42e</c> into a
    /// <see cref="BencodedInteger" /> with value 42.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSpanForCanonicalInteger_ShouldReturnBencodedInteger()
    {
        BencodedValue value = Bencode.Decode(CanonicalIntegerBytes);

        Assert.IsInstanceOfType<BencodedInteger>(value);
        Assert.AreEqual(42, ((BencodedInteger)value).Value);
    }
    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> decodes the canonical BEP 3 string example
    /// <c>4:spam</c> into a <see cref="BencodedString" /> whose UTF-8 text is <c>"spam"</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSpanForCanonicalString_ShouldReturnBencodedString()
    {
        BencodedValue value = Bencode.Decode(CanonicalSpamBytes);

        Assert.IsInstanceOfType<BencodedString>(value);
        Assert.AreEqual("spam", ((BencodedString)value).GetUtf8String());
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> decodes <c>de</c> into an empty
    /// <see cref="BencodedDictionary" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSpanForEmptyDictionary_ShouldReturnEmptyBencodedDictionary()
    {
        BencodedValue value = Bencode.Decode(CanonicalEmptyDictBytes);

        Assert.IsInstanceOfType<BencodedDictionary>(value);
        Assert.AreEqual(0, ((BencodedDictionary)value).Count);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> decodes <c>le</c> into an empty
    /// <see cref="BencodedList" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSpanForEmptyList_ShouldReturnEmptyBencodedList()
    {
        BencodedValue value = Bencode.Decode(CanonicalEmptyListBytes);

        Assert.IsInstanceOfType<BencodedList>(value);
        Assert.AreEqual(0, ((BencodedList)value).Count);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> handles a nested list containing a string
    /// and an integer.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSpanForNestedListOfStringAndInteger_ShouldReturnExpectedValues()
    {
        BencodedValue value = Bencode.Decode(Bytes("l4:spami42ee"));

        var list = (BencodedList)value;
        Assert.AreEqual(2, list.Count);
        Assert.AreEqual("spam", ((BencodedString)list[0]).GetUtf8String());
        Assert.AreEqual(42, ((BencodedInteger)list[1]).Value);
    }

    /// <summary>
    /// Verifies that <see cref="Bencode.Decode(ReadOnlySpan{byte})" /> throws
    /// <see cref="BencodeFormatException" /> when input contains trailing bytes beyond a complete value.
    /// </summary>
    [TestMethod]
    public void Decode_WhenTrailingData_ShouldThrowExactly()
    {
        BencodeFormatException ex = Assert.ThrowsExactly<BencodeFormatException>(() =>
        {
            _ = Bencode.Decode(Bytes("i3e0:trailing"));
        });

        Assert.AreEqual(FormatsResourceStrings.Format_Invalid_BencodeTrailingData, ex.Message);
    }

}
