// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.FactoryConstructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class ParallelMerkleTreeHashTests
{
    /// <summary>
    /// Verifies that <see cref="ParallelMerkleTreeHash(IHashAlgorithmFactory{HashAlgorithm}, int, int)" /> succeeds
    /// when a valid <see cref="IHashAlgorithmFactory{T}" /> is provided.
    /// </summary>
    [TestMethod]
    public void Ctor_WithFactoryInterface_WhenValid_ShouldSucceed()
    {
        IHashAlgorithmFactory<HashAlgorithm> factory = HashAlgorithmFactory.From<HashAlgorithm>(SHA256.Create);

        using var hasher = new ParallelMerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Assert.IsNotNull(hasher);
    }

    /// <summary>
    /// Verifies that <see cref="ParallelMerkleTreeHash(IHashAlgorithmFactory{HashAlgorithm}, int, int)" /> throws
    /// <see cref="ArgumentNullException" /> when the factory is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WithFactoryInterface_WhenNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ParallelMerkleTreeHash((IHashAlgorithmFactory<HashAlgorithm>)null!, blockSize: 4, fanOut: 2);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ParallelMerkleTreeHash" /> invokes its factory per node, producing a distinct
    /// algorithm instance for each concurrent leaf and internal-node computation so no algorithm state bleeds
    /// between the parallel workers.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenFactoryInvokedRepeatedly_ShouldReturnDistinctInstances()
    {
        HashAlgorithm? first = null;
        HashAlgorithm? second = null;

        Func<HashAlgorithm> trackingFactory = () =>
        {
            var instance = new MonitoringHashAlgorithm();
            if (first is null) first = instance;
            else if (second is null) second = instance;
            return instance;
        };

        // Two full blocks with fanOut=2 forces at least two leaf-algorithm constructions.
        using var hasher = new ParallelMerkleTreeHash(trackingFactory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MerkleTestData.MakeData(8));

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreNotSame(first, second);
    }
}
