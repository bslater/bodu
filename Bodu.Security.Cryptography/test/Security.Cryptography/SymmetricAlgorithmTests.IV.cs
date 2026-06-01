// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmTests.IV.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class SymmetricAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the IV property is not null upon algorithm creation.
    /// </summary>
    [TestMethod]
    public void IV_WhenAccessed_ShouldNotBeNull()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.IsNotNull(algorithm.IV);
    }

    /// <summary>
    /// Verifies that accessing <see cref="SymmetricAlgorithm.IV" /> after the algorithm has been disposed throws
    /// an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void IV_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.IV;
        });
    }

    /// <summary>
    /// Verifies that setting the <see cref="SymmetricAlgorithm.IV" /> to null throws an ArgumentNullException.
    /// </summary>
    [TestMethod]
    public void IV_WhenSetToNull_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        Assert.ThrowsExactly<ArgumentNullException>(() => algorithm.IV = null!);
    }

    /// <summary>
    /// Verifies that setting an invalid IV size throws a CryptographicException.
    /// </summary>
    [TestMethod]
    public void IV_WhenSetToInvalidSize_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var invalidIV = new byte[algorithm.BlockSize - 1];
        Assert.ThrowsExactly<CryptographicException>(() => algorithm.IV = invalidIV);
    }

    /// <summary>
    /// Verifies that setting <see cref="SymmetricAlgorithm.IV" /> returns the same hashValue on subsequent get.
    /// </summary>
    [TestMethod]
    public void IV_WhenSet_ShouldReturnSameValueOnGet()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var iv = new byte[algorithm.BlockSize / 8];
        CryptographyHelper.FillWithRandomNonZeroBytes(iv);

        algorithm.IV = iv;
        CollectionAssert.AreEqual(iv, algorithm.IV);
    }

    /// <summary>
    /// Verifies that the IV property returns a defensive copy (not the same reference).
    /// </summary>
    [TestMethod]
    public void IV_WhenSet_ShouldReturnDefensiveCopy()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var iv = new byte[algorithm.BlockSize / 8];
        CryptographyHelper.FillWithRandomNonZeroBytes(iv);

        algorithm.IV = iv;
        Assert.AreNotSame(iv, algorithm.IV);
    }

    /// <summary>
    /// Verifies that modifying the array returned by <see cref="SymmetricAlgorithm.IV" />
    /// does not mutate the algorithm's internal IV state.
    /// </summary>
    [TestMethod]
    public void IV_WhenReturnedArrayIsModified_ShouldNotChangeInternalValue()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var size = algorithm.BlockSize;
        var expected = Enumerable.Range(1, size / 8).Select(i => (byte)i).ToArray();

        algorithm.IV = expected;

        var returned = algorithm.IV;
        returned[0] ^= 0xFF;

        var actual = algorithm.IV;

        CollectionAssert.AreEqual(expected, actual);
        CollectionAssert.AreNotEqual(returned, actual);
    }

    /// <summary>
    /// Verifies that <see cref="SymmetricAlgorithm.GenerateIV" /> produces a different IV from the previous one.
    /// </summary>
    [TestMethod]
    public void GenerateIV_WhenCalled_ShouldChangeIV()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        var initialIV = algorithm.IV;

        algorithm.GenerateIV();
        CollectionAssert.AreNotEqual(initialIV, algorithm.IV);
    }

    /// <summary>
    /// Verifies that creating an encryptor with a wrong-length IV throws
    /// <see cref="CryptographicException" /> whose message reports the offending IV bit-length
    /// rather than an unrelated value (e.g. the key length). Regression guard for a copy-paste
    /// in <see cref="Threefish" />'s validation diagnostics.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvalidBlockSizeBytesData))]
    public void CreateEncryptor_WithInvalidIvLength_ShouldThrowExactly(int blockSize)
    {
        if (blockSize < 0) return;

        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.GenerateKey();

        var blockSizeBytes = algorithm.BlockSize / 8;
        var badIv = new byte[blockSize];

        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            using ICryptoTransform _ = algorithm.CreateEncryptor(algorithm.Key, badIv);
        });
    }

}
