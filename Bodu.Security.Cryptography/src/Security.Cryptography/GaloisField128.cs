// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GaloisField128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides doubling in the binary field <c>GF(2¹²⁸)</c> with big-endian bit order - the <c>dbl()</c> of the CMAC
/// subkey derivation, SIV's S2V, and the OCB offset ladder.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Double" /> folds its one secret-dependent decision through a <c>0x00</c>/<c>0xFF</c> mask, so the
/// key-derived CMAC subkeys and OCB offsets it produces do not influence control flow.
/// </para>
/// <para>
/// Multiplication in the GHASH field, which GCM and GCM-SIV need, lives in <see cref="Ghash" />, whose kernels fold
/// runs of blocks rather than multiplying one at a time.
/// </para>
/// </remarks>
internal static class GaloisField128
{
    /// <summary>
    /// Doubles <paramref name="x" /> in <c>GF(2¹²⁸)</c> with big-endian bit order - the <c>dbl()</c> of RFC 5297 §2.3,
    /// AES-CMAC subkey derivation (RFC 4493), and the OCB offset ladder: a one-bit left shift with the reduction
    /// constant <c>0x87</c> folded into the last byte when the shifted-out bit was set.
    /// </summary>
    /// <param name="x">The input block (16 bytes).</param>
    /// <param name="result">The destination span (16 bytes); may be the same span as <paramref name="x" />.</param>
    /// <remarks>
    /// <para>
    /// Branch-free: the single secret-dependent decision - whether the most-significant bit of <paramref name="x" /> is
    /// set - is folded in through a <c>0x00</c>/<c>0xFF</c> mask instead of a branch, so control flow does not depend
    /// on the (typically key-derived) operand.
    /// </para>
    /// <para>
    /// This is the big-endian doubling used by the CMAC-based and OCB transforms. It is a distinct operation from the
    /// GHASH-domain <c>mulX</c> (a right shift folding <c>0xE1</c> into the first byte in the bit-reflected
    /// representation) that <see cref="Ghash.MultiplyByX" /> implements for POLYVAL - the two must not be consolidated.
    /// </para>
    /// </remarks>
    internal static void Double(ReadOnlySpan<byte> x, Span<byte> result)
    {
        // msbMask is 0xFF when the bit shifted out of the block is set, 0x00 otherwise. Captured before any write so
        // the ascending shift loop below remains correct when result fully aliases x.
        byte msbMask = (byte)(-(x[0] >> 7));

        for (int i = 0; i < 15; i++)
            result[i] = (byte)((x[i] << 1) | (x[i + 1] >> 7));

        result[15] = (byte)((x[15] << 1) ^ (0x87 & msbMask));
    }
}
