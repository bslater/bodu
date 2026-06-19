// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests{T,T}.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that encrypting a payload and then decrypting the result recovers the original plaintext, confirming
    /// the cipher is self-inverse.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenDecryptingCiphertext_ShouldRecoverPlaintext()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();
        byte[] plaintext = CreatePayload(4097);

        byte[] ciphertext;
        using (TAlgorithm encryptor = CreateAlgorithm())
        using (ICryptoTransform e = encryptor.CreateEncryptor(key, nonce))
            ciphertext = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] recovered;
        using (TAlgorithm decryptor = CreateAlgorithm())
        using (ICryptoTransform d = decryptor.CreateDecryptor(key, nonce))
            recovered = d.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that a key and nonce produced by <see cref="SymmetricStreamAlgorithm.GenerateKey" /> and
    /// <see cref="SymmetricStreamAlgorithm.GenerateNonce" /> drive a successful encrypt / decrypt round-trip.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenGenerateKeyAndNonceUsed_ShouldRecoverPlaintext()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.GenerateNonce();

        Assert.AreEqual(KeyLengthBytes, cipher.Key.Length);
        Assert.AreEqual(NonceLengthBytes, cipher.Nonce.Length);

        byte[] plaintext = CreatePayload(200);

        byte[] ciphertext;
        using (ICryptoTransform e = cipher.CreateEncryptor())
            ciphertext = e.TransformFinalBlock(plaintext, 0, plaintext.Length);

        byte[] recovered;
        using (ICryptoTransform d = cipher.CreateDecryptor())
            recovered = d.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        CollectionAssert.AreEqual(plaintext, recovered);
    }

    /// <summary>
    /// Verifies that encrypting and decrypting a zero-length payload recovers an empty plaintext, confirming the
    /// stream-cipher transform handles the empty boundary case without adding padding.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenEncryptingEmptyInput_ShouldRecoverEmptyPlaintext()
    {
        byte[] key = CreateKey();
        byte[] nonce = CreateNonce();

        byte[] ciphertext;
        using (TAlgorithm encryptor = CreateAlgorithm())
        using (ICryptoTransform e = encryptor.CreateEncryptor(key, nonce))
            ciphertext = e.TransformFinalBlock([], 0, 0);

        Assert.AreEqual(0, ciphertext.Length);

        byte[] recovered;
        using (TAlgorithm decryptor = CreateAlgorithm())
        using (ICryptoTransform d = decryptor.CreateDecryptor(key, nonce))
            recovered = d.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

        Assert.AreEqual(0, recovered.Length);
    }
}
