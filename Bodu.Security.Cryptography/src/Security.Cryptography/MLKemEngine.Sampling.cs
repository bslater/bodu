// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Sampling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM sampling routines (FIPS 203 Algorithms 7–8): uniform NTT-domain sampling by rejection from a
/// SHAKE128 stream and centered-binomial noise from a SHAKE256-based PRF.
/// </summary>
internal static partial class MLKemEngine
{
    /// <summary>
    /// Samples a uniformly random NTT-domain polynomial from the XOF stream SHAKE128(ρ ‖ index1 ‖ index2) by rejection
    /// (FIPS 203 Algorithm 7 / SampleNTT).
    /// </summary>
    /// <param name="rho">The 32-byte public matrix seed.</param>
    /// <param name="index1">The first index byte absorbed after the seed.</param>
    /// <param name="index2">The second index byte absorbed after the seed.</param>
    /// <param name="destination">The span receiving 256 coefficients in [0, q).</param>
    /// <remarks>
    /// Rejection sampling consumes an unbounded number of squeeze blocks, which is why
    /// <see cref="KeccakSponge.Squeeze" /> supports streaming output. The matrix is public, so variable-time rejection
    /// here leaks nothing secret.
    /// </remarks>
    private static void SampleNtt(ReadOnlySpan<byte> rho, byte index1, byte index2, Span<int> destination)
    {
        var sponge = KeccakSponge.CreateShake128();
        sponge.Absorb(rho);

        Span<byte> indices = stackalloc byte[2];
        indices[0] = index1;
        indices[1] = index2;
        sponge.Absorb(indices);

        Span<byte> block = stackalloc byte[3];
        int count = 0;
        while (count < N)
        {
            sponge.Squeeze(block);

            int d1 = block[0] | ((block[1] & 0x0F) << 8);
            int d2 = (block[1] >> 4) | (block[2] << 4);

            if (d1 < Q)
                destination[count++] = d1;

            if (d2 < Q && count < N)
                destination[count++] = d2;
        }

        sponge.Clear();
    }

    /// <summary>
    /// Samples a centered-binomial-distribution polynomial from the PRF stream SHAKE256(seed ‖ counter) (FIPS 203
    /// Algorithm 8 / SamplePolyCBD with the PRF of §4.1).
    /// </summary>
    /// <param name="eta">The CBD parameter η (2 or 3); the PRF supplies 64η bytes.</param>
    /// <param name="seed">The 32-byte PRF seed (σ or r).</param>
    /// <param name="counter">The domain-separation counter byte N.</param>
    /// <param name="destination">The span receiving 256 coefficients in [0, q).</param>
    private static void SamplePolyCbd(int eta, ReadOnlySpan<byte> seed, byte counter, Span<int> destination)
    {
        Span<byte> counterByte = stackalloc byte[1];
        counterByte[0] = counter;

        Span<byte> stream = stackalloc byte[64 * 3];
        Span<byte> bytes = stream[..(64 * eta)];
        KeccakSponge.Shake256(seed, counterByte, bytes);

        for (int i = 0; i < N; i++)
        {
            int positive = 0;
            int negative = 0;

            for (int j = 0; j < eta; j++)
            {
                positive += GetBit(bytes, (2 * i * eta) + j);
                negative += GetBit(bytes, (2 * i * eta) + eta + j);
            }

            destination[i] = (positive - negative + Q) % Q;
        }

        CryptographyHelper.Clear(stream);
    }

    /// <summary>
    /// Reads a single bit from a little-endian bit stream.
    /// </summary>
    /// <param name="bytes">The source bytes.</param>
    /// <param name="bitIndex">The zero-based bit index.</param>
    /// <returns>0 or 1.</returns>
    private static int GetBit(ReadOnlySpan<byte> bytes, int bitIndex) =>
        (bytes[bitIndex >> 3] >> (bitIndex & 7)) & 1;
}
