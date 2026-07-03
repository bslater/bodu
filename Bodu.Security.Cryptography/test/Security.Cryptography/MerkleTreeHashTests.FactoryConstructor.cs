// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTests.FactoryConstructor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class MerkleTreeHashTests
{
    /// <summary>
    /// Verifies that <see cref="MerkleTreeHash(IHashAlgorithmFactory{HashAlgorithm}, int, int)" /> succeeds when a
    /// valid <see cref="IHashAlgorithmFactory{T}" /> is provided.
    /// </summary>
    [TestMethod]
    public void Ctor_WithFactoryInterface_WhenValid_ShouldSucceed()
    {
        IHashAlgorithmFactory<HashAlgorithm> factory = HashAlgorithmFactory.From<HashAlgorithm>(SHA256.Create);

        using var hasher = new MerkleTreeHash(factory, blockSize: 4, fanOut: 2);

        Assert.IsNotNull(hasher);
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeHash(IHashAlgorithmFactory{HashAlgorithm}, int, int)" /> throws
    /// <see cref="ArgumentNullException" /> when the factory is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WithFactoryInterface_WhenNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new MerkleTreeHash((IHashAlgorithmFactory<HashAlgorithm>)null!, blockSize: 4, fanOut: 2);
        });
    }

    /// <summary>
    /// Verifies that the sequential <see cref="MerkleTreeHash" /> invokes its factory exactly once and reuses that
    /// single <see cref="HashAlgorithm" /> across every leaf and internal node, rather than creating a distinct
    /// instance per node. The one-shot hashing path resets the algorithm between nodes, so no state bleeds.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenTreeHasManyNodes_ShouldReuseSingleFactoryInstance()
    {
        int factoryCalls = 0;
        Func<HashAlgorithm> countingFactory = () =>
        {
            factoryCalls++;
            return new MonitoringHashAlgorithm();
        };

        // Three full blocks with fanOut=2 build three leaves and three internal nodes — six nodes in all.
        using var hasher = new MerkleTreeHash(countingFactory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MerkleTestData.MakeData(12));

        Assert.AreEqual(1, factoryCalls,
            "MerkleTreeHash must reuse a single hash-algorithm instance across all nodes (one factory call).");
    }
}
