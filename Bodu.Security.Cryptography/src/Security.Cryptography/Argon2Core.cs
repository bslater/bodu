// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the Argon2 memory-hard function defined by RFC 9106 — the pre-hashing digest, the slicewise memory fill
/// with per-variant reference indexing, the compression function <c>G</c>, and the finalization — shared by the
/// <see cref="Argon2d" />, <see cref="Argon2i" />, and <see cref="Argon2id" /> public types.
/// </summary>
internal static class Argon2Core
{
    /// <summary>The number of 64-bit words in a 1024-byte memory block.</summary>
    private const int WordsPerBlock = 128;

    /// <summary>The number of vertical slices (synchronization points) each lane is divided into.</summary>
    private const int SyncPoints = 4;

    /// <summary>The number of (J1, J2) address pairs produced by a single Argon2i address block.</summary>
    private const int AddressesPerBlock = WordsPerBlock;

    /// <summary>
    /// Derives an Argon2 tag into <paramref name="tag" /> from the supplied inputs and parameters.
    /// </summary>
    /// <param name="type">The Argon2 variant selecting the reference-indexing strategy.</param>
    /// <param name="parameters">The validated cost and auxiliary parameters.</param>
    /// <param name="password">The password / message <c>P</c>.</param>
    /// <param name="salt">The salt / nonce <c>S</c>.</param>
    /// <param name="tag">The destination buffer; its length must equal <c>parameters.TagLength</c>.</param>
    /// <exception cref="CryptographicException">
    /// The requested memory size cannot be represented as a single managed array.
    /// </exception>
    internal static void DeriveTag(
        Argon2Type type,
        Argon2Parameters parameters,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> tag)
    {
        int lanes = parameters.Parallelism;

        // m' is m rounded down to the nearest multiple of 4*p (RFC 9106 Section 3.2).
        int memoryBlocks = SyncPoints * lanes * (parameters.MemoryKiB / (SyncPoints * lanes));
        int laneLength = memoryBlocks / lanes;           // q columns per lane
        int segmentLength = laneLength / SyncPoints;

        if ((long)memoryBlocks * WordsPerBlock > Array.MaxLength)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KdfMemoryExceedsLimit);

        ulong[] memory = new ulong[memoryBlocks * WordsPerBlock];
        Span<byte> h0 = stackalloc byte[Argon2Blake2b.MaxDigestBytes];

        try
        {
            ComputeH0(type, parameters, password, salt, h0);
            InitializeBlocks(memory, h0, lanes, laneLength);

            for (int pass = 0; pass < parameters.Iterations; pass++)
                for (int slice = 0; slice < SyncPoints; slice++)
                    for (int lane = 0; lane < lanes; lane++)
                        FillSegment(type, parameters, memory, pass, slice, lane, lanes, laneLength, segmentLength);

            Finalize(memory, lanes, laneLength, tag);
        }
        finally
        {
            CryptographyHelper.Clear(memory);
            CryptographyHelper.Clear(h0);
        }
    }

    /// <summary>
    /// Computes the 64-byte pre-hashing digest <c>H0</c> (RFC 9106, Figure 1).
    /// </summary>
    /// <param name="type">The Argon2 variant being computed.</param>
    /// <param name="p">The Argon2 parameters describing cost and auxiliary inputs.</param>
    /// <param name="password">The password bytes.</param>
    /// <param name="salt">The salt bytes.</param>
    /// <param name="h0">The destination span that receives the 64-byte <c>H0</c> digest.</param>
    private static void ComputeH0(
        Argon2Type type,
        Argon2Parameters p,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> h0)
    {
        ReadOnlySpan<byte> secret = p.Secret ?? ReadOnlySpan<byte>.Empty;
        ReadOnlySpan<byte> associatedData = p.AssociatedData ?? ReadOnlySpan<byte>.Empty;

        int length =
            (6 * 4) +
            (4 + password.Length) +
            (4 + salt.Length) +
            (4 + secret.Length) +
            (4 + associatedData.Length);

        byte[] buffer = new byte[length];
        Span<byte> w = buffer;
        int o = 0;

        WriteLE32(w, ref o, p.Parallelism);
        WriteLE32(w, ref o, p.TagLength);
        WriteLE32(w, ref o, p.MemoryKiB);
        WriteLE32(w, ref o, p.Iterations);
        WriteLE32(w, ref o, p.Version);
        WriteLE32(w, ref o, (int)type);
        WriteLengthPrefixed(w, ref o, password);
        WriteLengthPrefixed(w, ref o, salt);
        WriteLengthPrefixed(w, ref o, secret);
        WriteLengthPrefixed(w, ref o, associatedData);

        Argon2Blake2b.Hash(buffer, h0);
        CryptographyHelper.Clear(buffer);
    }

    /// <summary>
    /// Fills the first two columns of every lane from <c>H0</c> via the variable-length hash <c>H'</c> (RFC 9106,
    /// Figures 3 and 4).
    /// </summary>
    /// <param name="memory">The full memory matrix, as a flat array of 64-bit words.</param>
    /// <param name="h0">The 64-byte pre-hashing digest <c>H0</c>.</param>
    /// <param name="lanes">The number of parallel lanes.</param>
    /// <param name="laneLength">The number of blocks in each lane.</param>
    private static void InitializeBlocks(ulong[] memory, ReadOnlySpan<byte> h0, int lanes, int laneLength)
    {
        Span<byte> input = stackalloc byte[Argon2Blake2b.MaxDigestBytes + 8];
        h0.CopyTo(input);

        Span<byte> block = stackalloc byte[WordsPerBlock * 8];

        for (int lane = 0; lane < lanes; lane++)
        {
            for (int column = 0; column < 2; column++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(input.Slice(Argon2Blake2b.MaxDigestBytes, 4), column);
                BinaryPrimitives.WriteInt32LittleEndian(input.Slice(Argon2Blake2b.MaxDigestBytes + 4, 4), lane);

                Argon2Blake2b.HashVariableLength(input, block);
                LoadBlockLE(block, BlockSpan(memory, lane * laneLength + column));
            }
        }
    }

    /// <summary>
    /// Computes one segment (the intersection of a lane and a slice) of the memory matrix.
    /// </summary>
    /// <param name="type">The Argon2 variant, which selects data-dependent or data-independent indexing.</param>
    /// <param name="parameters">The Argon2 parameters describing cost and version.</param>
    /// <param name="memory">The full memory matrix, as a flat array of 64-bit words.</param>
    /// <param name="pass">The zero-based pass (iteration) index.</param>
    /// <param name="slice">The zero-based slice index within the pass.</param>
    /// <param name="lane">The zero-based lane index being filled.</param>
    /// <param name="lanes">The total number of lanes.</param>
    /// <param name="laneLength">The number of blocks in each lane.</param>
    /// <param name="segmentLength">The number of blocks in each segment.</param>
    private static void FillSegment(
        Argon2Type type,
        Argon2Parameters parameters,
        ulong[] memory,
        int pass,
        int slice,
        int lane,
        int lanes,
        int laneLength,
        int segmentLength)
    {
        bool dataIndependent = type == Argon2Type.Argon2i
            || (type == Argon2Type.Argon2id && pass == 0 && slice < SyncPoints / 2);

        Span<ulong> addressBlock = stackalloc ulong[WordsPerBlock];
        Span<ulong> inputBlock = stackalloc ulong[WordsPerBlock];
        Span<ulong> zeroBlock = stackalloc ulong[WordsPerBlock];

        if (dataIndependent)
        {
            inputBlock.Clear();
            inputBlock[0] = (ulong)pass;
            inputBlock[1] = (ulong)lane;
            inputBlock[2] = (ulong)slice;
            inputBlock[3] = (ulong)(laneLength * lanes);   // m'
            inputBlock[4] = (ulong)parameters.Iterations;
            inputBlock[5] = (ulong)(int)type;
        }

        int startIndex = 0;
        if (pass == 0 && slice == 0)
        {
            startIndex = 2;   // the first two blocks are already filled
            if (dataIndependent)
                NextAddresses(addressBlock, inputBlock, zeroBlock);
        }

        for (int i = startIndex; i < segmentLength; i++)
        {
            int column = slice * segmentLength + i;
            int prevColumn = column == 0 ? laneLength - 1 : column - 1;

            ulong pseudoRand;
            if (dataIndependent)
            {
                if (i % AddressesPerBlock == 0)
                    NextAddresses(addressBlock, inputBlock, zeroBlock);
                pseudoRand = addressBlock[i % AddressesPerBlock];
            }
            else
            {
                pseudoRand = BlockSpan(memory, lane * laneLength + prevColumn)[0];
            }

            uint refLane = (pass == 0 && slice == 0)
                ? (uint)lane
                : (uint)((pseudoRand >> 32) % (ulong)lanes);

            int refIndex = ReferenceIndex(
                pass, slice, i, segmentLength, laneLength,
                (uint)(pseudoRand & 0xFFFFFFFF), refLane == (uint)lane);

            ReadOnlySpan<ulong> prevBlock = BlockSpan(memory, lane * laneLength + prevColumn);
            ReadOnlySpan<ulong> refBlock = BlockSpan(memory, (int)refLane * laneLength + refIndex);
            Span<ulong> curBlock = BlockSpan(memory, lane * laneLength + column);

            // Version 0x10 always overwrites; version 0x13 XORs on subsequent passes.
            bool withXor = pass != 0 && parameters.Version != Argon2Parameters.Version10;
            FillBlock(prevBlock, refBlock, curBlock, withXor);
        }
    }

    /// <summary>
    /// Regenerates the Argon2i address block: <c>address = G(ZERO, G(ZERO, input))</c> (RFC 9106, Section 3.4.1.2).
    /// </summary>
    /// <param name="addressBlock">The block that receives the regenerated pseudo-random addresses.</param>
    /// <param name="inputBlock">The counter input block; its counter word is incremented in place.</param>
    /// <param name="zeroBlock">A block of zero words used as the first compression operand.</param>
    private static void NextAddresses(Span<ulong> addressBlock, Span<ulong> inputBlock, ReadOnlySpan<ulong> zeroBlock)
    {
        inputBlock[6]++;
        FillBlock(zeroBlock, inputBlock, addressBlock, withXor: false);
        FillBlock(zeroBlock, addressBlock, addressBlock, withXor: false);
    }

    /// <summary>
    /// Maps the pseudo-random value to a reference-block column within the same lane (RFC 9106, Section 3.4.2).
    /// </summary>
    /// <param name="pass">The zero-based pass index.</param>
    /// <param name="slice">The zero-based slice index.</param>
    /// <param name="index">The zero-based position within the current segment.</param>
    /// <param name="segmentLength">The number of blocks in each segment.</param>
    /// <param name="laneLength">The number of blocks in each lane.</param>
    /// <param name="j1">The low 32 bits of the pseudo-random value driving the mapping.</param>
    /// <param name="sameLane"><see langword="true" /> when the reference block is taken from the current lane.</param>
    /// <returns>The column index of the reference block within its lane.</returns>
    private static int ReferenceIndex(
        int pass, int slice, int index, int segmentLength, int laneLength, uint j1, bool sameLane)
    {
        long referenceAreaSize;
        if (pass == 0)
        {
            if (slice == 0)
                referenceAreaSize = index - 1;
            else
                referenceAreaSize = sameLane
                    ? (slice * segmentLength) + index - 1
                    : (slice * segmentLength) + (index == 0 ? -1 : 0);
        }
        else
        {
            referenceAreaSize = sameLane
                ? laneLength - segmentLength + index - 1
                : laneLength - segmentLength + (index == 0 ? -1 : 0);
        }

        // Nonuniform mapping: zz = areaSize - 1 - (areaSize * (j1^2 >> 32) >> 32).
        ulong relative = j1;
        relative = (relative * relative) >> 32;
        relative = (ulong)referenceAreaSize - 1 - (((ulong)referenceAreaSize * relative) >> 32);

        int startPosition = 0;
        if (pass != 0)
            startPosition = slice == SyncPoints - 1 ? 0 : (slice + 1) * segmentLength;

        return (int)(((ulong)startPosition + relative) % (ulong)laneLength);
    }

    /// <summary>
    /// Computes the final block as the XOR of the last column across lanes, then emits the tag via <c>H'</c> (RFC 9106,
    /// Figure 7).
    /// </summary>
    /// <param name="memory">The full memory matrix, as a flat array of 64-bit words.</param>
    /// <param name="lanes">The number of lanes.</param>
    /// <param name="laneLength">The number of blocks in each lane.</param>
    /// <param name="tag">The destination span that receives the final tag.</param>
    private static void Finalize(ulong[] memory, int lanes, int laneLength, Span<byte> tag)
    {
        Span<ulong> c = stackalloc ulong[WordsPerBlock];
        BlockSpan(memory, laneLength - 1).CopyTo(c);

        for (int lane = 1; lane < lanes; lane++)
        {
            ReadOnlySpan<ulong> last = BlockSpan(memory, lane * laneLength + laneLength - 1);
            for (int k = 0; k < WordsPerBlock; k++)
                c[k] ^= last[k];
        }

        Span<byte> cBytes = stackalloc byte[WordsPerBlock * 8];
        StoreBlockLE(c, cBytes);
        Argon2Blake2b.HashVariableLength(cBytes, tag);
    }

    /// <summary>
    /// The Argon2 compression function <c>G</c> (RFC 9106, Section 3.5).
    /// </summary>
    /// <param name="prev">The previous block <c>X</c>.</param>
    /// <param name="refBlock">The reference block <c>Y</c>.</param>
    /// <param name="next">The destination block.</param>
    /// <param name="withXor">
    /// <see langword="true" /> to XOR the permutation result with the existing destination block (used on passes after
    /// the first for version 0x13).
    /// </param>
    private static void FillBlock(ReadOnlySpan<ulong> prev, ReadOnlySpan<ulong> refBlock, Span<ulong> next, bool withXor)
    {
        Span<ulong> r = stackalloc ulong[WordsPerBlock];
        Span<ulong> tmp = stackalloc ulong[WordsPerBlock];

        for (int k = 0; k < WordsPerBlock; k++)
            r[k] = prev[k] ^ refBlock[k];

        r.CopyTo(tmp);
        if (withXor)
            for (int k = 0; k < WordsPerBlock; k++)
                tmp[k] ^= next[k];

        Permute(r);

        for (int k = 0; k < WordsPerBlock; k++)
            next[k] = tmp[k] ^ r[k];
    }

    /// <summary>
    /// Applies the BLAKE2b-based permutation <c>P</c> rowwise then columnwise to a 1024-byte block (RFC 9106, Figures
    /// 15 and 18).
    /// </summary>
    /// <param name="block">The 128-word (1024-byte) block permuted in place.</param>
    private static void Permute(Span<ulong> block)
    {
        for (int row = 0; row < 8; row++)
            Round(block.Slice(row * 16, 16));

        Span<ulong> column = stackalloc ulong[16];
        for (int col = 0; col < 8; col++)
        {
            int b = col * 2;
            column[0] = block[b]; column[1] = block[b + 1];
            column[2] = block[b + 16]; column[3] = block[b + 17];
            column[4] = block[b + 32]; column[5] = block[b + 33];
            column[6] = block[b + 48]; column[7] = block[b + 49];
            column[8] = block[b + 64]; column[9] = block[b + 65];
            column[10] = block[b + 80]; column[11] = block[b + 81];
            column[12] = block[b + 96]; column[13] = block[b + 97];
            column[14] = block[b + 112]; column[15] = block[b + 113];

            Round(column);

            block[b] = column[0]; block[b + 1] = column[1];
            block[b + 16] = column[2]; block[b + 17] = column[3];
            block[b + 32] = column[4]; block[b + 33] = column[5];
            block[b + 48] = column[6]; block[b + 49] = column[7];
            block[b + 64] = column[8]; block[b + 65] = column[9];
            block[b + 80] = column[10]; block[b + 81] = column[11];
            block[b + 96] = column[12]; block[b + 97] = column[13];
            block[b + 112] = column[14]; block[b + 113] = column[15];
        }
    }

    /// <summary>
    /// Applies one BLAKE2b round (eight <c>GB</c> calls) to sixteen 64-bit words.
    /// </summary>
    /// <param name="v">The sixteen 64-bit words mixed in place.</param>
    private static void Round(Span<ulong> v)
    {
        GB(ref v[0], ref v[4], ref v[8], ref v[12]);
        GB(ref v[1], ref v[5], ref v[9], ref v[13]);
        GB(ref v[2], ref v[6], ref v[10], ref v[14]);
        GB(ref v[3], ref v[7], ref v[11], ref v[15]);

        GB(ref v[0], ref v[5], ref v[10], ref v[15]);
        GB(ref v[1], ref v[6], ref v[11], ref v[12]);
        GB(ref v[2], ref v[7], ref v[8], ref v[13]);
        GB(ref v[3], ref v[4], ref v[9], ref v[14]);
    }

    /// <summary>
    /// The Argon2 mixing function <c>GB</c> — the BLAKE2b round function augmented with 64-bit multiplications (RFC
    /// 9106, Section 3.6).
    /// </summary>
    /// <param name="a">The first state word, combined and updated in place.</param>
    /// <param name="b">The second state word, combined and updated in place.</param>
    /// <param name="c">The third state word, combined and updated in place.</param>
    /// <param name="d">The fourth state word, combined and updated in place.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / rotate / XOR steps mirror the GB definition and preserve a compact, specification-like layout.")]
    private static void GB(ref ulong a, ref ulong b, ref ulong c, ref ulong d)
    {
        a = FBlaMka(a, b); d = BitOperations.RotateRight(d ^ a, 32);
        c = FBlaMka(c, d); b = BitOperations.RotateRight(b ^ c, 24);
        a = FBlaMka(a, b); d = BitOperations.RotateRight(d ^ a, 16);
        c = FBlaMka(c, d); b = BitOperations.RotateRight(b ^ c, 63);
    }

    /// <summary>
    /// Computes <c>a + b + 2 * trunc(a) * trunc(b)</c> modulo 2^64, where <c>trunc(x)</c> is the low 32 bits of x.
    /// </summary>
    /// <param name="x">The first operand.</param>
    /// <param name="y">The second operand.</param>
    /// <returns>The value <c>x + y + 2·trunc(x)·trunc(y)</c> modulo 2^64.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong FBlaMka(ulong x, ulong y)
    {
        ulong xy = (ulong)(uint)x * (uint)y;
        return x + y + (2 * xy);
    }

    /// <summary>
    /// Returns the 128-word span of the block at the specified absolute block index.
    /// </summary>
    /// <param name="memory">The full memory matrix, as a flat array of 64-bit words.</param>
    /// <param name="blockIndex">The absolute, zero-based block index.</param>
    /// <returns>The 128-word span backing the requested block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Span<ulong> BlockSpan(ulong[] memory, int blockIndex) =>
        memory.AsSpan(blockIndex * WordsPerBlock, WordsPerBlock);

    /// <summary>
    /// Loads a 1024-byte block into 128 little-endian 64-bit words.
    /// </summary>
    /// <param name="source">The 1024-byte source buffer.</param>
    /// <param name="block">The destination span of 128 64-bit words.</param>
    private static void LoadBlockLE(ReadOnlySpan<byte> source, Span<ulong> block)
    {
        for (int i = 0; i < WordsPerBlock; i++)
            block[i] = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(i * 8, 8));
    }

    /// <summary>
    /// Serializes 128 64-bit words into a 1024-byte little-endian block.
    /// </summary>
    /// <param name="block">The source span of 128 64-bit words.</param>
    /// <param name="destination">The 1024-byte destination buffer.</param>
    private static void StoreBlockLE(ReadOnlySpan<ulong> block, Span<byte> destination)
    {
        for (int i = 0; i < WordsPerBlock; i++)
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(i * 8, 8), block[i]);
    }

    /// <summary>
    /// Writes a 32-bit value in little-endian order and advances the offset.
    /// </summary>
    /// <param name="destination">The buffer that receives the encoded value.</param>
    /// <param name="offset">The write offset, advanced by four bytes on return.</param>
    /// <param name="value">The 32-bit value to write.</param>
    private static void WriteLE32(Span<byte> destination, ref int offset, int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset, 4), value);
        offset += 4;
    }

    /// <summary>
    /// Writes a 32-bit length prefix followed by the bytes, advancing the offset.
    /// </summary>
    /// <param name="destination">The buffer that receives the length prefix and bytes.</param>
    /// <param name="offset">The write offset, advanced past the written data on return.</param>
    /// <param name="value">The bytes to write after their 32-bit length prefix.</param>
    private static void WriteLengthPrefixed(Span<byte> destination, ref int offset, ReadOnlySpan<byte> value)
    {
        WriteLE32(destination, ref offset, value.Length);
        value.CopyTo(destination.Slice(offset));
        offset += value.Length;
    }
}
