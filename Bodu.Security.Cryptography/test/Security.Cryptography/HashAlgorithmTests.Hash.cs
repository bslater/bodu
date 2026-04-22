// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.Hash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that accessing <see cref="HashAlgorithm.Hash" /> after calling <see cref="HashAlgorithm.Initialize" /> without
    /// finalizing the hash computation throws a <see cref="CryptographicUnexpectedOperationException" />.
    /// </summary>
    [TestMethod]
    public void Hash_Get_WhenInitializedAfterTransformBlock_ShouldThrowExactly()
    {
        using var algorithm = CreateAlgorithm();
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
        using var algorithm = CreateAlgorithm();
        algorithm.TransformBlock(CryptoTestUtilities.ByteSequence256, offset, count, null, 0);
        Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
        {
            _ = algorithm.Hash;
        });
    }
}
