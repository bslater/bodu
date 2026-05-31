// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.GenerateIV.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;

public abstract partial class StreamCipherAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.GenerateIV" /> produces a nonce of the algorithm's required
    /// length.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenCalled_ShouldProduceNonceOfRequiredLength()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateIV();

        Assert.AreEqual(NonceLengthBytes, cipher.IV.Length);
    }

    /// <summary>
    /// Verifies that two successive <see cref="StreamCipherAlgorithm.GenerateIV" /> calls produce different nonces,
    /// confirming the generator draws fresh random material rather than a constant.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenCalledTwice_ShouldProduceDifferentNonces()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateIV();
        byte[] first = (byte[])cipher.IV.Clone();

        cipher.GenerateIV();
        byte[] second = cipher.IV;

        CollectionAssert.AreNotEqual(first, second);
    }

    /// <summary>
    /// Verifies that calling <see cref="StreamCipherAlgorithm.GenerateIV" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.GenerateIV();
        });
    }
}
