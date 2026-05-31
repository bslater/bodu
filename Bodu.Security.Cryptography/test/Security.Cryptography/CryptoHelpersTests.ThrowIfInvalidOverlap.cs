// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ThrowIfInvalidOverlap.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> permits non-overlapping spans in different
    /// arrays.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenSpansInDifferentArrays_ShouldNotThrow()
    {
        var input = new byte[16];
        var output = new byte[16];

        CryptoHelpers.ThrowIfInvalidOverlap(input, output);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> permits non-overlapping spans within the same
    /// array.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenSpansAreDisjointInSameArray_ShouldNotThrow()
    {
        var buffer = new byte[32];

        CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(0, 16), buffer.AsSpan(16, 16));
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> permits exact in-place aliasing when
    /// <c>allowExactInPlace</c> is <see langword="true" /> (the default).
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenExactInPlace_ShouldNotThrow()
    {
        var buffer = new byte[16];

        CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(), buffer.AsSpan());
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> rejects exact aliasing when
    /// <c>allowExactInPlace</c> is <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenExactInPlaceAndNotAllowed_ShouldThrowCryptographicException()
    {
        var buffer = new byte[16];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(), buffer.AsSpan(), allowExactInPlace: false);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> rejects an output range that starts inside
    /// the input range.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenOutputStartsInsideInput_ShouldThrowCryptographicException()
    {
        var buffer = new byte[32];

        // input [0, 16); output [8, 24) → shares [8, 16).
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(0, 16), buffer.AsSpan(8, 16));
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> rejects an input range that starts inside
    /// the output range.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenInputStartsInsideOutput_ShouldThrowCryptographicException()
    {
        var buffer = new byte[32];

        // output [0, 16); input [8, 24) → shares [8, 16).
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(8, 16), buffer.AsSpan(0, 16));
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> rejects a one-byte overlap between the
    /// trailing edge of the input and the leading edge of the output.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenOneByteOverlap_ShouldThrowCryptographicException()
    {
        var buffer = new byte[32];

        // input [0, 16); output [15, 31) → shares [15, 16).
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(0, 16), buffer.AsSpan(15, 16));
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> rejects spans that share the same start
    /// position but differ in length.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenSameStartDifferentLength_ShouldThrowCryptographicException()
    {
        var buffer = new byte[32];

        // input [0, 16); output [0, 24): same start, different length → not "exact in-place".
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(0, 16), buffer.AsSpan(0, 24));
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> short-circuits and accepts an empty input.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenInputIsEmpty_ShouldNotThrow()
    {
        var buffer = new byte[16];

        CryptoHelpers.ThrowIfInvalidOverlap(ReadOnlySpan<byte>.Empty, buffer.AsSpan());
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ThrowIfInvalidOverlap" /> short-circuits and accepts an empty output.
    /// </summary>
    [TestMethod]
    public void ThrowIfInvalidOverlap_WhenOutputIsEmpty_ShouldNotThrow()
    {
        var buffer = new byte[16];

        CryptoHelpers.ThrowIfInvalidOverlap(buffer.AsSpan(), Span<byte>.Empty);
    }
}
