// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Empty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that decoding an empty span returns an empty byte array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmptySpan_ShouldReturnEmptyByteArray()
    {
        var actual = Base16.Decode(ReadOnlySpan<char>.Empty);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray()
    {
        var actual = Base16.Decode(string.Empty);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that encoding an empty input with <see cref="BaseFormattingOptions.IncludePrefix" /> still emits the
    /// prefix.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyAndIncludePrefix_ShouldReturnPrefixOnly()
    {
        var actual = Base16.Encode(ReadOnlySpan<byte>.Empty, BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("0x", actual);
    }
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" /> with default options.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString()
    {
        var actual = Base16.Encode(Array.Empty<byte>());

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode" /> with an empty source returns <see langword="true" /> and writes
    /// zero bytes regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroBytesRegardlessOfDestination()
    {
        var tiny = new byte[1];
        var zero = Array.Empty<byte>();
        var huge = new byte[1000];

        Assert.IsTrue(Base16.TryDecode(ReadOnlySpan<char>.Empty, tiny, out var bytesTiny));
        Assert.AreEqual(0, bytesTiny);

        Assert.IsTrue(Base16.TryDecode(ReadOnlySpan<char>.Empty, zero, out var bytesZero));
        Assert.AreEqual(0, bytesZero);

        Assert.IsTrue(Base16.TryDecode(ReadOnlySpan<char>.Empty, huge, out var bytesHuge));
        Assert.AreEqual(0, bytesHuge);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode" /> with an empty source returns <see langword="true" /> and writes
    /// zero characters regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroCharsRegardlessOfDestination()
    {
        var tiny = new char[1];
        var zero = Array.Empty<char>();
        var huge = new char[1000];

        Assert.IsTrue(Base16.TryEncode(ReadOnlySpan<byte>.Empty, tiny, out var charsTiny));
        Assert.AreEqual(0, charsTiny);

        Assert.IsTrue(Base16.TryEncode(ReadOnlySpan<byte>.Empty, zero, out var charsZero));
        Assert.AreEqual(0, charsZero);

        Assert.IsTrue(Base16.TryEncode(ReadOnlySpan<byte>.Empty, huge, out var charsHuge));
        Assert.AreEqual(0, charsHuge);
    }

}
