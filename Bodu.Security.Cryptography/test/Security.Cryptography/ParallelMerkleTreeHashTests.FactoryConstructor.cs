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
}
