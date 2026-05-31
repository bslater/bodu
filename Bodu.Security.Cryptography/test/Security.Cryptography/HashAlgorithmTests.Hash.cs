// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.Hash.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that accessing <see cref="HashAlgorithm.Hash" /> after calling <see cref="HashAlgorithm.Initialise" /> without
    /// finalizing the hash computation throws a <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Hash_Get_WhenInitializedAfterTransformBlock_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.TransformBlock(CryptoTestUtilities.ByteSequence256, 0, 256, null, 0);
        algorithm.Initialize();
        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            _ = algorithm.Hash;
        });
    }

    /// <summary>
    /// Verifies that accessing <see cref="HashAlgorithm.Hash" /> without calling
    /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> throws a
    /// <see cref="CryptographicUnexpectedOperationException" />, as the hash is not finalized.
    /// </summary>
    /// <param name="offset">The starting position in the input buffer.</param>
    /// <param name="count">The number of bytes to process.</param>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 100)]
    [DataRow(10, 10)]
    public void Hash_Get_WhenTransformFinalBlockNotCalled_ShouldThrowExactly(int offset, int count)
    {
        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.TransformBlock(CryptoTestUtilities.ByteSequence256, offset, count, null, 0);
        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            _ = algorithm.Hash;
        });
    }

    /// <summary>
    /// Verifies that repeated reads of <see cref="HashAlgorithm.Hash" /> after
    /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> return the same final hash value.
    /// </summary>
    [TestMethod]
    public void Hash_Get_WhenCalledRepeatedlyAfterTransformFinalBlock_ShouldReturnSameHash()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        _ = algorithm.TransformFinalBlock(CryptoTestUtilities.ByteSequence256, 0, 256);

        var first = algorithm.Hash!;
        var second = algorithm.Hash!;

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[]?, int)" />
    /// with a zero-length input does not finalise the hash computation.
    /// </summary>
    [TestMethod]
    public void Hash_Get_WhenOnlyZeroLengthTransformBlockCalled_ShouldThrowExactly()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        _ = algorithm.TransformBlock(Array.Empty<byte>(), 0, 0, null, 0);

        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            _ = algorithm.Hash;
        });
    }
}
