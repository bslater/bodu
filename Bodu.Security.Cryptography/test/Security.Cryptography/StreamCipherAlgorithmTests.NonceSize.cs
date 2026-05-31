// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.NonceSize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class StreamCipherAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="StreamCipherAlgorithm.NonceSize" /> equals
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.BlockSize" />, confirming the nonce-size-as-block-size
    /// compatibility contract.
    /// </summary>
    [TestMethod]
    public void NonceSize_WhenRead_ShouldEqualBlockSize()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.AreEqual(cipher.BlockSize, cipher.NonceSize);
    }
}
