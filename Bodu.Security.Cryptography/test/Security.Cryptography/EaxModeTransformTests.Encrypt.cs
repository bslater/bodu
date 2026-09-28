// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class EaxModeTransformTests
{
    /// <summary>
    /// Verifies that the ciphertext and tag match a block-at-a-time EAX reference on the platform's AES for AES-128 and
    /// AES-256, across message lengths that fit in one run of counters, straddle one, and span several, with associated
    /// data of every alignment.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenMessageSpansSeveralRuns_ShouldMatchBlockAtATimeReference()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength);
            byte[] nonce = AesReference.RandomBytes(16, keyLength + 1);

            foreach (int length in AesReference.MessageLengths)
            {
                foreach (int aadLength in AesReference.AssociatedDataLengths)
                {
                    byte[] plaintext = AesReference.RandomBytes(length, length + 7);
                    byte[] aad = AesReference.RandomBytes(aadLength, aadLength + 11);

                    using var cipher = new AesBlockCipher(key);
                    using var transform = new EaxModeTransform(cipher, nonce);
                    transform.ProcessAssociatedData(aad);
                    byte[] actual = new byte[length + 16];
                    transform.Encrypt(plaintext, actual);

                    CollectionAssert.AreEqual(ReferenceSeal(key, nonce, aad, plaintext), actual, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }

    /// <summary>
    /// Seals a message with EAX one block at a time: <c>N' = OMAC⁰(N)</c>, <c>H' = OMAC¹(H)</c>, CTR from
    /// <c>N'</c>, <c>C' = OMAC²(C)</c>, and the tag <c>N' ⊕ H' ⊕ C'</c>, where <c>OMACᵗ(M)</c> is the CMAC of the
    /// block <c>[t]</c> followed by <c>M</c>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="nonce">The 16-byte nonce.</param>
    /// <param name="aad">The associated data.</param>
    /// <param name="plaintext">The plaintext.</param>
    /// <returns>The ciphertext followed by the tag.</returns>
    private static byte[] ReferenceSeal(byte[] key, byte[] nonce, byte[] aad, byte[] plaintext)
    {
        using Aes aes = AesReference.Create(key);
        byte[] nPrime = ReferenceOmac(aes, 0, nonce);
        byte[] hPrime = ReferenceOmac(aes, 1, aad);

        byte[] counter = (byte[])nPrime.Clone();
        byte[] ciphertext = new byte[plaintext.Length];
        for (int offset = 0; offset < plaintext.Length; offset += 16)
        {
            byte[] keystream = AesReference.Encrypt(aes, counter);
            for (int i = 0; i < Math.Min(16, plaintext.Length - offset); i++)
                ciphertext[offset + i] = (byte)(plaintext[offset + i] ^ keystream[i]);

            UInt128 next = BinaryPrimitives.ReadUInt128BigEndian(counter) + 1;
            BinaryPrimitives.WriteUInt128BigEndian(counter, next);
        }

        byte[] tag = AesReference.Xor(AesReference.Xor(nPrime, hPrime), ReferenceOmac(aes, 2, ciphertext));
        return [.. ciphertext, .. tag];
    }

    /// <summary>
    /// Computes <c>OMACᵗ(M)</c>: the CMAC of the block holding <paramref name="t" /> in its last byte, followed by
    /// <paramref name="message" />.
    /// </summary>
    /// <param name="aes">The keyed instance.</param>
    /// <param name="t">The domain tweak.</param>
    /// <param name="message">The message.</param>
    /// <returns>The 16-byte MAC.</returns>
    private static byte[] ReferenceOmac(Aes aes, byte t, byte[] message)
    {
        byte[] prefixed = new byte[16 + message.Length];
        prefixed[15] = t;
        message.CopyTo(prefixed, 16);
        return AesReference.Cmac(aes, prefixed);
    }
}
