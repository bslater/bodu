// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.Encrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class CcmModeTransformTests
{
    /// <summary>
    /// Verifies that the ciphertext and tag match the platform's <see cref="AesCcm" /> - an independent implementation
    /// - for AES-128 and AES-256, across message lengths that fit in one run of counters, straddle one, and span
    /// several, with associated data of every alignment.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenMessageSpansSeveralRuns_ShouldMatchPlatformAesCcm()
    {
        if (!AesCcm.IsSupported)
            Assert.Inconclusive("AesCcm is not supported on this platform.");

        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength);
            byte[] nonce = AesReference.RandomBytes(12, keyLength + 1);
            using var platform = new AesCcm(key);

            foreach (int length in AesReference.MessageLengths)
            {
                foreach (int aadLength in AesReference.AssociatedDataLengths)
                {
                    byte[] plaintext = AesReference.RandomBytes(length, length + 7);
                    byte[] aad = AesReference.RandomBytes(aadLength, aadLength + 11);
                    byte[] expected = new byte[length + 16];
                    platform.Encrypt(nonce, plaintext, expected.AsSpan(0, length), expected.AsSpan(length), aad);

                    using var cipher = new AesBlockCipher(key);
                    using var transform = new CcmModeTransform(cipher, PadNonce(nonce));
                    transform.ProcessAssociatedData(aad);
                    byte[] actual = new byte[length + 16];
                    transform.Encrypt(plaintext, actual);

                    CollectionAssert.AreEqual(expected, actual, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }
}
