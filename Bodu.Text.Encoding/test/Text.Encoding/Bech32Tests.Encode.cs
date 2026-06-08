// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bech32Tests.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Bech32Tests
{
    /// <summary>
    /// Verifies that <see cref="Bech32.Encode(string, ReadOnlySpan{byte}, Bech32Encoding)" /> throws for a null
    /// human-readable part.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNullHrp_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Bech32.Encode(null!, ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that encoding with an empty human-readable part throws an <see cref="ArgumentException" /> naming the
    /// <c>hrp</c> parameter.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmptyHrp_ShouldThrowExactlyForHrp()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Bech32.Encode(string.Empty, ReadOnlySpan<byte>.Empty);
        });

        Assert.AreEqual("hrp", ex.ParamName);
    }

    /// <summary>
    /// Verifies that encoding with a human-readable part containing an out-of-range character (a space, code point 32)
    /// throws an <see cref="ArgumentException" /> naming the <c>hrp</c> parameter.
    /// </summary>
    [TestMethod]
    public void Encode_WhenHrpCharOutOfRange_ShouldThrowExactlyForHrp()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Bech32.Encode("a b", ReadOnlySpan<byte>.Empty);
        });

        Assert.AreEqual("hrp", ex.ParamName);
    }

    /// <summary>
    /// Verifies that encoding a data group greater than 31 throws an <see cref="ArgumentException" /> naming the
    /// <c>data</c> parameter.
    /// </summary>
    [TestMethod]
    public void Encode_WhenDataValueExceedsFiveBits_ShouldThrowExactlyForData()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Bech32.Encode("a", new byte[] { 32 });
        });

        Assert.AreEqual("data", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an upper-case human-readable part is lower-cased to produce canonical output.
    /// </summary>
    [TestMethod]
    public void Encode_WhenHrpUpperCase_ShouldProduceLowerCaseOutput()
    {
        Assert.AreEqual("a12uel5l", Bech32.Encode("A", ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.TryEncode(string, ReadOnlySpan{byte}, Bech32Encoding, out string)" /> returns
    /// <see langword="false" /> without throwing for an invalid human-readable part.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenInvalidHrp_ShouldReturnFalse()
    {
        var success = Bech32.TryEncode(string.Empty, ReadOnlySpan<byte>.Empty, Bech32Encoding.Bech32, out var result);

        Assert.IsFalse(success);
        Assert.IsNull(result);
    }

    /// <summary>
    /// Verifies that <see cref="Bech32.GetEncodedLength(int, int)" /> returns the sum of the parts, the separator, and
    /// the six checksum symbols.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WhenGivenPartLengths_ShouldReturnTotal()
    {
        Assert.AreEqual(2 + 1 + 33 + 6, Bech32.GetEncodedLength(2, 33));
    }
}
