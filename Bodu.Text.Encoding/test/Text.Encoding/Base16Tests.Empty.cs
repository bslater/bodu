// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Empty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        byte[] actual = Base16.Decode([]);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray()
    {
        byte[] actual = Base16.Decode(string.Empty);

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that encoding an empty input with <see cref="BaseFormattingOptions.IncludePrefix" /> still emits the
    /// prefix.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyAndIncludePrefix_ShouldReturnPrefixOnly()
    {
        string actual = Base16.Encode([], BaseFormattingOptions.IncludePrefix);

        Assert.AreEqual("0x", actual);
    }
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" /> with default options.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString()
    {
        string actual = Base16.Encode([]);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryDecode" /> with an empty source returns <see langword="true" /> and writes
    /// zero bytes regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroBytesRegardlessOfDestination()
    {
        byte[] tiny = new byte[1];
        byte[] zero = Array.Empty<byte>();
        byte[] huge = new byte[1000];

        Assert.IsTrue(Base16.TryDecode([], tiny, out int bytesTiny));
        Assert.AreEqual(0, bytesTiny);

        Assert.IsTrue(Base16.TryDecode([], zero, out int bytesZero));
        Assert.AreEqual(0, bytesZero);

        Assert.IsTrue(Base16.TryDecode([], huge, out int bytesHuge));
        Assert.AreEqual(0, bytesHuge);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryEncode" /> with an empty source returns <see langword="true" /> and writes
    /// zero characters regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroCharsRegardlessOfDestination()
    {
        char[] tiny = new char[1];
        char[] zero = Array.Empty<char>();
        char[] huge = new char[1000];

        Assert.IsTrue(Base16.TryEncode([], tiny, out int charsTiny));
        Assert.AreEqual(0, charsTiny);

        Assert.IsTrue(Base16.TryEncode([], zero, out int charsZero));
        Assert.AreEqual(0, charsZero);

        Assert.IsTrue(Base16.TryEncode([], huge, out int charsHuge));
        Assert.AreEqual(0, charsHuge);
    }

}
