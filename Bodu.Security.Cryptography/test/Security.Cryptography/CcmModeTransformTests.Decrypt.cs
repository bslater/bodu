// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class CcmModeTransformTests
{
    /// <summary>
    /// Verifies that ciphertext and tags the platform's <see cref="AesCcm" /> produced authenticate and decrypt to the
    /// original plaintext across the same lengths and alignments as the encryption cross-check.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenGivenPlatformAesCcmOutput_ShouldRecoverPlaintext()
    {
        if (!AesCcm.IsSupported)
            Assert.Inconclusive("AesCcm is not supported on this platform.");

        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength + 21);
            byte[] nonce = AesReference.RandomBytes(12, keyLength + 22);
            using var platform = new AesCcm(key);

            foreach (int length in AesReference.MessageLengths)
            {
                foreach (int aadLength in AesReference.AssociatedDataLengths)
                {
                    byte[] plaintext = AesReference.RandomBytes(length, length + 23);
                    byte[] aad = AesReference.RandomBytes(aadLength, aadLength + 29);
                    byte[] sealedMessage = new byte[length + 16];
                    platform.Encrypt(nonce, plaintext, sealedMessage.AsSpan(0, length), sealedMessage.AsSpan(length), aad);

                    using var cipher = new AesBlockCipher(key);
                    using var transform = new CcmModeTransform(cipher, PadNonce(nonce));
                    transform.ProcessAssociatedData(aad);
                    byte[] recovered = new byte[length];
                    int written = transform.Decrypt(sealedMessage, recovered);

                    Assert.AreEqual(length, written);
                    CollectionAssert.AreEqual(plaintext, recovered, $"AES-{keyLength * 8}, {length} bytes, {aadLength} bytes of AAD");
                }
            }
        }
    }
}
