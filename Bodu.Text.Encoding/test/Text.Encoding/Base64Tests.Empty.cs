// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.Empty.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array for every variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [TestMethod]
    [DataRow(Base64Variant.Standard)]
    [DataRow(Base64Variant.UrlSafe)]
    [DataRow(Base64Variant.Mime)]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray(Base64Variant variant)
    {
        var actual = Base64.Decode(string.Empty, variant);

        Assert.AreEqual(0, actual.Length);
    }
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" /> for every variant.
    /// </summary>
    /// <param name="variant">The variant.</param>
    [TestMethod]
    [DataRow(Base64Variant.Standard)]
    [DataRow(Base64Variant.UrlSafe)]
    [DataRow(Base64Variant.Mime)]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString(Base64Variant variant)
    {
        var actual = Base64.Encode(Array.Empty<byte>(), variant);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryDecode" /> with an empty source returns <see langword="true" /> and writes
    /// zero bytes regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroBytesRegardlessOfDestination()
    {
        Assert.IsTrue(Base64.TryDecode(ReadOnlySpan<char>.Empty, new byte[1], out var t1));
        Assert.AreEqual(0, t1);

        Assert.IsTrue(Base64.TryDecode(ReadOnlySpan<char>.Empty, Array.Empty<byte>(), out var t2));
        Assert.AreEqual(0, t2);

        Assert.IsTrue(Base64.TryDecode(ReadOnlySpan<char>.Empty, new byte[100], out var t3));
        Assert.AreEqual(0, t3);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryEncode" /> with an empty source returns <see langword="true" /> and writes
    /// zero characters regardless of the destination span size.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroCharsRegardlessOfDestination()
    {
        Assert.IsTrue(Base64.TryEncode(ReadOnlySpan<byte>.Empty, new char[1], out var t1));
        Assert.AreEqual(0, t1);

        Assert.IsTrue(Base64.TryEncode(ReadOnlySpan<byte>.Empty, Array.Empty<char>(), out var t2));
        Assert.AreEqual(0, t2);

        Assert.IsTrue(Base64.TryEncode(ReadOnlySpan<byte>.Empty, new char[100], out var t3));
        Assert.AreEqual(0, t3);
    }

}
