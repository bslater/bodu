// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.GenerateNonce.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.GenerateNonce" /> produces a nonce of the algorithm's required
    /// length.
    /// </summary>
    [TestMethod]
    public void GenerateNonce_WhenCalled_ShouldProduceNonceOfRequiredLength()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateNonce();

        Assert.AreEqual(NonceLengthBytes, cipher.Nonce.Length);
    }

    /// <summary>
    /// Verifies that two successive <see cref="SymmetricStreamAlgorithm.GenerateNonce" /> calls produce different
    /// nonces, confirming the generator draws fresh random material rather than a constant.
    /// </summary>
    [TestMethod]
    public void GenerateNonce_WhenCalledTwice_ShouldProduceDifferentNonces()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateNonce();
        var first = (byte[])cipher.Nonce.Clone();

        cipher.GenerateNonce();
        var second = cipher.Nonce;

        CollectionAssert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.GenerateNonce" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateNonce_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.GenerateNonce();
        });
    }
}
