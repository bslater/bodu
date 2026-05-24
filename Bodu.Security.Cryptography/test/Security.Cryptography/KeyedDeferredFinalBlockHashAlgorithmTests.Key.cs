// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedDeferredFinalBlockHashAlgorithmTests.Key.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class KeyedDeferredFinalBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that the <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> property returns an empty
    /// array when the instance is created without a key.
    /// </summary>
    [TestMethod]
    public void Key_WhenDefaultConstructed_ShouldReturnEmptyArray()
    {
        using TAlgorithm sut = CreateAlgorithm();

        var key = sut.Key;

        Assert.IsNotNull(key);
        Assert.AreEqual(0, key.Length);
    }

    /// <summary>
    /// Verifies that assigning <see langword="null" /> to
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToNull_ShouldThrowExactly()
    {
        using TAlgorithm sut = CreateAlgorithm();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.Key = null!;
        });
    }

    /// <summary>
    /// Verifies that assigning a key whose length exceeds
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.MaximumKeySize" /> / 8 bytes throws
    /// <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToKeyLongerThanMaximumKeySize_ShouldThrowExactly()
    {
        using TAlgorithm sut = CreateAlgorithm();

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            sut.Key = new byte[(sut.MaximumKeySize / 8) + 1];
        });
    }

    /// <summary>
    /// Verifies that assigning a key of exactly
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.MaximumKeySize" /> / 8 bytes is accepted without throwing.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToMaximumSizeKey_ShouldNotThrow()
    {
        using TAlgorithm sut = CreateAlgorithm();
        var key = new byte[sut.MaximumKeySize / 8];

        sut.Key = key;

        CollectionAssert.AreEqual(key, sut.Key);
    }

    /// <summary>
    /// Verifies that the <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> getter returns a defensive
    /// copy so that mutations to the returned array do not affect the algorithm's internal key.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetterInvoked_ShouldReturnDefensiveCopy()
    {
        using TAlgorithm sut = CreateAlgorithm();
        var keySize = Math.Max(1, sut.MaximumKeySize / 16);
        var key = Enumerable.Range(1, keySize).Select(i => (byte)i).ToArray();
        sut.Key = key;

        var retrieved = sut.Key;
        retrieved[0] = 0xFF;

        Assert.AreNotEqual(0xFF, sut.Key[0]);
    }

    /// <summary>
    /// Verifies that mutating the original key array after assigning it does not affect the algorithm's internal
    /// copy.
    /// </summary>
    [TestMethod]
    public void Key_WhenOriginalArrayMutatedAfterSetting_ShouldNotAffectInternalKey()
    {
        using TAlgorithm sut = CreateAlgorithm();
        var keySize = Math.Max(1, sut.MaximumKeySize / 16);
        var key = Enumerable.Range(1, keySize).Select(i => (byte)i).ToArray();
        sut.Key = key;

        var snapshot = sut.Key;
        key[0] = 0xFF;

        CollectionAssert.AreEqual(snapshot, sut.Key);
    }

    /// <summary>
    /// Verifies that assigning an empty array to
    /// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> clears the key and reverts the instance to
    /// unkeyed digest mode.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToEmptyArray_ShouldRevertToUnkeyedMode()
    {
        using TAlgorithm sut = CreateAlgorithm();
        sut.Key = new byte[Math.Max(1, sut.MaximumKeySize / 16)];

        sut.Key = [];

        Assert.AreEqual(0, sut.Key.Length);
    }

    /// <summary>
    /// Verifies that assigning <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> after hashing has
    /// begun throws <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetAfterHashingBegins_ShouldThrowExactly()
    {
        using TAlgorithm sut = CreateAlgorithm();
        var input = new byte[32];
        sut.TransformBlock(input, 0, input.Length, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            sut.Key = new byte[Math.Max(1, sut.MaximumKeySize / 16)];
        });
    }

    /// <summary>
    /// Verifies that accessing <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> on a disposed instance
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenGetOnDisposedInstance_ShouldThrowExactly()
    {
        TAlgorithm sut = CreateAlgorithm();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = sut.Key;
        });
    }

    /// <summary>
    /// Verifies that assigning <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> on a disposed instance
    /// throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetOnDisposedInstance_ShouldThrowExactly()
    {
        TAlgorithm sut = CreateAlgorithm();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Key = new byte[Math.Max(1, sut.MaximumKeySize / 16)];
        });
    }
}
