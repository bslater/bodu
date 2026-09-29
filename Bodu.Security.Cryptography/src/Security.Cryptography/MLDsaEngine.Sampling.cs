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
/// <remarks>
/// <para>
/// The matrix, the secret vectors and the masks are each expanded from independent XOF streams. Where
/// <see cref="KeccakPermutation.IsFourWayAccelerated" /> holds, they are expanded four streams at a time through
/// <see cref="KeccakSponge4" />, whose sponges produce exactly the streams one <see cref="KeccakSponge" /> each would;
/// otherwise one at a time. The coefficients are the same either way.
/// </para>
/// <para>
/// A batch shorter than four leaves its idle sponges absorbing a copy of an active one's message; their output is never
/// read.
/// </para>
/// </remarks>
internal static partial class MLDsaEngine
{
    /// <summary>The length of a matrix entry's XOF input: the 32-byte seed ρ and the two index bytes.</summary>
    private const int MatrixSeedInputLength = 32 + 2;

    /// <summary>The length of a secret or mask XOF input: a 64-byte seed and the two nonce bytes.</summary>
    private const int NoncedSeedInputLength = 64 + 2;

    /// <summary>The number of SHAKE256 blocks that hold a mask polynomial's 32·(bitlen(γ₁ − 1) + 1) bytes.</summary>
    private const int MaskBlocks = 5;

    /// <summary>
    /// Samples the entries of the public matrix, each in the NTT domain, from SHAKE128(ρ ‖ s ‖ r) by three-byte
    /// rejection (FIPS 204 Algorithms 30 and 32 with CoeffFromThreeBytes).
    /// </summary>
    /// <param name="parameters">The parameter set supplying k and ℓ.</param>
    /// <param name="rho">The 32-byte public matrix seed.</param>
    /// <param name="matrix">The span receiving the k·ℓ entries, Â[r][s] at offset (r·ℓ + s)·256, in [0, q).</param>
    private static void SampleMatrix(MLDsaParameters parameters, ReadOnlySpan<byte> rho, Span<int> matrix) =>
        SampleMatrix(parameters, rho, matrix, KeccakPermutation.IsFourWayAccelerated);

    /// <summary>
    /// Samples the entries of the public matrix, four streams at a time or one at a time as specified; both produce the
    /// same entries.
    /// </summary>
    /// <param name="parameters">The parameter set supplying k and ℓ.</param>
    /// <param name="rho">The 32-byte public matrix seed.</param>
    /// <param name="matrix">The span receiving the k·ℓ entries, Â[r][s] at offset (r·ℓ + s)·256, in [0, q).</param>
    /// <param name="fourWay">
    /// <see langword="true" /> to sample four streams at a time through <see cref="KeccakSponge4" />;
    /// <see langword="false" /> to sample them one at a time.
    /// </param>
    internal static void SampleMatrix(MLDsaParameters parameters, ReadOnlySpan<byte> rho, Span<int> matrix, bool fourWay)
    {
        int entries = parameters.K * parameters.L;
        if (fourWay)
        {
            for (int first = 0; first < entries; first += KeccakSponge4.Ways)
                RejNttPolys(rho, parameters.L, first, Math.Min(KeccakSponge4.Ways, entries - first), matrix);

            return;
        }

        for (int entry = 0; entry < entries; entry++)
            RejNttPoly(rho, (byte)(entry % parameters.L), (byte)(entry / parameters.L), matrix.Slice(entry * N, N));
    }

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
            count = RejNttBlock(block, destination, count);
        }

        sponge.Clear();
    }

    /// <summary>
    /// Samples up to four consecutive entries of the matrix at once, through the four-way SHAKE128.
    /// </summary>
    /// <param name="rho">The 32-byte public matrix seed.</param>
    /// <param name="columns">The number of columns ℓ, which maps an entry's position to its row and column.</param>
    /// <param name="first">The position of the first entry, counting row by row.</param>
    /// <param name="count">The number of entries, from 1 to 4.</param>
    /// <param name="matrix">The matrix, entry e at offset e·256.</param>
    private static void RejNttPolys(ReadOnlySpan<byte> rho, int columns, int first, int count, Span<int> matrix)
    {
        Span<byte> inputs = stackalloc byte[KeccakSponge4.Ways * MatrixSeedInputLength];
        for (int lane = 0; lane < KeccakSponge4.Ways; lane++)
        {
            int entry = first + Math.Min(lane, count - 1);
            Span<byte> input = inputs.Slice(lane * MatrixSeedInputLength, MatrixSeedInputLength);
            rho.CopyTo(input);
            input[32] = (byte)(entry % columns);
            input[33] = (byte)(entry / columns);
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
            filled0 = RejNttBlock(block0, entry0, filled0);
            filled1 = RejNttBlock(block1, entry1, filled1);
            filled2 = RejNttBlock(block2, entry2, filled2);
            filled3 = RejNttBlock(block3, entry3, filled3);
        }

        sponge.Clear();
    }

    /// <summary>
    /// Adds the coefficients one squeezed SHAKE128 block yields to a matrix polynomial being sampled, three bytes per
    /// candidate, keeping each candidate below q (FIPS 204 Algorithm 14 / CoeffFromThreeBytes).
    /// </summary>
    /// <param name="block">The block.</param>
    /// <param name="destination">The polynomial being sampled.</param>
    /// <param name="count">The number of coefficients already sampled.</param>
    /// <returns>The number of coefficients sampled after the block, at most 256.</returns>
    private static int RejNttBlock(ReadOnlySpan<byte> block, Span<int> destination, int count)
    {
        for (int offset = 0; offset + 3 <= block.Length && count < N; offset += 3)
        {
            int candidate = block[offset] | (block[offset + 1] << 8) | ((block[offset + 2] & 0x7F) << 16);
            if (candidate < Q)
                destination[count++] = candidate;
        }

        return count;
    }

    /// <summary>
    /// Samples a vector of bounded secret polynomials via ExpandS (FIPS 204 Algorithm 33).
    /// </summary>
    /// <param name="parameters">The parameter set supplying η.</param>
    /// <param name="rhoPrime">The 64-byte secret expansion seed.</param>
    /// <param name="nonceBase">The first polynomial's nonce; each next polynomial's is one more.</param>
    /// <param name="vector">
    /// The span receiving the polynomials, centered coefficients folded into [0, q); its length fixes their number.
    /// </param>
    private static void SampleSecretVector(MLDsaParameters parameters, ReadOnlySpan<byte> rhoPrime, int nonceBase, Span<int> vector) =>
        SampleSecretVector(parameters, rhoPrime, nonceBase, vector, KeccakPermutation.IsFourWayAccelerated);

    /// <summary>
    /// Samples a vector of bounded secret polynomials via ExpandS (FIPS 204 Algorithm 33), four streams at a time or
    /// one at a time as specified; both produce the same polynomials.
    /// </summary>
    /// <param name="parameters">The parameter set supplying η.</param>
    /// <param name="rhoPrime">The 64-byte secret expansion seed.</param>
    /// <param name="nonceBase">The first polynomial's nonce; each next polynomial's is one more.</param>
    /// <param name="vector">
    /// The span receiving the polynomials, centered coefficients folded into [0, q); its length fixes their number.
    /// </param>
    /// <param name="fourWay">
    /// <see langword="true" /> to sample four streams at a time through <see cref="KeccakSponge4" />;
    /// <see langword="false" /> to sample them one at a time.
    /// </param>
    internal static void SampleSecretVector(MLDsaParameters parameters, ReadOnlySpan<byte> rhoPrime, int nonceBase, Span<int> vector, bool fourWay)
    {
        int polynomials = vector.Length / N;
        if (fourWay)
        {
            for (int first = 0; first < polynomials; first += KeccakSponge4.Ways)
                RejBoundedPolys(parameters.Eta, rhoPrime, nonceBase, first, Math.Min(KeccakSponge4.Ways, polynomials - first), vector);

            return;
        }

        for (int r = 0; r < polynomials; r++)
            RejBoundedPoly(parameters.Eta, rhoPrime, nonceBase + r, vector.Slice(r * N, N));
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
            count = RejBoundedBlock(eta, block, destination, count);
        }

        CryptographyHelper.Clear(block);
        sponge.Clear();
    }

    /// <summary>
    /// Samples up to four consecutive polynomials of a secret vector at once, through the four-way SHAKE256.
    /// </summary>
    /// <param name="eta">The bound η (2 or 4).</param>
    /// <param name="rhoPrime">The 64-byte secret expansion seed.</param>
    /// <param name="nonceBase">The nonce of the vector's first polynomial.</param>
    /// <param name="first">The index of the first polynomial to sample.</param>
    /// <param name="count">The number of polynomials, from 1 to 4.</param>
    /// <param name="vector">The vector, polynomial i at offset i·256.</param>
    private static void RejBoundedPolys(int eta, ReadOnlySpan<byte> rhoPrime, int nonceBase, int first, int count, Span<int> vector)
    {
        var sponge = KeccakSponge4.CreateShake256();
        AbsorbNoncedSeeds(ref sponge, rhoPrime, nonceBase + first, count);

        // The streams determine secret coefficients, so the blocks are cleared afterwards.
        Span<byte> blocks = stackalloc byte[KeccakSponge4.Ways * KeccakSponge.Shake256RateBytes];
        Span<byte> block0 = blocks[..KeccakSponge.Shake256RateBytes];
        Span<byte> block1 = blocks.Slice(KeccakSponge.Shake256RateBytes, KeccakSponge.Shake256RateBytes);
        Span<byte> block2 = blocks.Slice(2 * KeccakSponge.Shake256RateBytes, KeccakSponge.Shake256RateBytes);
        Span<byte> block3 = blocks.Slice(3 * KeccakSponge.Shake256RateBytes, KeccakSponge.Shake256RateBytes);

        Span<int> poly0 = vector.Slice(first * N, N);
        Span<int> poly1 = count > 1 ? vector.Slice((first + 1) * N, N) : default;
        Span<int> poly2 = count > 2 ? vector.Slice((first + 2) * N, N) : default;
        Span<int> poly3 = count > 3 ? vector.Slice((first + 3) * N, N) : default;
        int filled0 = 0;
        int filled1 = count > 1 ? 0 : N;
        int filled2 = count > 2 ? 0 : N;
        int filled3 = count > 3 ? 0 : N;

        while (filled0 < N || filled1 < N || filled2 < N || filled3 < N)
        {
            sponge.Squeeze(block0, block1, block2, block3);
            filled0 = RejBoundedBlock(eta, block0, poly0, filled0);
            filled1 = RejBoundedBlock(eta, block1, poly1, filled1);
            filled2 = RejBoundedBlock(eta, block2, poly2, filled2);
            filled3 = RejBoundedBlock(eta, block3, poly3, filled3);
        }

        CryptographyHelper.Clear(blocks);
        sponge.Clear();
    }

    /// <summary>
    /// Adds the coefficients one squeezed SHAKE256 block yields to a secret polynomial being sampled, two half-bytes
    /// per byte, each kept when it maps into [−η, η].
    /// </summary>
    /// <param name="eta">The bound η (2 or 4).</param>
    /// <param name="block">The block.</param>
    /// <param name="destination">The polynomial being sampled.</param>
    /// <param name="count">The number of coefficients already sampled.</param>
    /// <returns>The number of coefficients sampled after the block, at most 256.</returns>
    private static int RejBoundedBlock(int eta, ReadOnlySpan<byte> block, Span<int> destination, int count)
    {
        for (int offset = 0; offset < block.Length && count < N; offset++)
        {
            int low = block[offset] & 0x0F;
            int high = block[offset] >> 4;

            if (TryCoeffFromHalfByte(eta, low, out int first) && count < N)
                destination[count++] = first;

            if (TryCoeffFromHalfByte(eta, high, out int second) && count < N)
                destination[count++] = second;
        }

        return count;
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
    /// Expands the mask vector y with coefficients in [−γ₁ + 1, γ₁], polynomial r from SHAKE256(ρ″ ‖ IntegerToBytes(κ +
    /// r, 2)) (FIPS 204 Algorithm 34 / ExpandMask).
    /// </summary>
    /// <param name="parameters">The parameter set supplying ℓ, γ₁ and its packing width.</param>
    /// <param name="rhoDoublePrime">The 64-byte per-message seed ρ″.</param>
    /// <param name="kappa">The attempt's nonce offset κ.</param>
    /// <param name="y">The span receiving the ℓ polynomials, centered coefficients folded into [0, q).</param>
    private static void ExpandMaskVector(MLDsaParameters parameters, ReadOnlySpan<byte> rhoDoublePrime, int kappa, Span<int> y) =>
        ExpandMaskVector(parameters, rhoDoublePrime, kappa, y, KeccakPermutation.IsFourWayAccelerated);

    /// <summary>
    /// Expands the mask vector y (FIPS 204 Algorithm 34 / ExpandMask), four streams at a time or one at a time as
    /// specified; both produce the same polynomials.
    /// </summary>
    /// <param name="parameters">The parameter set supplying ℓ, γ₁ and its packing width.</param>
    /// <param name="rhoDoublePrime">The 64-byte per-message seed ρ″.</param>
    /// <param name="kappa">The attempt's nonce offset κ.</param>
    /// <param name="y">The span receiving the ℓ polynomials, centered coefficients folded into [0, q).</param>
    /// <param name="fourWay">
    /// <see langword="true" /> to sample four streams at a time through <see cref="KeccakSponge4" />;
    /// <see langword="false" /> to sample them one at a time.
    /// </param>
    internal static void ExpandMaskVector(MLDsaParameters parameters, ReadOnlySpan<byte> rhoDoublePrime, int kappa, Span<int> y, bool fourWay)
    {
        if (fourWay)
        {
            for (int first = 0; first < parameters.L; first += KeccakSponge4.Ways)
                ExpandMasks(parameters, rhoDoublePrime, kappa, first, Math.Min(KeccakSponge4.Ways, parameters.L - first), y);

            return;
        }

        for (int r = 0; r < parameters.L; r++)
            ExpandMask(parameters, rhoDoublePrime, kappa + r, y.Slice(r * N, N));
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
    /// Expands up to four consecutive mask polynomials at once, through the four-way SHAKE256.
    /// </summary>
    /// <param name="parameters">The parameter set supplying γ₁ and its packing width.</param>
    /// <param name="rhoDoublePrime">The 64-byte per-message seed ρ″.</param>
    /// <param name="kappa">The attempt's nonce offset κ.</param>
    /// <param name="first">The index r of the first polynomial to expand.</param>
    /// <param name="count">The number of polynomials, from 1 to 4.</param>
    /// <param name="y">The mask vector, polynomial r at offset r·256.</param>
    /// <remarks>
    /// Five blocks cover the 576 or 640 bytes a mask polynomial packs into; the bytes past them are never read.
    /// </remarks>
    private static void ExpandMasks(MLDsaParameters parameters, ReadOnlySpan<byte> rhoDoublePrime, int kappa, int first, int count, Span<int> y)
    {
        var sponge = KeccakSponge4.CreateShake256();
        AbsorbNoncedSeeds(ref sponge, rhoDoublePrime, kappa + first, count);

        // The streams determine the masks, and with them the signature's secrets, so they are cleared afterwards.
        const int StreamBytes = MaskBlocks * KeccakSponge.Shake256RateBytes;
        Span<byte> streams = stackalloc byte[KeccakSponge4.Ways * StreamBytes];
        sponge.Squeeze(
            streams[..StreamBytes],
            streams.Slice(StreamBytes, StreamBytes),
            streams.Slice(2 * StreamBytes, StreamBytes),
            streams.Slice(3 * StreamBytes, StreamBytes));
        sponge.Clear();

        int packedBytes = 32 * parameters.Gamma1Bits;
        for (int lane = 0; lane < count; lane++)
            BitUnpackSigned(parameters.Gamma1Bits, parameters.Gamma1, streams.Slice(lane * StreamBytes, packedBytes), y.Slice((first + lane) * N, N));

        CryptographyHelper.Clear(streams);
    }

    /// <summary>
    /// Absorbs a 64-byte seed followed by consecutive two-byte little-endian nonces into the four sponges, one nonce
    /// each.
    /// </summary>
    /// <param name="sponge">The four sponges, fresh.</param>
    /// <param name="seed">The 64-byte seed.</param>
    /// <param name="firstNonce">The nonce of the first sponge's stream.</param>
    /// <param name="count">
    /// The number of streams wanted, from 1 to 4; the sponges past them repeat the last one's input.
    /// </param>
    private static void AbsorbNoncedSeeds(ref KeccakSponge4 sponge, ReadOnlySpan<byte> seed, int firstNonce, int count)
    {
        Span<byte> inputs = stackalloc byte[KeccakSponge4.Ways * NoncedSeedInputLength];
        for (int lane = 0; lane < KeccakSponge4.Ways; lane++)
        {
            int nonce = firstNonce + Math.Min(lane, count - 1);
            Span<byte> input = inputs.Slice(lane * NoncedSeedInputLength, NoncedSeedInputLength);
            seed.CopyTo(input);
            input[64] = (byte)nonce;
            input[65] = (byte)(nonce >> 8);
        }

        sponge.Absorb(
            inputs[..NoncedSeedInputLength],
            inputs.Slice(NoncedSeedInputLength, NoncedSeedInputLength),
            inputs.Slice(2 * NoncedSeedInputLength, NoncedSeedInputLength),
            inputs.Slice(3 * NoncedSeedInputLength, NoncedSeedInputLength));

        CryptographyHelper.Clear(inputs);
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
