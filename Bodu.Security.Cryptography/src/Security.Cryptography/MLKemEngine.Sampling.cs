// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Sampling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM sampling routines (FIPS 203 Algorithms 7-8): uniform NTT-domain sampling by rejection from a
/// SHAKE128 stream and centered-binomial noise from a SHAKE256-based PRF.
/// </summary>
/// <remarks>
/// <para>
/// The matrix entries and the noise polynomials are each sampled from independent XOF streams. Where
/// <see cref="KeccakPermutation.IsFourWayAccelerated" /> holds, they are sampled four streams at a time through
/// <see cref="KeccakSponge4" />, whose sponges produce exactly the streams one <see cref="KeccakSponge" /> each would;
/// otherwise one at a time. The coefficients are the same either way.
/// </para>
/// <para>
/// A batch shorter than four leaves its idle sponges absorbing a copy of an active one's message; their output is never
/// read.
/// </para>
/// </remarks>
internal static partial class MLKemEngine
{
    /// <summary>The length of a matrix entry's XOF input: the 32-byte seed ρ and the two index bytes.</summary>
    private const int MatrixSeedInputLength = 32 + 2;

    /// <summary>The length of a noise polynomial's PRF input: the 32-byte seed and the counter byte.</summary>
    private const int NoiseSeedInputLength = 32 + 1;

    /// <summary>
    /// Samples every entry of the matrix Â, entry (i, j) from SHAKE128(ρ ‖ j ‖ i) (FIPS 203 Algorithm 13, lines 3-7).
    /// </summary>
    /// <param name="parameters">The parameter set supplying k.</param>
    /// <param name="rho">The 32-byte matrix seed.</param>
    /// <param name="matrix">
    /// The span receiving the k² NTT-domain polynomials, entry (i, j) at offset (i·k + j)·256.
    /// </param>
    private static void SampleMatrix(MLKemParameters parameters, ReadOnlySpan<byte> rho, Span<int> matrix) =>
        SampleMatrix(parameters, rho, matrix, KeccakPermutation.IsFourWayAccelerated);

    /// <summary>
    /// Samples every entry of the matrix Â, four streams at a time or one at a time as specified; both produce the same
    /// entries.
    /// </summary>
    /// <param name="parameters">The parameter set supplying k.</param>
    /// <param name="rho">The 32-byte matrix seed.</param>
    /// <param name="matrix">
    /// The span receiving the k² NTT-domain polynomials, entry (i, j) at offset (i·k + j)·256.
    /// </param>
    /// <param name="fourWay">
    /// <see langword="true" /> to sample four streams at a time through <see cref="KeccakSponge4" />;
    /// <see langword="false" /> to sample them one at a time.
    /// </param>
    internal static void SampleMatrix(MLKemParameters parameters, ReadOnlySpan<byte> rho, Span<int> matrix, bool fourWay)
    {
        int k = parameters.K;
        int entries = k * k;
        if (fourWay)
        {
            for (int first = 0; first < entries; first += KeccakSponge4.Ways)
                SampleNtts(rho, k, first, Math.Min(KeccakSponge4.Ways, entries - first), matrix);

            return;
        }

        for (int entry = 0; entry < entries; entry++)
            SampleNtt(rho, (byte)(entry % k), (byte)(entry / k), matrix.Slice(entry * N, N));
    }

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

        // FIPS 203 Algorithm 7 reads the XOF three bytes at a time. Squeezing a whole rate block - 56 triples, one
        // permutation - reads the same byte stream; the bytes left over once the polynomial is full are never used.
        Span<byte> block = stackalloc byte[KeccakSponge.Shake128RateBytes];
        int count = 0;
        while (count < N)
        {
            sponge.Squeeze(block);
            count = SampleNttBlock(block, destination, count);
        }

        sponge.Clear();
    }

    /// <summary>
    /// Samples up to four consecutive entries of the matrix at once, through the four-way SHAKE128.
    /// </summary>
    /// <param name="rho">The 32-byte public matrix seed.</param>
    /// <param name="k">The rank k, which maps an entry's position to its row i and column j.</param>
    /// <param name="first">The position of the first entry, counting row by row.</param>
    /// <param name="count">The number of entries, from 1 to 4.</param>
    /// <param name="matrix">The matrix, entry e at offset e·256.</param>
    private static void SampleNtts(ReadOnlySpan<byte> rho, int k, int first, int count, Span<int> matrix)
    {
        Span<byte> inputs = stackalloc byte[KeccakSponge4.Ways * MatrixSeedInputLength];
        for (int lane = 0; lane < KeccakSponge4.Ways; lane++)
        {
            int entry = first + Math.Min(lane, count - 1);
            Span<byte> input = inputs.Slice(lane * MatrixSeedInputLength, MatrixSeedInputLength);
            rho.CopyTo(input);
            input[32] = (byte)(entry % k);
            input[33] = (byte)(entry / k);
        }

        var sponge = KeccakSponge4.CreateShake128();
        sponge.Absorb(
            inputs[..MatrixSeedInputLength],
            inputs.Slice(MatrixSeedInputLength, MatrixSeedInputLength),
            inputs.Slice(2 * MatrixSeedInputLength, MatrixSeedInputLength),
            inputs.Slice(3 * MatrixSeedInputLength, MatrixSeedInputLength));

        Span<byte> blocks = stackalloc byte[KeccakSponge4.Ways * KeccakSponge.Shake128RateBytes];
        Span<byte> block0 = blocks[..KeccakSponge.Shake128RateBytes];
        Span<byte> block1 = blocks.Slice(KeccakSponge.Shake128RateBytes, KeccakSponge.Shake128RateBytes);
        Span<byte> block2 = blocks.Slice(2 * KeccakSponge.Shake128RateBytes, KeccakSponge.Shake128RateBytes);
        Span<byte> block3 = blocks.Slice(3 * KeccakSponge.Shake128RateBytes, KeccakSponge.Shake128RateBytes);

        // An idle lane starts full, so it is never parsed.
        Span<int> entry0 = matrix.Slice(first * N, N);
        Span<int> entry1 = count > 1 ? matrix.Slice((first + 1) * N, N) : default;
        Span<int> entry2 = count > 2 ? matrix.Slice((first + 2) * N, N) : default;
        Span<int> entry3 = count > 3 ? matrix.Slice((first + 3) * N, N) : default;
        int filled0 = 0;
        int filled1 = count > 1 ? 0 : N;
        int filled2 = count > 2 ? 0 : N;
        int filled3 = count > 3 ? 0 : N;

        while (filled0 < N || filled1 < N || filled2 < N || filled3 < N)
        {
            sponge.Squeeze(block0, block1, block2, block3);
            filled0 = SampleNttBlock(block0, entry0, filled0);
            filled1 = SampleNttBlock(block1, entry1, filled1);
            filled2 = SampleNttBlock(block2, entry2, filled2);
            filled3 = SampleNttBlock(block3, entry3, filled3);
        }

        sponge.Clear();
    }

    /// <summary>
    /// Adds the coefficients one squeezed SHAKE128 block yields to a polynomial being sampled: two 12-bit candidates
    /// per three bytes, each kept when it is below q.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <param name="destination">The polynomial being sampled.</param>
    /// <param name="count">The number of coefficients already sampled.</param>
    /// <returns>The number of coefficients sampled after the block, at most 256.</returns>
    private static int SampleNttBlock(ReadOnlySpan<byte> block, Span<int> destination, int count)
    {
        for (int offset = 0; offset + 3 <= block.Length && count < N; offset += 3)
        {
            int d1 = block[offset] | ((block[offset + 1] & 0x0F) << 8);
            int d2 = (block[offset + 1] >> 4) | (block[offset + 2] << 4);

            if (d1 < Q)
                destination[count++] = d1;

            if (d2 < Q && count < N)
                destination[count++] = d2;
        }

        return count;
    }

    /// <summary>
    /// Samples a vector of centered-binomial noise polynomials, polynomial r from SHAKE256(seed ‖ (counterBase + r))
    /// (FIPS 203 Algorithm 8 with the PRF of §4.1).
    /// </summary>
    /// <param name="eta">The CBD parameter η (2 or 3).</param>
    /// <param name="seed">The 32-byte PRF seed (σ or r).</param>
    /// <param name="counterBase">The first polynomial's counter byte N; each next polynomial's is one more.</param>
    /// <param name="vector">The span receiving the polynomials in [0, q); its length fixes their number.</param>
    private static void SampleNoiseVector(int eta, ReadOnlySpan<byte> seed, int counterBase, Span<int> vector) =>
        SampleNoiseVector(eta, seed, counterBase, vector, KeccakPermutation.IsFourWayAccelerated);

    /// <summary>
    /// Samples a vector of centered-binomial noise polynomials, four streams at a time or one at a time as specified;
    /// both produce the same polynomials.
    /// </summary>
    /// <param name="eta">The CBD parameter η (2 or 3).</param>
    /// <param name="seed">The 32-byte PRF seed (σ or r).</param>
    /// <param name="counterBase">The first polynomial's counter byte N; each next polynomial's is one more.</param>
    /// <param name="vector">The span receiving the polynomials in [0, q); its length fixes their number.</param>
    /// <param name="fourWay">
    /// <see langword="true" /> to sample four streams at a time through <see cref="KeccakSponge4" />;
    /// <see langword="false" /> to sample them one at a time.
    /// </param>
    internal static void SampleNoiseVector(int eta, ReadOnlySpan<byte> seed, int counterBase, Span<int> vector, bool fourWay)
    {
        int polynomials = vector.Length / N;
        if (fourWay)
        {
            for (int first = 0; first < polynomials; first += KeccakSponge4.Ways)
                SamplePolyCbds(eta, seed, counterBase, first, Math.Min(KeccakSponge4.Ways, polynomials - first), vector);

            return;
        }

        for (int r = 0; r < polynomials; r++)
            SamplePolyCbd(eta, seed, (byte)(counterBase + r), vector.Slice(r * N, N));
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
    internal static void SamplePolyCbd(int eta, ReadOnlySpan<byte> seed, byte counter, Span<int> destination)
    {
        ThrowHelper.ThrowIfLessThan(destination.Length, N, nameof(destination));

        Span<byte> counterByte = stackalloc byte[1];
        counterByte[0] = counter;

        Span<byte> stream = stackalloc byte[64 * 3];
        Span<byte> bytes = stream[..(64 * eta)];
        KeccakSponge.Shake256(seed, counterByte, bytes);

        CenteredBinomial(eta, bytes, destination);
        CryptographyHelper.Clear(stream);
    }

    /// <summary>
    /// Samples up to four noise polynomials of a vector at once, through the four-way SHAKE256.
    /// </summary>
    /// <param name="eta">The CBD parameter η (2 or 3).</param>
    /// <param name="seed">The 32-byte PRF seed (σ or r).</param>
    /// <param name="counterBase">The counter byte of the vector's first polynomial.</param>
    /// <param name="first">The index of the first polynomial to sample.</param>
    /// <param name="count">The number of polynomials, from 1 to 4.</param>
    /// <param name="vector">The vector, polynomial r at offset r·256.</param>
    /// <remarks>
    /// One block holds the 128 bytes η = 2 needs and two the 192 bytes η = 3 needs; the bytes past them are never read.
    /// </remarks>
    private static void SamplePolyCbds(int eta, ReadOnlySpan<byte> seed, int counterBase, int first, int count, Span<int> vector)
    {
        Span<byte> inputs = stackalloc byte[KeccakSponge4.Ways * NoiseSeedInputLength];
        for (int lane = 0; lane < KeccakSponge4.Ways; lane++)
        {
            Span<byte> input = inputs.Slice(lane * NoiseSeedInputLength, NoiseSeedInputLength);
            seed.CopyTo(input);
            input[32] = (byte)(counterBase + first + Math.Min(lane, count - 1));
        }

        var sponge = KeccakSponge4.CreateShake256();
        sponge.Absorb(
            inputs[..NoiseSeedInputLength],
            inputs.Slice(NoiseSeedInputLength, NoiseSeedInputLength),
            inputs.Slice(2 * NoiseSeedInputLength, NoiseSeedInputLength),
            inputs.Slice(3 * NoiseSeedInputLength, NoiseSeedInputLength));
        CryptographyHelper.Clear(inputs);

        // The streams determine secret noise, so they are cleared afterwards.
        int streamBytes = (((64 * eta) + KeccakSponge.Shake256RateBytes - 1) / KeccakSponge.Shake256RateBytes) * KeccakSponge.Shake256RateBytes;
        Span<byte> streams = stackalloc byte[KeccakSponge4.Ways * 2 * KeccakSponge.Shake256RateBytes];
        streams = streams[..(KeccakSponge4.Ways * streamBytes)];
        sponge.Squeeze(
            streams[..streamBytes],
            streams.Slice(streamBytes, streamBytes),
            streams.Slice(2 * streamBytes, streamBytes),
            streams.Slice(3 * streamBytes, streamBytes));
        sponge.Clear();

        for (int lane = 0; lane < count; lane++)
            CenteredBinomial(eta, streams.Slice(lane * streamBytes, 64 * eta), vector.Slice((first + lane) * N, N));

        CryptographyHelper.Clear(streams);
    }

    /// <summary>
    /// Forms a centered-binomial polynomial from 64η PRF bytes: coefficient i is the sum of η stream bits minus the sum
    /// of the next η.
    /// </summary>
    /// <param name="eta">The CBD parameter η (2 or 3).</param>
    /// <param name="bytes">The 64η bytes.</param>
    /// <param name="destination">The span receiving 256 coefficients in [0, q).</param>
    /// <remarks>
    /// The bits are counted a word at a time: adding a word's bits to its bits shifted right by one (and, for η = 3, by
    /// two) leaves each η-bit field holding the count of its own bits, from which every coefficient in the word reads
    /// off with a shift and a mask.
    /// </remarks>
    private static void CenteredBinomial(int eta, ReadOnlySpan<byte> bytes, Span<int> destination)
    {
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
    }
}
