// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.GenerateKey.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;

public abstract partial class StreamCipherAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.GenerateKey" /> produces a key of the algorithm's required
    /// length.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldProduceKeyOfRequiredLength()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();

        Assert.AreEqual(KeyLengthBytes, cipher.Key.Length);
    }

    /// <summary>
    /// Verifies that two successive <see cref="StreamCipherAlgorithm.GenerateKey" /> calls produce different keys,
    /// confirming the generator draws fresh random material rather than a constant.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalledTwice_ShouldProduceDifferentKeys()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();
        byte[] first = (byte[])cipher.Key.Clone();

        cipher.GenerateKey();
        byte[] second = cipher.Key;

        CollectionAssert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that calling <see cref="StreamCipherAlgorithm.GenerateKey" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.GenerateKey();
        });
    }

    /// <summary>
    /// Verifies that a key and nonce produced by <see cref="StreamCipherAlgorithm.GenerateKey" /> and
    /// <see cref="StreamCipherAlgorithm.GenerateIV" /> drive a successful encrypt / decrypt round-trip.
    /// </summary>
    [TestMethod]
    public void GenerateKeyAndIV_WhenUsed_ShouldDriveSuccessfulRoundTrip()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.GenerateIV();

        Assert.AreEqual(KeyLengthBytes, cipher.Key.Length);
        Assert.AreEqual(NonceLengthBytes, cipher.IV.Length);

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
