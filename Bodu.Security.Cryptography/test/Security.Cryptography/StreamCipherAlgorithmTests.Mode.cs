// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.Mode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using System.Security.Cryptography;

public abstract partial class StreamCipherAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.Mode" /> reports <see cref="CipherMode.ECB" />, the only mode an
    /// additive stream cipher supports.
    /// </summary>
    [TestMethod]
    public void Mode_WhenRead_ShouldReturnEcb()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.AreEqual(CipherMode.ECB, cipher.Mode);
    }

    /// <summary>
    /// Verifies that assigning <see cref="CipherMode.ECB" /> to <see cref="StreamCipherAlgorithm.Mode" /> is accepted.
    /// </summary>
    [TestMethod]
    public void Mode_WhenSetToEcb_ShouldSucceed()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.Mode = CipherMode.ECB;

        Assert.AreEqual(CipherMode.ECB, cipher.Mode);
    }

    /// <summary>
    /// Verifies that assigning a mode other than <see cref="CipherMode.ECB" /> to
    /// <see cref="StreamCipherAlgorithm.Mode" /> throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Mode_WhenSetToNonEcb_ShouldThrowCryptographicException()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.Mode = CipherMode.CBC;
        });
    }
}
