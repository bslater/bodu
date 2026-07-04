// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.PaddingConstantTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.DepadBlock(PaddingMode, int, ReadOnlySpan{byte}, Span{byte})" />
    /// rejects a corrupted pad byte regardless of its position within the pad region — a bad byte at the first,
    /// middle, or last position is rejected identically.
    /// </summary>
    /// <remarks>
    /// This pins the observable accept/reject contract that the constant-time padding check must preserve: pad
    /// validation accumulates a difference across the whole candidate region with no data-dependent early exit, so
    /// the rejection outcome does not depend on where the first bad byte sits. Wall-clock timing is not
    /// unit-testable; this test guards the outcome invariance that the timing property builds on.
    /// </remarks>
    /// <param name="description">A human-readable label for the corrupted-position scenario.</param>
    /// <param name="padding">The padding mode under test.</param>
    /// <param name="inputHex">The corrupted padded block, hex-encoded.</param>
    [TestMethod]
    [DataRow("PKCS7 bad byte at first pad position", PaddingMode.PKCS7, "0008080808080808")]
    [DataRow("PKCS7 bad byte at middle pad position", PaddingMode.PKCS7, "0808080008080808")]
    [DataRow("PKCS7 bad byte at last non-count position", PaddingMode.PKCS7, "0808080808080008")]
    [DataRow("ANSIX923 bad byte at first pad position", PaddingMode.ANSIX923, "FF00000000000008")]
    [DataRow("ANSIX923 bad byte at middle pad position", PaddingMode.ANSIX923, "00000000FF000008")]
    [DataRow("ANSIX923 bad byte at last zero position", PaddingMode.ANSIX923, "000000000000FF08")]
    public void DepadBlock_WhenPadByteCorruptedAtAnyPosition_ShouldRejectIdentically(
        string description, PaddingMode padding, string inputHex)
    {
        byte[] input = Convert.FromHexString(inputHex);
        byte[] destination = new byte[input.Length];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            _ = CryptographyHelper.DepadBlock(padding, input.Length * 8, input, destination);
        }, description);
    }
}
