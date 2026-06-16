// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScryptCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the scrypt sequential memory-hard function defined by RFC 7914 — the PBKDF2-HMAC-SHA256 envelope, the
/// <c>scryptROMix</c> and <c>scryptBlockMix</c> mixing functions, and the Salsa20/8 core.
/// </summary>
internal static class ScryptCore
{
    /// <summary>
    /// The number of 32-bit words in a Salsa20 block (64 bytes).
    /// </summary>
    private const int BlockWords = 16;

    /// <summary>
    /// Derives <paramref name="output" />.Length bytes from the password and salt using scrypt with the given cost
    /// parameters.
    /// </summary>
    /// <param name="password">The password <c>P</c>.</param>
    /// <param name="salt">The salt <c>S</c>.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c> (a power of two greater than one).</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <param name="parallelization">The parallelization parameter <c>p</c>.</param>
    /// <param name="output">The destination buffer; its length is the derived-key length <c>dkLen</c>.</param>
    /// <exception cref="CryptographicException">
    /// The requested memory size cannot be represented as a single managed array.
    /// </exception>
    internal static void DeriveKey(
        ReadOnlySpan<byte> password,
        ReadOnlySpan<byte> salt,
        int costN,
        int blockSizeR,
        int parallelization,
        Span<byte> output)
    {
        int unitWords = 2 * blockSizeR * BlockWords;   // 32*r words = 128*r bytes per ROMix unit
        long totalBytes = (long)parallelization * unitWords * 4;
        long vWords = (long)costN * unitWords;
        if (totalBytes > Array.MaxLength || vWords > Array.MaxLength)
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_KdfMemoryExceedsLimit);

        // Step 1: B = PBKDF2-HMAC-SHA256(P, S, 1, p * 128 * r).
        byte[] b = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations: 1, HashAlgorithmName.SHA256, parallelization * 128 * blockSizeR);

        uint[] words = new uint[parallelization * unitWords];
        BytesToWordsLE(b, words);

        try
        {
            // Step 2: B_i = scryptROMix(r, B_i, N) for each of the p independent blocks.
            for (int i = 0; i < parallelization; i++)
                ROMix(words.AsSpan(i * unitWords, unitWords), costN, blockSizeR);

            WordsToBytesLE(words, b);

            // Step 3: DK = PBKDF2-HMAC-SHA256(P, B, 1, dkLen).
            Rfc2898DeriveBytes.Pbkdf2(password, b, output, iterations: 1, HashAlgorithmName.SHA256);
        }
        finally
        {
            CryptographyHelper.Clear(words);
            CryptographyHelper.Clear(b);
        }
    }

    /// <summary>
    /// Applies <c>scryptROMix</c> to a single 128*r-byte block in place (RFC 7914, Section 5).
    /// </summary>
    /// <param name="block">The 128·r-byte block, as 32-bit words, processed in place.</param>
    /// <param name="costN">The CPU/memory cost parameter <c>N</c>.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    private static void ROMix(Span<uint> block, int costN, int blockSizeR)
    {
        int unitWords = block.Length;

        // The caller (DeriveKey) has already verified costN * unitWords fits within Array.MaxLength.
        uint[] x = block.ToArray();
        uint[] y = new uint[unitWords];
        uint[] v = new uint[costN * unitWords];

        try
        {
            for (int i = 0; i < costN; i++)
            {
                x.AsSpan().CopyTo(v.AsSpan(i * unitWords, unitWords));
                BlockMix(x, y, blockSizeR);
                (x, y) = (y, x);
            }

            for (int i = 0; i < costN; i++)
            {
                int j = (int)(Integerify(x, blockSizeR) % (ulong)costN);
                ReadOnlySpan<uint> vj = v.AsSpan(j * unitWords, unitWords);
                for (int k = 0; k < unitWords; k++)
                    x[k] ^= vj[k];

                BlockMix(x, y, blockSizeR);
                (x, y) = (y, x);
            }

            x.AsSpan().CopyTo(block);
        }
        finally
        {
            CryptographyHelper.Clear(v);
            CryptographyHelper.Clear(x);
            CryptographyHelper.Clear(y);
        }
    }

    /// <summary>
    /// Applies <c>scryptBlockMix</c>, writing the shuffled result to <paramref name="output" /> (RFC 7914, Section 4).
    /// </summary>
    /// <param name="input">The 2·r 64-byte input blocks, as 32-bit words.</param>
    /// <param name="output">The destination that receives the shuffled blocks.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    private static void BlockMix(ReadOnlySpan<uint> input, Span<uint> output, int blockSizeR)
    {
        int blocks = 2 * blockSizeR;

        Span<uint> x = stackalloc uint[BlockWords];
        input.Slice((blocks - 1) * BlockWords, BlockWords).CopyTo(x);

        for (int i = 0; i < blocks; i++)
        {
            ReadOnlySpan<uint> bi = input.Slice(i * BlockWords, BlockWords);
            for (int j = 0; j < BlockWords; j++)
                x[j] ^= bi[j];

            Salsa20_8(x);

            // Even-indexed results occupy the first half; odd-indexed the second.
            int dest = (i % 2 == 0) ? (i / 2) : (blockSizeR + (i - 1) / 2);
            x.CopyTo(output.Slice(dest * BlockWords, BlockWords));
        }
    }

    /// <summary>
    /// Interprets the last 64-byte block as a little-endian integer (RFC 7914, Section 5).
    /// </summary>
    /// <param name="block">The block whose final 64-byte sub-block is interpreted.</param>
    /// <param name="blockSizeR">The block-size parameter <c>r</c>.</param>
    /// <returns>The little-endian integer value of the final 64-byte sub-block.</returns>
    private static ulong Integerify(ReadOnlySpan<uint> block, int blockSizeR)
    {
        int offset = (2 * blockSizeR - 1) * BlockWords;
        return ((ulong)block[offset + 1] << 32) | block[offset];
    }

    /// <summary>
    /// Applies the Salsa20/8 core in place: <c>B = B + doubleround^4(B)</c> (RFC 7914, Section 3).
    /// </summary>
    /// <param name="b">The sixteen 32-bit state words transformed in place.</param>
    private static void Salsa20_8(Span<uint> b)
    {
        Span<uint> x = stackalloc uint[BlockWords];
        b.CopyTo(x);

        for (int i = 0; i < 8; i += 2)
        {
            QuarterRound(ref x[0], ref x[4], ref x[8], ref x[12]);
            QuarterRound(ref x[5], ref x[9], ref x[13], ref x[1]);
            QuarterRound(ref x[10], ref x[14], ref x[2], ref x[6]);
            QuarterRound(ref x[15], ref x[3], ref x[7], ref x[11]);

            QuarterRound(ref x[0], ref x[1], ref x[2], ref x[3]);
            QuarterRound(ref x[5], ref x[6], ref x[7], ref x[4]);
            QuarterRound(ref x[10], ref x[11], ref x[8], ref x[9]);
            QuarterRound(ref x[15], ref x[12], ref x[13], ref x[14]);
        }

        for (int i = 0; i < BlockWords; i++)
            b[i] += x[i];
    }

    /// <summary>
    /// Applies the Salsa20 quarter-round to four state words in place.
    /// </summary>
    /// <param name="a">The first state word, updated in place.</param>
    /// <param name="b">The second state word, updated in place.</param>
    /// <param name="c">The third state word, updated in place.</param>
    /// <param name="d">The fourth state word, updated in place.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1107:Code should not contain multiple statements on one line", Justification = "The grouped add / rotate / XOR steps mirror the Salsa20 quarter-round definition.")]
    private static void QuarterRound(ref uint a, ref uint b, ref uint c, ref uint d)
    {
        b ^= BitOperations.RotateLeft(a + d, 7);
        c ^= BitOperations.RotateLeft(b + a, 9);
        d ^= BitOperations.RotateLeft(c + b, 13);
        a ^= BitOperations.RotateLeft(d + c, 18);
    }

    /// <summary>
    /// Converts a little-endian byte buffer into 32-bit words.
    /// </summary>
    /// <param name="source">The little-endian byte buffer to read.</param>
    /// <param name="destination">The span that receives the decoded 32-bit words.</param>
    private static void BytesToWordsLE(ReadOnlySpan<byte> source, Span<uint> destination)
    {
        for (int i = 0; i < destination.Length; i++)
            destination[i] = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(i * 4, 4));
    }

    /// <summary>
    /// Converts 32-bit words into a little-endian byte buffer.
    /// </summary>
    /// <param name="source">The 32-bit words to encode.</param>
    /// <param name="destination">The span that receives the little-endian bytes.</param>
    private static void WordsToBytesLE(ReadOnlySpan<uint> source, Span<byte> destination)
    {
        for (int i = 0; i < source.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(i * 4, 4), source[i]);
    }
}
