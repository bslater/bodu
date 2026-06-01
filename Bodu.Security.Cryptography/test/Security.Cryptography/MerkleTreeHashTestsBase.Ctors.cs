// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTestsBase.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

public abstract partial class MerkleTreeHashTestsBase<THasher>
{
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Constructor validation
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that a <see langword="null" /> algorithm factory raises
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAlgorithmFactoryIsNull_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentNullException>(() => Construct(null));

    /// <summary>
    /// Verifies that a non-positive block size raises <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenBlockSizeIsNonPositive_ShouldThrowExactly(int blockSize)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Construct(Factory, blockSize: blockSize));
    }

    /// <summary>
    /// Verifies that a fan-out below 2 raises <see cref="ArgumentOutOfRangeException" /> —
    /// Merkle trees require at least binary fan-out to be meaningful.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void Ctor_WhenFanOutIsBelowTwo_ShouldThrowExactly(int fanOut)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            Construct(Factory, fanOut: fanOut));
    }

    /// <summary>
    /// Verifies that construction with a valid factory and default parameters succeeds.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValidFactoryProvided_ShouldSucceed()
    {
        using THasher hasher = Construct(Factory);
        Assert.IsNotNull(hasher);
    }

    /// <summary>
    /// Verifies that construction with a range of valid explicit parameters succeeds.
    /// </summary>
    [TestMethod]
    [DataRow(1, 2)]
    [DataRow(4, 2)]
    [DataRow(4, 3)]
    [DataRow(1024, 4)]
    public void Ctor_WhenValidParametersProvided_ShouldSucceed(int blockSize, int fanOut)
    {
        using THasher hasher = Construct(Factory, blockSize, fanOut);
        Assert.IsNotNull(hasher);
    }

    /// <summary>
    /// Verifies that each factory invocation produces a distinct algorithm instance so no
    /// algorithm state bleeds between leaf computations.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenFactoryInvokedRepeatedly_ShouldReturnDistinctInstances()
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
        using THasher hasher = Construct(trackingFactory, blockSize: 4, fanOut: 2);
        ComputeHash(hasher, MakeData(8));

        Assert.IsNotNull(first);
        Assert.IsNotNull(second);
        Assert.AreNotSame(first, second);
    }
}
