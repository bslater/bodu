// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45Tests.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base45Tests
{
    /// <summary>
    /// Verifies that <see cref="Base45.GetEncodedLength(int)" /> returns three characters per byte pair plus two for a
    /// trailing odd byte.
    /// </summary>
    /// <param name="byteCount">The input byte count.</param>
    /// <param name="expected">The expected encoded character count.</param>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 5)]
    [DataRow(4, 6)]
    [DataRow(5, 8)]
    public void GetEncodedLength_WhenGivenByteCount_ShouldReturnExpected(int byteCount, int expected)
    {
        Assert.AreEqual(expected, Base45.GetEncodedLength(byteCount));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.GetEncodedLength(int)" /> throws for a negative byte count.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base45.GetEncodedLength(-1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base45.Encode(byte[])" /> throws for a null byte array.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullByteArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base45.Encode((byte[])null!);
        });
    }

    /// <summary>
    /// Verifies that the span-returning <see cref="Base45.Encode(ReadOnlySpan{byte}, Span{char})" /> writes the exact
    /// encoded text and reports the character count.
    /// </summary>
    [TestMethod]
    public void Encode_WhenWritingToSpan_ShouldWriteExpectedText()
    {
        Span<char> destination = new char[Base45.GetEncodedLength(2)];

        var written = Base45.Encode("AB"u8.ToArray(), destination);

        Assert.AreEqual(3, written);
        Assert.AreEqual("BB8", new string(destination.Slice(0, written)));
    }

    /// <summary>
    /// Verifies that the span-returning <see cref="Base45.Encode(ReadOnlySpan{byte}, Span{char})" /> throws an
    /// <see cref="ArgumentException" /> for the <c>destination</c> parameter when the buffer is too small.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDestinationTooSmall_ShouldThrowExactlyForDestination()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            Span<char> destination = new char[2];
            _ = Base45.Encode("AB"u8.ToArray(), destination);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Base45.Encode(byte[], int, int)" /> encodes only the requested segment.
    /// </summary>
    [TestMethod]
    public void Encode_WhenOffsetAndCount_ShouldEncodeSegmentOnly()
    {
        byte[] payload = [0xFF, 0x41, 0x42, 0xFF];

        Assert.AreEqual("BB8", Base45.Encode(payload, 1, 2));
    }
}
