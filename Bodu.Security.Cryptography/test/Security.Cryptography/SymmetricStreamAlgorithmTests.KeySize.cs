// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmTests.KeySize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricStreamAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that a default-constructed cipher reports the expected default key size.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenCreated_ShouldExposeExpectedKeySize()
    {
        using TAlgorithm cipher = CreateAlgorithm();
        Assert.AreEqual(GetSpecification().DefaultKeySizeBits, cipher.KeySize);
    }

    /// <summary>
    /// Verifies that reading <see cref="SymmetricStreamAlgorithm.KeySize" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void KeySize_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
    {
        TAlgorithm cipher = CreateAlgorithm();
        cipher.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = cipher.KeySize);
    }

    /// <summary>
    /// Verifies that assigning each legal key size to <see cref="SymmetricStreamAlgorithm.KeySize" /> round-trips on the
    /// subsequent get.
    /// </summary>
    /// <param name="keySizeBits">The key size, in bits, to assign.</param>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizeBitsData))]
    public void KeySize_WhenSet_ShouldReturnSameValueOnGet(int keySizeBits)
    {
        using TAlgorithm cipher = CreateAlgorithm();

        cipher.KeySize = keySizeBits;

        Assert.AreEqual(keySizeBits, cipher.KeySize);
    }

    /// <summary>
    /// Verifies that assigning a key size that is not one of the cipher's legal key sizes throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    /// <param name="keySizeBits">The candidate key size, in bits, expected to be rejected.</param>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBitsData))]
    public void KeySize_WhenSetToInvalidSize_ShouldThrowCryptographicException(int keySizeBits)
    {
        using TAlgorithm cipher = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            cipher.KeySize = keySizeBits;
        });
    }

    /// <summary>
    /// Verifies that reassigning <see cref="SymmetricStreamAlgorithm.KeySize" /> discards any previously cached key, so
    /// the next read of <see cref="SymmetricStreamAlgorithm.Key" /> returns a freshly generated key of the new length.
    /// </summary>
    /// <param name="keySizeBits">The key size, in bits, to assign before reading the key.</param>
    [TestMethod]
    [DynamicData(nameof(LegalKeySizeBitsData))]
    public void KeySize_WhenSet_ShouldDiscardCachedKey(int keySizeBits)
    {
        using TAlgorithm cipher = CreateAlgorithm();
        cipher.GenerateKey();

        cipher.KeySize = keySizeBits;

        Assert.AreEqual(keySizeBits / 8, cipher.Key.Length);
    }
}
