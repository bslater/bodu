// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the Argon2 memory-hard function defined by RFC 9106 — the pre-hashing digest, the slicewise memory fill
/// with per-variant reference indexing, the compression function <c>G</c>, and the finalization — shared by the
/// <see cref="Argon2d" />, <see cref="Argon2i" />, and <see cref="Argon2id" /> public types.
/// </summary>
/// <remarks>
/// <para>
/// The fill is generic over an <see cref="IArgon2Kernel" />, the compression function for one instruction set, so each
/// kernel runs in a loop the JIT specializes for it. Every kernel carries the previous block across a segment, so a
/// block reads only its reference block and, on passes that XOR, its own previous contents.
/// </para>
/// <para>
/// Every buffer that holds a password-derived word — the matrix, H0, the per-segment scratch, and the buffers used to
/// build the first and last blocks — is cleared before it is released. Values the JIT keeps in registers or spills to
/// its own stack slots are beyond the library's reach.
/// </para>
/// </remarks>
internal static partial class Argon2Core
{
    /// <summary>The number of 64-bit words in a 1024-byte memory block.</summary>
    private const int WordsPerBlock = Argon2Matrix.WordsPerBlock;

    /// <summary>The number of bytes in a memory block.</summary>
    private const int BlockSizeBytes = WordsPerBlock * sizeof(ulong);

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
        var geometry = new Geometry(type, parameters);

        if ((long)geometry.MemoryBlocks * WordsPerBlock > Array.MaxLength)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KdfMemoryExceedsLimit);

        using Argon2Matrix matrix = Argon2Matrix.Rent(geometry.MemoryBlocks);
        Span<byte> h0 = stackalloc byte[Argon2Blake2b.MaxDigestBytes];

        try
        {
            ComputeH0(type, parameters, password, salt, h0);
            InitializeBlocks(matrix, h0, geometry);
            FillMemory<ScalarKernel>(matrix, geometry);
            Finalize(matrix, geometry, tag);
        }
        finally
        {
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
    /// <remarks>
    /// The fields are hashed as they are appended, so the password is never copied into a buffer of its own.
    /// </remarks>
    private static void ComputeH0(
        Argon2Type type,
        Argon2Parameters p,
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        Span<byte> h0)
    {
        ReadOnlySpan<byte> secret = p.Secret ?? ReadOnlySpan<byte>.Empty;
        ReadOnlySpan<byte> associatedData = p.AssociatedData ?? ReadOnlySpan<byte>.Empty;

        Span<ulong> state = stackalloc ulong[Argon2Blake2b.StateWords];
        Span<byte> block = stackalloc byte[Argon2Blake2b.BlockSizeBytes];

        var hasher = new Argon2Blake2b.Hasher(state, block, Argon2Blake2b.MaxDigestBytes);
        hasher.AppendLittleEndian(p.Parallelism);
        hasher.AppendLittleEndian(p.TagLength);
        hasher.AppendLittleEndian(p.MemoryKiB);
        hasher.AppendLittleEndian(p.Iterations);
        hasher.AppendLittleEndian(p.Version);
        hasher.AppendLittleEndian((int)type);
        AppendLengthPrefixed(ref hasher, password);
        AppendLengthPrefixed(ref hasher, salt);
        AppendLengthPrefixed(ref hasher, secret);
        AppendLengthPrefixed(ref hasher, associatedData);
        hasher.Finish(h0);
    }

    /// <summary>
    /// Appends a 32-bit little-endian length prefix followed by the bytes, as RFC 9106 encodes H0's variable fields.
    /// </summary>
    /// <param name="hasher">The hasher computing H0.</param>
    /// <param name="value">The bytes to append after their length.</param>
    private static void AppendLengthPrefixed(ref Argon2Blake2b.Hasher hasher, ReadOnlySpan<byte> value)
    {
        hasher.AppendLittleEndian(value.Length);
        hasher.Append(value);
    }

    /// <summary>
    /// Fills the first two columns of every lane from <c>H0</c> via the variable-length hash <c>H'</c> (RFC 9106,
    /// Figures 3 and 4).
    /// </summary>
    /// <param name="matrix">The memory matrix.</param>
    /// <param name="h0">The 64-byte pre-hashing digest <c>H0</c>.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    private static void InitializeBlocks(Argon2Matrix matrix, ReadOnlySpan<byte> h0, in Geometry geometry)
    {
        Span<byte> input = stackalloc byte[Argon2Blake2b.MaxDigestBytes + 8];
        Span<byte> block = stackalloc byte[BlockSizeBytes];

        try
        {
            h0.CopyTo(input);

            for (int lane = 0; lane < geometry.Lanes; lane++)
            {
                for (int column = 0; column < 2; column++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(input.Slice(Argon2Blake2b.MaxDigestBytes, 4), column);
                    BinaryPrimitives.WriteInt32LittleEndian(input.Slice(Argon2Blake2b.MaxDigestBytes + 4, 4), lane);

                    Argon2Blake2b.HashVariableLength(input, block);
                    LoadBlockLE(block, matrix.BlockSpan((lane * geometry.LaneLength) + column));
                }
            }
        }
        finally
        {
            CryptographyHelper.Clear(input);
            CryptographyHelper.Clear(block);
        }
    }

    /// <summary>
    /// Computes the final block as the XOR of the last column across lanes, then emits the tag via <c>H'</c> (RFC 9106,
    /// Figure 7).
    /// </summary>
    /// <param name="matrix">The memory matrix.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    /// <param name="tag">The destination span that receives the final tag.</param>
    private static void Finalize(Argon2Matrix matrix, in Geometry geometry, Span<byte> tag)
    {
        Span<ulong> c = stackalloc ulong[WordsPerBlock];
        Span<byte> cBytes = stackalloc byte[BlockSizeBytes];

        try
        {
            matrix.BlockSpan(geometry.LaneLength - 1).CopyTo(c);

            for (int lane = 1; lane < geometry.Lanes; lane++)
            {
                ReadOnlySpan<ulong> last = matrix.BlockSpan((lane * geometry.LaneLength) + geometry.LaneLength - 1);
                for (int k = 0; k < WordsPerBlock; k++)
                    c[k] ^= last[k];
            }

            StoreBlockLE(c, cBytes);
            Argon2Blake2b.HashVariableLength(cBytes, tag);
        }
        finally
        {
            CryptographyHelper.Clear(c);
            CryptographyHelper.Clear(cBytes);
        }
    }

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
}
