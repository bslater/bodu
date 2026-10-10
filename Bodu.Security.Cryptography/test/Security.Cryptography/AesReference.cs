// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Block-at-a-time building blocks on the platform's <see cref="Aes" />, from which the mode tests assemble reference
/// implementations that share no code with the transforms they check.
/// </summary>
internal static class AesReference
{
    /// <summary>Returns the lengths the long-message cross-checks use: short tails, one to three blocks, one either side of a four-block group and of a 4 KiB run, and several runs.</summary>
    internal static readonly int[] MessageLengths = [0, 1, 15, 16, 17, 63, 64, 65, 4095, 4096, 4097, (3 * 4096) + 17, 20000];

    /// <summary>Returns the associated-data lengths the long-message cross-checks use: none, a partial block, one past a block, four blocks, and an unaligned run past four blocks.</summary>
    internal static readonly int[] AssociatedDataLengths = [0, 1, 17, 64, 100];

    /// <summary>Returns the block-aligned lengths the unauthenticated-mode cross-checks use: one to four blocks, one block either side of a 4 KiB run, several runs, and 20,000 bytes.</summary>
    internal static readonly int[] AlignedLengths = [16, 32, 64, 4080, 4096, 4112, (3 * 4096) + 80, 20000];

    /// <summary>
    /// Creates a platform AES instance keyed with <paramref name="key" />.
    /// </summary>
    /// <param name="key">The 16-, 24-, or 32-byte key.</param>
    /// <returns>The keyed instance; the caller disposes it.</returns>
    internal static Aes Create(byte[] key)
    {
        var aes = Aes.Create();
        aes.Key = key;
        return aes;
    }

    /// <summary>
    /// Encrypts one block.
    /// </summary>
    /// <param name="aes">The keyed instance.</param>
    /// <param name="block">The 16-byte block.</param>
    /// <returns>The encrypted block.</returns>
    internal static byte[] Encrypt(Aes aes, byte[] block) =>
        aes.EncryptEcb(block, PaddingMode.None);

    /// <summary>
    /// Decrypts one block.
    /// </summary>
    /// <param name="aes">The keyed instance.</param>
    /// <param name="block">The 16-byte block.</param>
    /// <returns>The decrypted block.</returns>
    internal static byte[] Decrypt(Aes aes, byte[] block) =>
        aes.DecryptEcb(block, PaddingMode.None);

    /// <summary>
    /// Returns the byte-wise XOR of two equal-length buffers.
    /// </summary>
    /// <param name="left">The first buffer.</param>
    /// <param name="right">The second buffer.</param>
    /// <returns>A new buffer holding the XOR.</returns>
    internal static byte[] Xor(byte[] left, byte[] right)
    {
        byte[] result = new byte[left.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = (byte)(left[i] ^ right[i]);

        return result;
    }

    /// <summary>
    /// Doubles a block in <c>GF(2¹²⁸)</c> with big-endian bit order, reducing by <c>0x87</c>: the <c>dbl()</c> of CMAC,
    /// SIV, and OCB.
    /// </summary>
    /// <param name="block">The 16-byte block.</param>
    /// <returns>The doubled block.</returns>
    internal static byte[] Double(byte[] block)
    {
        byte[] result = new byte[16];
        for (int i = 0; i < 15; i++)
            result[i] = (byte)((block[i] << 1) | (block[i + 1] >> 7));

        result[15] = (byte)(block[15] << 1);
        if ((block[0] & 0x80) != 0)
            result[15] ^= 0x87;

        return result;
    }

    /// <summary>
    /// Computes AES-CMAC (RFC 4493) one block at a time.
    /// </summary>
    /// <param name="aes">The keyed instance.</param>
    /// <param name="message">The message.</param>
    /// <returns>The 16-byte MAC.</returns>
    internal static byte[] Cmac(Aes aes, byte[] message)
    {
        byte[] k1 = Double(Encrypt(aes, new byte[16]));
        byte[] k2 = Double(k1);
        int blocks = Math.Max(1, (message.Length + 15) / 16);
        bool lastIsFull = message.Length > 0 && message.Length % 16 == 0;

        byte[] mac = new byte[16];
        for (int index = 0; index < blocks - 1; index++)
            mac = Encrypt(aes, Xor(mac, message[(16 * index)..(16 * (index + 1))]));

        byte[] last = new byte[16];
        int lastLength = message.Length - (16 * (blocks - 1));
        Array.Copy(message, 16 * (blocks - 1), last, 0, lastLength);
        if (!lastIsFull)
            last[lastLength] = 0x80;

        return Encrypt(aes, Xor(mac, Xor(last, lastIsFull ? k1 : k2)));
    }

    /// <summary>
    /// Returns a deterministic pseudo-random buffer.
    /// </summary>
    /// <param name="length">The buffer's length.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The buffer.</returns>
    internal static byte[] RandomBytes(int length, int seed)
    {
        byte[] buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }
}
