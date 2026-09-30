// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Sampling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

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

        // FIPS 203 Algorithm 7 reads the XOF three bytes at a time. Squeezing a whole rate block — 56 triples, one
        // permutation — reads the same byte stream; the bytes left over once the polynomial is full are never used.
        Span<byte> block = stackalloc byte[KeccakSponge.Shake128RateBytes];
        int count = 0;
        while (count < N)
        {
            sponge.Squeeze(block);

            for (int offset = 0; offset < block.Length && count < N; offset += 3)
            {
                int d1 = block[offset] | ((block[offset + 1] & 0x0F) << 8);
                int d2 = (block[offset + 1] >> 4) | (block[offset + 2] << 4);

                if (d1 < Q)
                    destination[count++] = d1;

                if (d2 < Q && count < N)
                    destination[count++] = d2;
            }
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
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="destination" /> holds fewer than 256 coefficients.
    /// </exception>
    /// <remarks>
    /// Coefficient i is the sum of η stream bits minus the sum of the next η. The bits are counted a word at a time:
    /// adding a word's bits to its bits shifted right by one (and, for η = 3, by two) leaves each η-bit field holding
    /// the count of its own bits, from which every coefficient in the word reads off with a shift and a mask.
    /// </remarks>
    internal static void SamplePolyCbd(int eta, ReadOnlySpan<byte> seed, byte counter, Span<int> destination)
    {
        ThrowHelper.ThrowIfLessThan(destination.Length, N, nameof(destination));

        Span<byte> counterByte = stackalloc byte[1];
        counterByte[0] = counter;

        Span<byte> stream = stackalloc byte[64 * 3];
        Span<byte> bytes = stream[..(64 * eta)];
        KeccakSponge.Shake256(seed, counterByte, bytes);

        if (eta == 2)
        {
            // Four bytes hold eight coefficients of four bits each: two bits for the positive count, two for the negative.
            for (int i = 0; i < N / 8; i++)
            {
                uint word = BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4 * i, 4));
                uint counts = (word & 0x5555_5555) + ((word >> 1) & 0x5555_5555);

                for (int j = 0; j < 8; j++)
                {
                    int positive = (int)((counts >> (4 * j)) & 3);
                    int negative = (int)((counts >> ((4 * j) + 2)) & 3);
                    destination[(8 * i) + j] = Canonicalize(positive - negative);
                }
            }
        }
        else
        {
            // Three bytes hold four coefficients of six bits each: three bits for the positive count, three for the
            // negative.
            for (int i = 0; i < N / 4; i++)
            {
                uint word = (uint)(bytes[3 * i] | (bytes[(3 * i) + 1] << 8) | (bytes[(3 * i) + 2] << 16));
                uint counts = (word & 0x24_9249) + ((word >> 1) & 0x24_9249) + ((word >> 2) & 0x24_9249);

                for (int j = 0; j < 4; j++)
                {
                    int positive = (int)((counts >> (6 * j)) & 7);
                    int negative = (int)((counts >> ((6 * j) + 3)) & 7);
                    destination[(4 * i) + j] = Canonicalize(positive - negative);
                }
            }
        }

        CryptographyHelper.Clear(stream);
    }
}
