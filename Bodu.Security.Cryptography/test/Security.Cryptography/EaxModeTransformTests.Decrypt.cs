// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.Decrypt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class EaxModeTransformTests
{
    /// <summary>
    /// Verifies that messages the block-at-a-time reference sealed authenticate and decrypt to the original plaintext
    /// across the same lengths and alignments as the encryption cross-check.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenGivenReferenceSealedMessage_ShouldRecoverPlaintext()
    {
        foreach (int keyLength in new[] { 16, 32 })
        {
            byte[] key = AesReference.RandomBytes(keyLength, keyLength + 21);
            byte[] nonce = AesReference.RandomBytes(16, keyLength + 22);

            foreach (int length in AesReference.MessageLengths)
            {
                foreach (int aadLength in AesReference.AssociatedDataLengths)
                {
                    byte[] plaintext = AesReference.RandomBytes(length, length + 23);
                    byte[] aad = AesReference.RandomBytes(aadLength, aadLength + 29);
                    byte[] sealedMessage = ReferenceSeal(key, nonce, aad, plaintext);

                    using var cipher = new AesBlockCipher(key);
                    using var transform = new EaxModeTransform(cipher, nonce);
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
