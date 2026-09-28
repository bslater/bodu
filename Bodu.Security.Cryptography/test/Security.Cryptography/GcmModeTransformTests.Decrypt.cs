// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public sealed partial class GcmModeTransformTests
{
    /// <summary>
    /// Verifies that ciphertext and tags the platform's <see cref="AesGcm" /> produced authenticate and decrypt to the
    /// original plaintext across the same lengths and alignments as the encryption cross-check.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenGivenPlatformAesGcmOutput_ShouldRecoverPlaintext()
    {
        if (!AesGcm.IsSupported)
            Assert.Inconclusive("AesGcm is not supported on this platform.");

        byte[] key = CrossCheckBytes(16, 21);
        byte[] nonce = CrossCheckBytes(NonceSizeBytes, 22);
        using var platform = new AesGcm(key, 16);
        using var cipher = new AesBlockCipher(key);

        foreach (int length in s_crossCheckLengths)
        {
            foreach (int aadLength in s_crossCheckAadLengths)
            {
                byte[] plaintext = CrossCheckBytes(length, length + 23);
                byte[] aad = CrossCheckBytes(aadLength, aadLength + 29);
                byte[] sealedMessage = new byte[length + 16];
                platform.Encrypt(nonce, plaintext, sealedMessage.AsSpan(0, length), sealedMessage.AsSpan(length), aad);

                using var transform = new GcmModeTransform(cipher, nonce);
                transform.ProcessAssociatedData(aad);
                byte[] recovered = new byte[length];
                int written = transform.Decrypt(sealedMessage, recovered);

                Assert.AreEqual(length, written);
                CollectionAssert.AreEqual(plaintext, recovered, $"{length} bytes, {aadLength} bytes of AAD");
            }
        }
    }
}
