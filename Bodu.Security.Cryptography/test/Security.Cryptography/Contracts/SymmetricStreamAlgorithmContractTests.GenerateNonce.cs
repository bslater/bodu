// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmContractTests.GenerateNonce.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Contracts;

public abstract partial class SymmetricStreamAlgorithmContractTests<TCipher>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.GenerateNonce" /> produces a nonce of the algorithm's required
    /// length.
    /// </summary>
    [TestMethod]
    public void GenerateNonce_WhenCalled_ShouldProduceNonceOfRequiredLength()
    {
        using TCipher cipher = CreateAlgorithm();
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
        using TCipher cipher = CreateAlgorithm();
        cipher.GenerateNonce();
        byte[] first = (byte[])cipher.Nonce.Clone();

        cipher.GenerateNonce();
        byte[] second = cipher.Nonce;

        CollectionAssert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.GenerateNonce" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateNonce_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TCipher cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.GenerateNonce();
        });
    }
}
