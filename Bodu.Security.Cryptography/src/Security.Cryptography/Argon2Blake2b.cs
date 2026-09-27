// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Blake2b.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a self-contained, unkeyed <c>BLAKE2b</c> primitive supporting arbitrary digest lengths from 1 to 64 bytes,
/// together with the Argon2 variable-length hash function <c>H'</c> built on top of it.
/// </summary>
/// <remarks>
/// <para>
/// Argon2 (RFC 9106) is defined as a mode of operation over BLAKE2b ([BLAKE2], RFC 7693) used both directly as
/// <c>H^x</c> (for the pre-hashing digest) and through the variable-length hash <c>H'</c> (for the initial memory
/// blocks and the final tag). The latter requires BLAKE2b outputs of any length in the range 1–64 bytes, whereas the
/// public <see cref="Blake2b" /> type intentionally restricts its output to a curated set of sizes. Rather than alter
/// that well-tested type, Argon2 bundles its own minimal BLAKE2b here — the standard approach taken by reference Argon2
/// implementations. Only the unkeyed digest is needed; Argon2 never uses BLAKE2b's keyed (MAC) mode.
/// </para>
/// <para>
/// The digest is computed incrementally through <see cref="Hasher" />, so a caller can hash several inputs — the
/// pre-hashing digest's length-prefixed password, salt, secret, and associated data — without first copying them into
/// one buffer. Every buffer that holds input or state is cleared before it is released.
/// </para>
/// </remarks>
internal static partial class Argon2Blake2b
{
    /// <summary>The BLAKE2b block size, in bytes.</summary>
    internal const int BlockSizeBytes = 128;

    /// <summary>The maximum BLAKE2b digest length, in bytes.</summary>
    internal const int MaxDigestBytes = 64;

    /// <summary>The number of 64-bit words in the BLAKE2b chaining state.</summary>
    internal const int StateWords = 8;

    /// <summary>The number of 64-bit message words in one BLAKE2b block.</summary>
    private const int MessageWords = 16;

    /// <summary>The BLAKE2b initialization vector (the SHA-512 IV).</summary>
    private static readonly ulong[] s_iv =
    [
        0x6A09E667F3BCC908UL, 0xBB67AE8584CAA73BUL,
        0x3C6EF372FE94F82BUL, 0xA54FF53A5F1D36F1UL,
        0x510E527FADE682D1UL, 0x9B05688C2B3E6C1FUL,
        0x1F83D9ABFB41BD6BUL, 0x5BE0CD19137E2179UL,
    ];

    /// <summary>The BLAKE2b message-word permutation schedule (twelve rounds of sixteen indices each).</summary>
    private static readonly byte[][] s_sigma =
    [
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
        [14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3],
        [11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4],
        [7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8],
        [9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13],
        [2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9],
        [12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11],
        [13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10],
        [6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5],
        [10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0],
        [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15],
        [14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3],
    ];

    /// <summary>
    /// Computes the unkeyed BLAKE2b digest of <paramref name="input" /> with an arbitrary output length and writes it
    /// to <paramref name="output" />.
    /// </summary>
    /// <param name="input">The message to hash.</param>
    /// <param name="output">
    /// The destination buffer; its length determines the digest size (1–64 bytes). It may overlap
    /// <paramref name="input" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>output.Length</c> is not between 1 and 64 inclusive.
    /// </exception>
    internal static void Hash(ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (output.Length is < 1 or > MaxDigestBytes)
            throw new ArgumentOutOfRangeException(nameof(output));

        Span<ulong> state = stackalloc ulong[StateWords];
        Span<byte> block = stackalloc byte[BlockSizeBytes];

        var hasher = new Hasher(state, block, output.Length);
        hasher.Append(input);
        hasher.Finish(output);
    }

    /// <summary>
    /// Computes the Argon2 variable-length hash <c>H'</c> of <paramref name="input" /> into <paramref name="output" />,
    /// as defined in RFC 9106, Section 3.3.
    /// </summary>
    /// <param name="input">The message to hash (the <c>A</c> argument of <c>H'</c>).</param>
    /// <param name="output">The destination buffer; its length is the requested output length <c>T</c>.</param>
    /// <remarks>
    /// For <c>T &lt;= 64</c> the result is <c>H^T(LE32(T) || A)</c>. For larger <c>T</c> the output is assembled from a
    /// chain of 64-byte BLAKE2b digests, taking the first 32 bytes of each except the final (shortened) block.
    /// </remarks>
    internal static void HashVariableLength(ReadOnlySpan<byte> input, Span<byte> output)
    {
        int outLen = output.Length;

        Span<ulong> state = stackalloc ulong[StateWords];
        Span<byte> block = stackalloc byte[BlockSizeBytes];

        if (outLen <= MaxDigestBytes)
        {
            var hasher = new Hasher(state, block, outLen);
            hasher.AppendLittleEndian(outLen);
            hasher.Append(input);
            hasher.Finish(output);
            return;
        }

        // r = ceil(T / 32) - 2 full 64-byte digests contribute their first 32 bytes; the final
        // (r+1)-th digest is sized to the remaining bytes (RFC 9106, Figure 8).
        int r = ((outLen + 31) / 32) - 2;

        Span<byte> v = stackalloc byte[MaxDigestBytes];

        var first = new Hasher(state, block, MaxDigestBytes);
        first.AppendLittleEndian(outLen);
        first.Append(input);
        first.Finish(v);                   // V_1

        v[..32].CopyTo(output[..32]);      // W_1

        for (int i = 2; i <= r; i++)
        {
            Hash(v, v);                    // V_i
            v[..32].CopyTo(output.Slice((i - 1) * 32, 32));   // W_i
        }

        // V_{r+1} = H^(T - 32*r)(V_r), sized to the remaining output bytes.
        Hash(v, output[(r * 32)..]);
        CryptographyHelper.Clear(v);
    }

    /// <summary>
    /// Applies the BLAKE2b compression function to the state.
    /// </summary>
    /// <param name="h">The eight-word chaining state, updated in place.</param>
    /// <param name="block">The 128-byte message block.</param>
    /// <param name="counter">
    /// The total number of input bytes processed so far (low 64 bits; high word is zero here).
    /// </param>
    /// <param name="last"><see langword="true" /> when this is the final block, applying the finalization flag.</param>
    private static void Compress(Span<ulong> h, ReadOnlySpan<byte> block, ulong counter, bool last)
    {
        Span<ulong> m = stackalloc ulong[MessageWords];
        Span<ulong> v = stackalloc ulong[MessageWords];

        for (int i = 0; i < MessageWords; i++)
            m[i] = BinaryPrimitives.ReadUInt64LittleEndian(block.Slice(i * 8, 8));

        h.CopyTo(v);
        s_iv.CopyTo(v[8..]);

        v[12] ^= counter;     // low 64 bits of the byte counter

        // v[13] ^= 0;        // high 64 bits — always zero for Argon2's inputs
        if (last)
            v[14] ^= 0xFFFF_FFFF_FFFF_FFFFUL;

        for (int round = 0; round < 12; round++)
        {
            byte[] s = s_sigma[round];

            Mix(v, 0, 4, 8, 12, m[s[0]], m[s[1]]);
            Mix(v, 1, 5, 9, 13, m[s[2]], m[s[3]]);
            Mix(v, 2, 6, 10, 14, m[s[4]], m[s[5]]);
            Mix(v, 3, 7, 11, 15, m[s[6]], m[s[7]]);

            Mix(v, 0, 5, 10, 15, m[s[8]], m[s[9]]);
            Mix(v, 1, 6, 11, 12, m[s[10]], m[s[11]]);
            Mix(v, 2, 7, 8, 13, m[s[12]], m[s[13]]);
            Mix(v, 3, 4, 9, 14, m[s[14]], m[s[15]]);
        }

        for (int i = 0; i < StateWords; i++)
            h[i] ^= v[i] ^ v[i + 8];

        // The message words hold raw input — the password, while H0 is computed — and the working vector is derived
        // from it, so neither outlives the call.
        CryptographyHelper.Clear(m);
        CryptographyHelper.Clear(v);
    }

    /// <summary>
    /// The BLAKE2b mixing function <c>G</c> applied to four working-vector words.
    /// </summary>
    /// <param name="v">The sixteen-word working vector.</param>
    /// <param name="a">The index of the first word.</param>
    /// <param name="b">The index of the second word.</param>
    /// <param name="c">The index of the third word.</param>
    /// <param name="d">The index of the fourth word.</param>
    /// <param name="x">The first message word.</param>
    /// <param name="y">The second message word.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Mix(Span<ulong> v, int a, int b, int c, int d, ulong x, ulong y)
    {
        v[a] = v[a] + v[b] + x;
        v[d] = BitOperations.RotateRight(v[d] ^ v[a], 32);
        v[c] = v[c] + v[d];
        v[b] = BitOperations.RotateRight(v[b] ^ v[c], 24);
        v[a] = v[a] + v[b] + y;
        v[d] = BitOperations.RotateRight(v[d] ^ v[a], 16);
        v[c] = v[c] + v[d];
        v[b] = BitOperations.RotateRight(v[b] ^ v[c], 63);
    }
}
