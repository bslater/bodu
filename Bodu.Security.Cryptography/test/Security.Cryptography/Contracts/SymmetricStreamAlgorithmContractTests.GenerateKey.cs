// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmContractTests.GenerateKey.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

public abstract partial class SymmetricStreamAlgorithmContractTests<TCipher>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.GenerateKey" /> produces a key of the algorithm's required
    /// length.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldProduceKeyOfRequiredLength()
    {
        using TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();

        Assert.AreEqual(KeyLengthBytes, cipher.Key.Length);
    }

    /// <summary>
    /// Verifies that two successive <see cref="SymmetricStreamAlgorithm.GenerateKey" /> calls produce different keys,
    /// confirming the generator draws fresh random material rather than a constant.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalledTwice_ShouldProduceDifferentKeys()
    {
        using TCipher cipher = CreateAlgorithm();
        cipher.GenerateKey();
        byte[] first = (byte[])cipher.Key.Clone();

        cipher.GenerateKey();
        byte[] second = cipher.Key;

        CollectionAssert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.GenerateKey" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.GenerateKey();
        });
    }

    /// <summary>
    /// Verifies that a key and nonce produced by <see cref="SymmetricStreamAlgorithm.GenerateKey" /> and
    /// <see cref="SymmetricStreamAlgorithm.GenerateNonce" /> drive a successful encrypt / decrypt round-trip.
    /// </summary>
    [TestMethod]
    public void GenerateKeyAndNonce_WhenUsed_ShouldDriveSuccessfulRoundTrip()
    {
        using TCipher cipher = CreateAlgorithm();
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
}
