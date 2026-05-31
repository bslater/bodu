// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.Padding.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;

public abstract partial class StreamCipherAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.Padding" /> reports <see cref="PaddingMode.None" />, the only
    /// padding an additive stream cipher supports.
    /// </summary>
    [TestMethod]
    public void Padding_WhenRead_ShouldReturnNone()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.AreEqual(PaddingMode.None, cipher.Padding);
    }

    /// <summary>
    /// Verifies that assigning <see cref="PaddingMode.None" /> to <see cref="StreamCipherAlgorithm.Padding" /> is
    /// accepted.
    /// </summary>
    [TestMethod]
    public void Padding_WhenSetToNone_ShouldSucceed()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.Padding = PaddingMode.None;

        Assert.AreEqual(PaddingMode.None, cipher.Padding);
    }

    /// <summary>
    /// Verifies that assigning a padding mode other than <see cref="PaddingMode.None" /> to
    /// <see cref="StreamCipherAlgorithm.Padding" /> throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Padding_WhenSetToNonNone_ShouldThrowCryptographicException()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.Padding = PaddingMode.PKCS7;
        });
    }

    /// <summary>
    /// Verifies that assigning to <see cref="StreamCipherAlgorithm.Padding" /> on a disposed cipher throws
    /// <see cref="ObjectDisposedException" />, keeping the property lifecycle consistent with the rest of the algorithm
    /// state.
    /// </summary>
    [TestMethod]
    public void Padding_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.Padding = PaddingMode.None;
        });
    }
}
