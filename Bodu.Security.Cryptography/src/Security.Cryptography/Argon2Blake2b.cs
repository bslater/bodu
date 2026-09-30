// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Blake2b.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
/// blocks and the final tag). The latter requires BLAKE2b outputs of any length in the range 1-64 bytes, whereas the
/// public <see cref="Blake2b" /> type intentionally restricts its output to a curated set of sizes. Argon2 therefore
/// drives the BLAKE2b compression function, <see cref="Blake2bCore" />, directly - the kernels <see cref="Blake2b" />
/// runs on too. Only the unkeyed digest is needed; Argon2 never uses BLAKE2b's keyed (MAC) mode.
/// </para>
/// <para>
/// The digest is computed incrementally through <see cref="Hasher" />, so a caller can hash several inputs - the
/// pre-hashing digest's length-prefixed password, salt, secret, and associated data - without first copying them into
/// one buffer. Every buffer that holds input or state is cleared before it is released.
/// </para>
/// </remarks>
[SkipLocalsInit]
internal static partial class Argon2Blake2b
{
    /// <summary>The BLAKE2b block size, in bytes.</summary>
    internal const int BlockSizeBytes = Blake2bCore.BlockBytes;

    /// <summary>The maximum BLAKE2b digest length, in bytes.</summary>
    internal const int MaxDigestBytes = 64;

    /// <summary>The number of 64-bit words in the BLAKE2b chaining state.</summary>
    internal const int StateWords = Blake2bCore.StateWords;

    /// <summary>
    /// Computes the unkeyed BLAKE2b digest of <paramref name="input" /> with an arbitrary output length and writes it
    /// to <paramref name="output" />.
    /// </summary>
    /// <param name="input">The message to hash.</param>
    /// <param name="output">
    /// The destination buffer; its length determines the digest size (1-64 bytes). It may overlap
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
}
