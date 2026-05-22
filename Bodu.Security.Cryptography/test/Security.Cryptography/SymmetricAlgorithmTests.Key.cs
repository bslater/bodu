// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.Key.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the Key property is not null upon algorithm creation.
    /// </summary>
    [TestMethod]
    public void Key_WhenAccessed_ShouldNotBeNull()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.IsNotNull(algorithm.Key);
    }

    /// <summary>
    /// Verifies that accessing <see cref="SymmetricAlgorithm.Key" /> after the algorithm has been disposed throws
    /// an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Key_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.Key;
        });
    }

    /// <summary>
    /// Verifies that setting the <see cref="SymmetricAlgorithm.Key" /> to null throws an ArgumentNullException.
    /// </summary>
    [TestMethod]
    public void Key_WhenSetToNull_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.ThrowsExactly<ArgumentNullException>(() => algorithm.Key = null);
    }

    /// <summary>
    /// Verifies that assigning a key whose length in bits is not among <see cref="SymmetricAlgorithm.LegalKeySizes" />
    /// throws <see cref="CryptographicException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidKeySizeBytesData))]
    public void Key_WhenSetToInvalidSize_ShouldThrowExactly(int keySize)
    {
        if (keySize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();

        var invalidKey = new byte[keySize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            algorithm.Key = invalidKey;
        },
        $"Setting a {keySize}-byte key should throw CryptographicException " +
        $"for {typeof(TAlgorithm).Name}.");
    }

    /// <summary>
    /// Verifies that setting <see cref="SymmetricAlgorithm.Key" /> returns the same hashValue on subsequent get.
    /// </summary>
    [TestMethod]
    public void Key_WhenSet_ShouldReturnSameValueOnGet()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.LegalKeySizes[0].MinSize;
        var key = new byte[size / 8];
        CryptoHelpers.FillWithRandomNonZeroBytes(key);

        algorithm.Key = key;
        CollectionAssert.AreEqual(key, algorithm.Key);
    }

    /// <summary>
    /// Verifies that the Key property returns a defensive copy (not the same reference).
    /// </summary>
    [TestMethod]
    public void Key_WhenSet_ShouldReturnDefensiveCopy()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.LegalKeySizes[0].MinSize;
        var key = new byte[size / 8];
        CryptoHelpers.FillWithRandomNonZeroBytes(key);

        algorithm.Key = key;
        Assert.AreNotSame(key, algorithm.Key);
    }

    /// <summary>
    /// Verifies that modifying the array returned by <see cref="SymmetricAlgorithm.Key" />
    /// does not mutate the algorithm's internal key state.
    /// </summary>
    [TestMethod]
    public void Key_WhenReturnedArrayIsModified_ShouldNotChangeInternalValue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.LegalKeySizes[0].MinSize;
        var expected = Enumerable.Range(1, size / 8).Select(i => (byte)i).ToArray();

        algorithm.KeySize = size;
        algorithm.Key = expected;

        var returned = algorithm.Key;
        returned[0] ^= 0xFF;

        var actual = algorithm.Key;

        CollectionAssert.AreEqual(expected, actual);
        CollectionAssert.AreNotEqual(returned, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.GenerateKey" /> produces a different Key from the previous one.
    /// </summary>
    [TestMethod]
    public void GenerateKey_WhenCalled_ShouldChangeKey()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var initialKey = algorithm.Key;

        algorithm.GenerateKey();
        CollectionAssert.AreNotEqual(initialKey, algorithm.Key);
    }
}
