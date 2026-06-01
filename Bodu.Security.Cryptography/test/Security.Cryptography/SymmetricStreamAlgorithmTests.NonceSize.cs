// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.NonceSize.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a default-constructed cipher reports the expected nonce size.
    /// </summary>
    [TestMethod]
    public void NonceSize_WhenCreated_ShouldExposeExpectedNonceSize()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        Assert.AreEqual(GetSpecification().NonceSizeBits, cipher.NonceSize);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.NonceSize" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void NonceSize_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.NonceSize);
    }
}
