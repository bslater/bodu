// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemEngine.Encoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the ML-KEM bit-packing codecs and coefficient compression (FIPS 203 Algorithms 4–6 and §4.2.1).
/// </summary>
internal static partial class MLKemEngine
{
    /// <summary>
    /// Serializes 256 d-bit coefficients into 32·d bytes in little-endian bit order (FIPS 203 ByteEncode_d).
    /// </summary>
    /// <param name="bits">The bit width d of each coefficient (1–12).</param>
    /// <param name="coefficients">The 256 coefficients, each below 2^d.</param>
    /// <param name="destination">The span receiving 32·d bytes.</param>
    private static void ByteEncode(int bits, ReadOnlySpan<int> coefficients, Span<byte> destination)
    {
        ulong accumulator = 0;
        int bitCount = 0;
        int index = 0;

        for (int i = 0; i < N; i++)
        {
            accumulator |= (ulong)(uint)coefficients[i] << bitCount;
            bitCount += bits;

            while (bitCount >= 8)
            {
                destination[index++] = (byte)accumulator;
                accumulator >>= 8;
                bitCount -= 8;
            }
        }
    }

    /// <summary>
    /// Deserializes 32·d bytes into 256 d-bit coefficients in little-endian bit order (FIPS 203 ByteDecode_d).
    /// </summary>
    /// <param name="bits">The bit width d of each coefficient (1–12).</param>
    /// <param name="source">The 32·d source bytes.</param>
    /// <param name="destination">The span receiving the 256 coefficients.</param>
    private static void ByteDecode(int bits, ReadOnlySpan<byte> source, Span<int> destination)
    {
        int mask = (1 << bits) - 1;
        ulong accumulator = 0;
        int bitCount = 0;
        int index = 0;

        for (int i = 0; i < N; i++)
        {
            while (bitCount < bits)
            {
                accumulator |= (ulong)source[index++] << bitCount;
                bitCount += 8;
            }

            destination[i] = (int)(accumulator & (uint)mask);
            accumulator >>= bits;
            bitCount -= bits;
        }
    }

    /// <summary>
    /// Compresses a coefficient to d bits: ⌈(2^d / q)·x⌋ mod 2^d (FIPS 203 §4.2.1).
    /// </summary>
    /// <param name="bits">The target bit width d.</param>
    /// <param name="value">The coefficient in [0, q).</param>
    /// <returns>The compressed value below 2^d.</returns>
    /// <remarks>
    /// The constant-divisor division is lowered by the JIT to a multiply-and-shift sequence, so the operation does not
    /// perform a data-dependent hardware division.
    /// </remarks>
    private static int Compress(int bits, int value) =>
        (int)((((uint)value << bits) + (Q / 2)) / Q) & ((1 << bits) - 1);

    /// <summary>
    /// Decompresses a d-bit value back to a coefficient: ⌈(q / 2^d)·y⌋ (FIPS 203 §4.2.1).
    /// </summary>
    /// <param name="bits">The source bit width d.</param>
    /// <param name="value">The compressed value below 2^d.</param>
    /// <returns>The decompressed coefficient in [0, q).</returns>
    private static int Decompress(int bits, int value) =>
        (int)(((uint)(value * Q) + (1u << (bits - 1))) >> bits);

    /// <summary>
    /// Compresses a polynomial to d bits per coefficient and serializes it (Compress_d then ByteEncode_d).
    /// </summary>
    /// <param name="bits">The target bit width d.</param>
    /// <param name="coefficients">The 256 coefficients in [0, q).</param>
    /// <param name="destination">The span receiving 32·d bytes.</param>
    private static void CompressEncode(int bits, ReadOnlySpan<int> coefficients, Span<byte> destination)
    {
        Span<int> compressed = stackalloc int[N];
        for (int i = 0; i < N; i++)
            compressed[i] = Compress(bits, coefficients[i]);

        ByteEncode(bits, compressed, destination);
        CryptographyHelper.Clear(compressed);
    }

    /// <summary>
    /// Deserializes a d-bit packed polynomial and decompresses each coefficient (ByteDecode_d then Decompress_d).
    /// </summary>
    /// <param name="bits">The source bit width d.</param>
    /// <param name="source">The 32·d source bytes.</param>
    /// <param name="destination">The span receiving 256 coefficients in [0, q).</param>
    private static void DecodeDecompress(int bits, ReadOnlySpan<byte> source, Span<int> destination)
    {
        ByteDecode(bits, source, destination);
        for (int i = 0; i < N; i++)
            destination[i] = Decompress(bits, destination[i]);
    }
}
