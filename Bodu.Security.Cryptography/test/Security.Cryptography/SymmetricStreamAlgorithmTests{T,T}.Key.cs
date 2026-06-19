// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests{T,T}.Key.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Key" /> on a fresh cipher lazily generates a key of the
    /// required length.
    /// </summary>
    [TestMethod]
    public void Key_WhenReadOnFreshCipher_ShouldGenerateKeyOfRequiredLength()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.AreEqual(KeyLengthBytes, cipher.Key.Length);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.Key" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.Key);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to <see cref="SymmetricStreamAlgorithm.Key" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToNull_ShouldThrowArgumentNullException()
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            cipher.Key = null!;
        });
    }

    /// <summary>
    /// Verifies that assigning a key whose length in bytes is not one of the cipher's legal key sizes throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="keySizeBytes">The candidate key length, in bytes, expected to be rejected.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBytesData))]
    public void Key_WhenSetToInvalidLength_ShouldThrowCryptographicException(int keySizeBytes)
    {
        if (keySizeBytes < 0) return;

        using TAlgorithm cipher = CreateAlgorithm();
        byte[] invalidKey = new byte[keySizeBytes];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.Key = invalidKey;
        });
    }

    /// <summary>
    /// Verifies that a key assigned to <see cref="SymmetricStreamAlgorithm.Key" /> round-trips on the subsequent get.
    /// </summary>
    [TestMethod]
    public void Key_WhenSet_ShouldReturnSameValueOnGet()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] key = new byte[KeyLengthBytes];
        CryptographyHelper.FillWithRandomNonZeroBytes(key);

        cipher.Key = key;

        CollectionAssert.AreEqual(key, cipher.Key);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricStreamAlgorithm.Key" /> returns a defensive copy rather than the supplied
    /// reference.
    /// </summary>
    [TestMethod]
    public void Key_WhenSet_ShouldReturnDefensiveCopy()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        byte[] key = new byte[KeyLengthBytes];
        CryptographyHelper.FillWithRandomNonZeroBytes(key);

        cipher.Key = key;

        Assert.AreNotSame(key, cipher.Key);
    }

    /// <summary>
    /// Verifies that the <see cref="SymmetricStreamAlgorithm.Key" /> getter returns an independent copy, so mutating the
    /// returned array does not change the cipher's stored key.
    /// </summary>
    [TestMethod]
    public void Key_WhenMutatedAfterRead_ShouldNotAffectStoredKey()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();

        byte[] first = cipher.Key;
        first[0] ^= 0xFF;
        byte[] second = cipher.Key;

        Assert.AreNotEqual(first[0], second[0]);
    }
}
