// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.GenerateKey.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.GenerateKey" /> produces a key of the algorithm's required
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
    /// Verifies that two successive <see cref="SymmetricStreamAlgorithm.GenerateKey" /> calls produce different keys,
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
    /// Verifies that calling <see cref="SymmetricStreamAlgorithm.GenerateKey" /> on a disposed cipher throws
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
}
