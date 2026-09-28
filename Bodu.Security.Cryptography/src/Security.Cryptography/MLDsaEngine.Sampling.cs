// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngine.Sampling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-DSA expansion and sampling routines (FIPS 204 Algorithms 29–34): matrix expansion by rejection from
/// SHAKE128, bounded secret sampling and mask expansion from SHAKE256, and the SampleInBall challenge sampler.
/// </summary>
internal static partial class MLDsaEngine
{
    /// <summary>
    /// Samples one uniformly random NTT-domain matrix polynomial Â[r][s] from SHAKE128(ρ ‖ s ‖ r) by three-byte
    /// rejection (FIPS 204 Algorithms 30 and 32 with CoeffFromThreeBytes).
    /// </summary>
    /// <param name="rho">The 32-byte public matrix seed.</param>
    /// <param name="column">The column index s, absorbed first.</param>
    /// <param name="row">The row index r, absorbed second.</param>
    /// <param name="destination">The span receiving 256 coefficients in [0, q).</param>
    private static void RejNttPoly(ReadOnlySpan<byte> rho, byte column, byte row, Span<int> destination)
    {
        var sponge = KeccakSponge.CreateShake128();
        sponge.Absorb(rho);

        Span<byte> indices = stackalloc byte[2];
        indices[0] = column;
        indices[1] = row;
        sponge.Absorb(indices);

        // FIPS 204 Algorithm 30 reads the XOF three bytes at a time. Squeezing a whole rate block — 56 triples, one
        // permutation — reads the same byte stream; the bytes left over once the polynomial is full are never used.
        Span<byte> block = stackalloc byte[KeccakSponge.Shake128RateBytes];
        int count = 0;
        while (count < N)
        {
            sponge.Squeeze(block);

            for (int offset = 0; offset < block.Length && count < N; offset += 3)
            {
                int candidate = block[offset] | (block[offset + 1] << 8) | ((block[offset + 2] & 0x7F) << 16);
                if (candidate < Q)
                    destination[count++] = candidate;
            }
        }

        sponge.Clear();
    }

    /// <summary>
    /// Samples one secret polynomial with coefficients in [−η, η] from SHAKE256(ρ′ ‖ nonce) by half-byte rejection
    /// (FIPS 204 Algorithms 31 and 33 with CoeffFromHalfByte).
    /// </summary>
    /// <param name="eta">The bound η (2 or 4).</param>
    /// <param name="rhoPrime">The 64-byte secret expansion seed.</param>
    /// <param name="nonce">The 16-bit domain-separation nonce, absorbed little-endian.</param>
    /// <param name="destination">
    /// The span receiving 256 coefficients in [0, q), centered values folded modulo q.
    /// </param>
    private static void RejBoundedPoly(int eta, ReadOnlySpan<byte> rhoPrime, int nonce, Span<int> destination)
    {
        var sponge = KeccakSponge.CreateShake256();
        sponge.Absorb(rhoPrime);

        Span<byte> nonceBytes = stackalloc byte[2];
        nonceBytes[0] = (byte)nonce;
        nonceBytes[1] = (byte)(nonce >> 8);
        sponge.Absorb(nonceBytes);

        // FIPS 204 Algorithm 31 reads the XOF a byte at a time. Squeezing a whole rate block — one permutation — reads
        // the same byte stream. The stream determines the secret coefficients, so the block is cleared afterwards.
        Span<byte> block = stackalloc byte[KeccakSponge.Shake256RateBytes];
        int count = 0;
        while (count < N)
        {
            sponge.Squeeze(block);

            for (int offset = 0; offset < block.Length && count < N; offset++)
            {
                int low = block[offset] & 0x0F;
                int high = block[offset] >> 4;

                if (TryCoeffFromHalfByte(eta, low, out int first) && count < N)
                    destination[count++] = first;

                if (TryCoeffFromHalfByte(eta, high, out int second) && count < N)
                    destination[count++] = second;
            }
        }

        CryptographyHelper.Clear(block);
        sponge.Clear();
    }

    /// <summary>
    /// Maps a half-byte onto a coefficient in [−η, η] folded into [0, q), rejecting out-of-range values (FIPS 204
    /// Algorithm 15 / CoeffFromHalfByte).
    /// </summary>
    /// <param name="eta">The bound η (2 or 4).</param>
    /// <param name="halfByte">The candidate half-byte in [0, 16).</param>
    /// <param name="coefficient">Receives the accepted coefficient.</param>
    /// <returns><see langword="true" /> when the candidate was accepted; otherwise, <see langword="false" />.</returns>
    private static bool TryCoeffFromHalfByte(int eta, int halfByte, out int coefficient)
    {
        if (eta == 2 && halfByte < 15)
        {
            coefficient = Canonicalize(2 - (halfByte % 5));
            return true;
        }

        if (eta == 4 && halfByte < 9)
        {
            coefficient = Canonicalize(4 - halfByte);
            return true;
        }

        coefficient = 0;
        return false;
    }

    /// <summary>
    /// Expands one mask polynomial y[r] with coefficients in [−γ₁ + 1, γ₁] from SHAKE256(ρ″ ‖ IntegerToBytes(κ + r, 2))
    /// (FIPS 204 Algorithm 34 / ExpandMask).
    /// </summary>
    /// <param name="parameters">The parameter set supplying γ₁ and its packing width.</param>
    /// <param name="rhoDoublePrime">The 64-byte per-message seed ρ″.</param>
    /// <param name="nonce">The value κ + r, absorbed as two little-endian bytes.</param>
    /// <param name="destination">
    /// The span receiving 256 coefficients in [0, q), centered values folded modulo q.
    /// </param>
    private static void ExpandMask(MLDsaParameters parameters, ReadOnlySpan<byte> rhoDoublePrime, int nonce, Span<int> destination)
    {
        Span<byte> nonceBytes = stackalloc byte[2];
        nonceBytes[0] = (byte)nonce;
        nonceBytes[1] = (byte)(nonce >> 8);

        Span<byte> packed = stackalloc byte[32 * 20];
        Span<byte> stream = packed[..(32 * parameters.Gamma1Bits)];
        KeccakSponge.Shake256(rhoDoublePrime, nonceBytes, stream);

        BitUnpackSigned(parameters.Gamma1Bits, parameters.Gamma1, stream, destination);
        CryptographyHelper.Clear(packed);
    }

    /// <summary>
    /// Samples the challenge polynomial with exactly τ coefficients of ±1 from the commitment hash c̃ (FIPS 204
    /// Algorithm 29 / SampleInBall).
    /// </summary>
    /// <param name="parameters">The parameter set supplying τ.</param>
    /// <param name="commitmentHash">The λ/4-byte commitment hash c̃, absorbed in full.</param>
    /// <param name="destination">The span receiving 256 coefficients in [0, q): τ entries of ±1, the rest zero.</param>
    private static void SampleInBall(MLDsaParameters parameters, ReadOnlySpan<byte> commitmentHash, Span<int> destination)
    {
        destination.Clear();

        var sponge = KeccakSponge.CreateShake256();
        sponge.Absorb(commitmentHash);

        // FIPS 204 Algorithm 29 reads eight sign bytes and then one byte per candidate index from a single XOF stream.
        // Squeezing a whole rate block at a time reads the same stream, one permutation per block.
        Span<byte> block = stackalloc byte[KeccakSponge.Shake256RateBytes];
        sponge.Squeeze(block);

        Span<byte> signBytes = stackalloc byte[8];
        block[..8].CopyTo(signBytes);
        int position = 8;
        int signIndex = 0;

        for (int i = N - parameters.Tau; i < N; i++)
        {
            int j;
            do
            {
                if (position == block.Length)
                {
                    sponge.Squeeze(block);
                    position = 0;
                }

                j = block[position++];
            }
            while (j > i);

            int sign = (signBytes[signIndex >> 3] >> (signIndex & 7)) & 1;
            signIndex++;

            destination[i] = destination[j];
            destination[j] = sign == 1 ? Q - 1 : 1;
        }

        sponge.Clear();
    }
}
