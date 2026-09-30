// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the table-driven Serpent-128 that <see cref="SerpentCore" /> replaced - S-boxes applied by gathering each
/// 4-bit input across the four words and reading it through a 16-entry table - kept as an independent oracle that the
/// circuit tests and the differential tests hold the core to.
/// </summary>
internal static class SerpentReference
{
    /// <summary>The number of Serpent-128 rounds.</summary>
    private const int RoundCount = 32;

    /// <summary>The number of round-key words.</summary>
    private const int RoundKeyWordCount = (RoundCount + 1) * 4;

    /// <summary>The golden-ratio constant of the prekey recurrence.</summary>
    private const uint Phi = 0x9E3779B9u;

    /// <summary>The eight Serpent S-boxes, indexed <c>[sboxIndex * 16 + nibble]</c>.</summary>
    private static readonly byte[] s_sBoxes =
    [
        3, 8, 15, 1, 10, 6, 5, 11, 14, 13, 4, 2, 7, 0, 9, 12,
        15, 12, 2, 7, 9, 0, 5, 10, 1, 11, 14, 8, 6, 13, 3, 4,
        8, 6, 7, 9, 3, 12, 10, 15, 13, 1, 14, 4, 0, 11, 5, 2,
        0, 15, 11, 8, 12, 9, 6, 3, 13, 1, 2, 4, 10, 7, 5, 14,
        1, 15, 8, 3, 12, 0, 11, 6, 2, 5, 4, 10, 9, 14, 7, 13,
        15, 5, 2, 11, 4, 10, 9, 12, 0, 3, 14, 8, 13, 6, 7, 1,
        7, 2, 12, 5, 8, 4, 6, 11, 14, 9, 1, 15, 13, 3, 10, 0,
        1, 13, 15, 0, 14, 8, 2, 11, 7, 4, 12, 10, 9, 3, 5, 6,
    ];

    /// <summary>The eight Serpent inverse S-boxes, indexed <c>[sboxIndex * 16 + nibble]</c>.</summary>
    private static readonly byte[] s_invSBoxes =
    [
        13, 3, 11, 0, 10, 6, 5, 12, 1, 14, 4, 7, 15, 9, 8, 2,
        5, 8, 2, 14, 15, 6, 12, 3, 11, 4, 7, 9, 1, 13, 10, 0,
        12, 9, 15, 4, 11, 14, 1, 2, 0, 3, 6, 13, 5, 8, 10, 7,
        0, 9, 10, 7, 11, 14, 6, 13, 3, 5, 12, 2, 4, 8, 15, 1,
        5, 0, 8, 3, 10, 9, 7, 14, 2, 12, 11, 6, 4, 15, 13, 1,
        8, 15, 2, 9, 4, 1, 13, 14, 11, 6, 5, 3, 7, 12, 10, 0,
        15, 10, 1, 13, 5, 3, 6, 0, 4, 9, 14, 7, 2, 12, 8, 11,
        3, 0, 6, 13, 9, 14, 15, 8, 5, 12, 11, 7, 10, 1, 4, 2,
    ];

    /// <summary>
    /// Expands a 16-, 24- or 32-byte key into the 132 Serpent-128 round-key words.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The round keys.</returns>
    internal static uint[] ExpandKey(ReadOnlySpan<byte> key)
    {
        byte[] paddedKey = new byte[32];
        key.CopyTo(paddedKey);
        if (key.Length < 32)
            paddedKey[key.Length] = 0x01;

        uint[] prekeys = new uint[8 + RoundKeyWordCount];
        for (int i = 0; i < 8; i++)
            prekeys[i] = BinaryPrimitives.ReadUInt32LittleEndian(paddedKey.AsSpan(i * 4));

        for (int i = 0; i + 8 < prekeys.Length; i++)
        {
            uint value = prekeys[i] ^ prekeys[i + 3] ^ prekeys[i + 5] ^ prekeys[i + 7] ^ Phi ^ (uint)i;
            prekeys[i + 8] = BitOperations.RotateLeft(value, 11);
        }

        uint[] roundKeys = new uint[RoundKeyWordCount];
        for (int r = 0; r <= RoundCount; r++)
        {
            int src = 8 + (r * 4);
            uint x0 = prekeys[src];
            uint x1 = prekeys[src + 1];
            uint x2 = prekeys[src + 2];
            uint x3 = prekeys[src + 3];

            ApplySBox((3 - r) & 7, ref x0, ref x1, ref x2, ref x3);

            roundKeys[r * 4] = x0;
            roundKeys[(r * 4) + 1] = x1;
            roundKeys[(r * 4) + 2] = x2;
            roundKeys[(r * 4) + 3] = x3;
        }

        return roundKeys;
    }

    /// <summary>
    /// Encrypts one 16-byte block under expanded round keys.
    /// </summary>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The plaintext block.</param>
    /// <returns>The ciphertext block.</returns>
    internal static byte[] Encrypt(uint[] roundKeys, ReadOnlySpan<byte> input)
    {
        uint x0 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint x1 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        uint x2 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        uint x3 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);

        for (int r = 0; r < RoundCount; r++)
        {
            x0 ^= roundKeys[r * 4];
            x1 ^= roundKeys[(r * 4) + 1];
            x2 ^= roundKeys[(r * 4) + 2];
            x3 ^= roundKeys[(r * 4) + 3];

            ApplySBox(r & 7, ref x0, ref x1, ref x2, ref x3);

            if (r < RoundCount - 1)
                LinearTransform(ref x0, ref x1, ref x2, ref x3);
        }

        x0 ^= roundKeys[RoundCount * 4];
        x1 ^= roundKeys[(RoundCount * 4) + 1];
        x2 ^= roundKeys[(RoundCount * 4) + 2];
        x3 ^= roundKeys[(RoundCount * 4) + 3];

        return Words(x0, x1, x2, x3);
    }

    /// <summary>
    /// Decrypts one 16-byte block under expanded round keys.
    /// </summary>
    /// <param name="roundKeys">The 132 round-key words.</param>
    /// <param name="input">The ciphertext block.</param>
    /// <returns>The plaintext block.</returns>
    internal static byte[] Decrypt(uint[] roundKeys, ReadOnlySpan<byte> input)
    {
        uint x0 = BinaryPrimitives.ReadUInt32LittleEndian(input);
        uint x1 = BinaryPrimitives.ReadUInt32LittleEndian(input[4..]);
        uint x2 = BinaryPrimitives.ReadUInt32LittleEndian(input[8..]);
        uint x3 = BinaryPrimitives.ReadUInt32LittleEndian(input[12..]);

        x0 ^= roundKeys[RoundCount * 4];
        x1 ^= roundKeys[(RoundCount * 4) + 1];
        x2 ^= roundKeys[(RoundCount * 4) + 2];
        x3 ^= roundKeys[(RoundCount * 4) + 3];

        for (int r = RoundCount - 1; r >= 0; r--)
        {
            if (r < RoundCount - 1)
                InverseLinearTransform(ref x0, ref x1, ref x2, ref x3);

            ApplyInverseSBox(r & 7, ref x0, ref x1, ref x2, ref x3);

            x0 ^= roundKeys[r * 4];
            x1 ^= roundKeys[(r * 4) + 1];
            x2 ^= roundKeys[(r * 4) + 2];
            x3 ^= roundKeys[(r * 4) + 3];
        }

        return Words(x0, x1, x2, x3);
    }

    /// <summary>
    /// Applies an S-box to four words by gathering each 4-bit input across them and reading it through the table.
    /// </summary>
    /// <param name="sBoxIndex">The S-box index, 0 to 7.</param>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    internal static void ApplySBox(int sBoxIndex, ref uint x0, ref uint x1, ref uint x2, ref uint x3) =>
        Substitute(s_sBoxes.AsSpan(sBoxIndex * 16, 16), ref x0, ref x1, ref x2, ref x3);

    /// <summary>
    /// Applies an inverse S-box to four words by gathering each 4-bit input across them and reading it through the
    /// table.
    /// </summary>
    /// <param name="sBoxIndex">The index of the S-box whose inverse applies, 0 to 7.</param>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    internal static void ApplyInverseSBox(int sBoxIndex, ref uint x0, ref uint x1, ref uint x2, ref uint x3) =>
        Substitute(s_invSBoxes.AsSpan(sBoxIndex * 16, 16), ref x0, ref x1, ref x2, ref x3);

    /// <summary>
    /// Applies the Serpent linear transform to four words.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    internal static void LinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        x0 = BitOperations.RotateLeft(x0, 13);
        x2 = BitOperations.RotateLeft(x2, 3);
        x1 ^= x0 ^ x2;
        x3 ^= x2 ^ (x0 << 3);
        x1 = BitOperations.RotateLeft(x1, 1);
        x3 = BitOperations.RotateLeft(x3, 7);
        x0 ^= x1 ^ x3;
        x2 ^= x3 ^ (x1 << 7);
        x0 = BitOperations.RotateLeft(x0, 5);
        x2 = BitOperations.RotateLeft(x2, 22);
    }

    /// <summary>
    /// Applies the inverse of the Serpent linear transform to four words.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    internal static void InverseLinearTransform(ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        x2 = BitOperations.RotateRight(x2, 22);
        x0 = BitOperations.RotateRight(x0, 5);
        x2 ^= x3 ^ (x1 << 7);
        x0 ^= x1 ^ x3;
        x3 = BitOperations.RotateRight(x3, 7);
        x1 = BitOperations.RotateRight(x1, 1);
        x3 ^= x2 ^ (x0 << 3);
        x1 ^= x0 ^ x2;
        x2 = BitOperations.RotateRight(x2, 3);
        x0 = BitOperations.RotateRight(x0, 13);
    }

    /// <summary>
    /// Substitutes each of the 32 bit columns of four words through a 16-entry table.
    /// </summary>
    /// <param name="table">The table.</param>
    /// <param name="x0">The first word: bit 0 of every column.</param>
    /// <param name="x1">The second word: bit 1 of every column.</param>
    /// <param name="x2">The third word: bit 2 of every column.</param>
    /// <param name="x3">The fourth word: bit 3 of every column.</param>
    private static void Substitute(ReadOnlySpan<byte> table, ref uint x0, ref uint x1, ref uint x2, ref uint x3)
    {
        uint y0 = 0, y1 = 0, y2 = 0, y3 = 0;

        for (int i = 0; i < 32; i++)
        {
            int nibble = (int)(((x0 >> i) & 1u)
                             | (((x1 >> i) & 1u) << 1)
                             | (((x2 >> i) & 1u) << 2)
                             | (((x3 >> i) & 1u) << 3));

            int s = table[nibble];
            y0 |= (uint)(s & 1) << i;
            y1 |= (uint)((s >> 1) & 1) << i;
            y2 |= (uint)((s >> 2) & 1) << i;
            y3 |= (uint)((s >> 3) & 1) << i;
        }

        x0 = y0;
        x1 = y1;
        x2 = y2;
        x3 = y3;
    }

    /// <summary>
    /// Writes four words little-endian into a new 16-byte block.
    /// </summary>
    /// <param name="x0">The first word.</param>
    /// <param name="x1">The second word.</param>
    /// <param name="x2">The third word.</param>
    /// <param name="x3">The fourth word.</param>
    /// <returns>The block.</returns>
    private static byte[] Words(uint x0, uint x1, uint x2, uint x3)
    {
        byte[] block = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(block, x0);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), x1);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(8), x2);
        BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(12), x3);
        return block;
    }
}
