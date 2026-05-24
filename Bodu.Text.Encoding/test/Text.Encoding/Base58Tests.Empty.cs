// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Empty.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that decoding an empty string returns an empty byte array.
    /// </summary>
    /// <param name="variant">The Base58 variant.</param>
    [DataTestMethod]
    [DataRow(Base58Variant.BitcoinFlickr)]
    [DataRow(Base58Variant.Ripple)]
    public void Decode_WhenEmptyString_ShouldReturnEmptyByteArray(Base58Variant variant)
    {
        var actual = Base58.Decode(string.Empty, variant);

        Assert.AreEqual(0, actual.Length);
    }
    /// <summary>
    /// Verifies that encoding an empty byte array returns <see cref="string.Empty" />.
    /// </summary>
    /// <param name="variant">The Base58 variant.</param>
    [DataTestMethod]
    [DataRow(Base58Variant.BitcoinFlickr)]
    [DataRow(Base58Variant.Ripple)]
    public void Encode_WhenEmptyByteArray_ShouldReturnEmptyString(Base58Variant variant)
    {
        var actual = Base58.Encode(Array.Empty<byte>(), variant);

        Assert.AreEqual(string.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryEncode" /> and <see cref="Base58.TryDecode" /> with empty source return
    /// <see langword="true" /> with zero counts written.
    /// </summary>
    [TestMethod]
    public void TryEncodeAndTryDecode_WhenSourceIsEmpty_ShouldReturnTrueAndZeroWritten()
    {
        Assert.IsTrue(Base58.TryEncode(ReadOnlySpan<byte>.Empty, new char[8], out var charsWritten));
        Assert.AreEqual(0, charsWritten);

        Assert.IsTrue(Base58.TryDecode(ReadOnlySpan<char>.Empty, new byte[8], out var bytesWritten));
        Assert.AreEqual(0, bytesWritten);
    }

}
