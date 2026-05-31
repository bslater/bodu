// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherAlgorithmTests.Sizes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class StreamCipherAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a default-constructed cipher exposes the expected key size and nonce size, and that
    /// <see cref="System.Security.Cryptography.SymmetricAlgorithm.BlockSize" /> mirrors the nonce size — locking in the
    /// public algorithm shape, including the deliberate <c>BlockSize == NonceSize</c> compatibility mapping.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCreated_ShouldExposeExpectedKeyAndNonceSizes()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.AreEqual(ExpectedKeySizeBits, cipher.KeySize);
        Assert.AreEqual(ExpectedNonceSizeBits, cipher.NonceSize);
        Assert.AreEqual(ExpectedNonceSizeBits, cipher.BlockSize);
    }
}
