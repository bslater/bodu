// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CounterKeystreamTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="CounterKeystream" />, the keystream layer the counter-based modes share, grouped into
/// member-named partial files.
/// </summary>
[TestClass]
public sealed partial class CounterKeystreamTests
{
    /// <summary>
    /// Gets the 128-bit block ciphers the tests run the counter over: AES, whose counter blocks go through
    /// <see cref="IBlockCipher.EncryptBlocks" /> a run at a time, and Serpent-128, which forms its counter blocks and
    /// applies their keystream in its own kernels.
    /// </summary>
    /// <returns>One row per cipher: its name, and a factory for a fresh keyed instance.</returns>
    public static IEnumerable<object[]> Ciphers()
    {
        yield return new object[] { "AES-128", (Func<IBlockCipher>)(() => new AesBlockCipher(Enumerable.Range(1, 16).Select(i => (byte)i).ToArray())) };
        yield return new object[] { "Serpent-128", (Func<IBlockCipher>)(() => new Serpent128Cipher(Enumerable.Range(1, 32).Select(i => (byte)i).ToArray())) };
    }

    /// <summary>
    /// Computes counter-mode output one block at a time, the counter a big-endian 128-bit integer that increases by one
    /// per whole or partial block, modulo 2^128.
    /// </summary>
    /// <param name="cipher">The cipher producing the keystream.</param>
    /// <param name="initialCounter">The first 16-byte counter block.</param>
    /// <param name="input">The input.</param>
    /// <returns>The output.</returns>
    private static byte[] ReferenceBigEndian128(IBlockCipher cipher, byte[] initialCounter, byte[] input)
    {
        ulong high = BinaryPrimitives.ReadUInt64BigEndian(initialCounter);
        ulong low = BinaryPrimitives.ReadUInt64BigEndian(initialCounter.AsSpan(8));
        byte[] counter = new byte[16];
        byte[] keystream = new byte[16];
        byte[] output = new byte[input.Length];

        for (int offset = 0; offset < input.Length; offset += 16)
        {
            BinaryPrimitives.WriteUInt64BigEndian(counter, high);
            BinaryPrimitives.WriteUInt64BigEndian(counter.AsSpan(8), low);
            cipher.Encrypt(counter, keystream);
            if (++low == 0)
                high++;

            for (int i = 0; i < Math.Min(16, input.Length - offset); i++)
                output[offset + i] = (byte)(input[offset + i] ^ keystream[i]);
        }

        return output;
    }
}
