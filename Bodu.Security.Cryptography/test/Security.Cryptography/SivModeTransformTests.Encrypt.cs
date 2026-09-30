// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class SivModeTransformTests
{
    /// <summary>
    /// Verifies that the ciphertext and synthetic IV match a block-at-a-time RFC 5297 reference on the platform's AES,
    /// with AES-128 and AES-256 key halves, across message lengths that fit in one run of counters, straddle one, and
    /// span several, with associated data of every alignment.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenMessageSpansSeveralRuns_ShouldMatchBlockAtATimeReference()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] s2vKey = AesReference.RandomBytes(keyLength, keyLength);
            byte[] ctrKey = AesReference.RandomBytes(keyLength, keyLength + 1);

            foreach (int length in AesReference.MessageLengths)
            {
                foreach (int aadLength in AesReference.AssociatedDataLengths)
                {
                    byte[] plaintext = AesReference.RandomBytes(length, length + 7);
                    byte[] aad = AesReference.RandomBytes(aadLength, aadLength + 11);

                    using var s2vCipher = new AesBlockCipher(s2vKey);
                    using var ctrCipher = new AesBlockCipher(ctrKey);
                    using var transform = new SivModeTransform(s2vCipher, ctrCipher, new byte[16]);
                    transform.ProcessAssociatedData(aad);
                    byte[] actual = new byte[length + 16];
                    transform.Encrypt(plaintext, actual);

                    CollectionAssert.AreEqual(ReferenceSeal(s2vKey, ctrKey, aad, plaintext), actual, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }

    /// <summary>
    /// Seals a message with RFC 5297 AES-SIV one block at a time: S2V over the associated data - a string only when it is
    /// non-empty, as the transform defines - and the plaintext, then CTR from the synthetic IV with bits 31 and 63
    /// cleared.
    /// </summary>
    /// <param name="s2vKey">The key <c>K1</c> for S2V.</param>
    /// <param name="ctrKey">The key <c>K2</c> for CTR.</param>
    /// <param name="aad">The associated data.</param>
    /// <param name="plaintext">The plaintext.</param>
    /// <returns>The ciphertext followed by the synthetic IV.</returns>
    private static byte[] ReferenceSeal(byte[] s2vKey, byte[] ctrKey, byte[] aad, byte[] plaintext)
    {
        using Aes s2v = AesReference.Create(s2vKey);
        byte[] d = AesReference.Cmac(s2v, new byte[16]);
        if (aad.Length > 0)
            d = AesReference.Xor(AesReference.Double(d), AesReference.Cmac(s2v, aad));

        byte[] t;
        if (plaintext.Length >= 16)
        {
            t = (byte[])plaintext.Clone();
            for (int i = 0; i < 16; i++)
                t[t.Length - 16 + i] ^= d[i];
        }
        else
        {
            byte[] padded = new byte[16];
            plaintext.CopyTo(padded, 0);
            padded[plaintext.Length] = 0x80;
            t = AesReference.Xor(AesReference.Double(d), padded);
        }

        byte[] v = AesReference.Cmac(s2v, t);

        using Aes ctr = AesReference.Create(ctrKey);
        byte[] counter = (byte[])v.Clone();
        counter[8] &= 0x7F;
        counter[12] &= 0x7F;
        byte[] ciphertext = new byte[plaintext.Length];
        for (int offset = 0; offset < plaintext.Length; offset += 16)
        {
            byte[] keystream = AesReference.Encrypt(ctr, counter);
            for (int i = 0; i < Math.Min(16, plaintext.Length - offset); i++)
                ciphertext[offset + i] = (byte)(plaintext[offset + i] ^ keystream[i]);

            UInt128 next = BinaryPrimitives.ReadUInt128BigEndian(counter) + 1;
            BinaryPrimitives.WriteUInt128BigEndian(counter, next);
        }

        return [.. ciphertext, .. v];
    }
}
