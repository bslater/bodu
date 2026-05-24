// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.TryGetHashAndReset.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.TryGetHashAndReset(Span{byte}, out int)" /> writes
    /// the digest into a correctly sized destination and reports the number of bytes written.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void TryGetHashAndReset_WhenDestinationIsExactSize_ShouldReturnTrueAndWriteHash(TVariant variant)
    {
        NonCryptographicHashAlgorithm snapshot = CreateAlgorithm(variant);
        snapshot.Append(NonCryptographicHashSharedInputs.Abc);
        var expected = snapshot.GetCurrentHash();

        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        var destination = new byte[algorithm.HashLengthInBytes];
        var succeeded = algorithm.TryGetHashAndReset(destination, out var written);

        Assert.IsTrue(succeeded);
        Assert.AreEqual(algorithm.HashLengthInBytes, written);
        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.TryGetHashAndReset(Span{byte}, out int)" />
    /// returns <see langword="false" /> and leaves the accumulator untouched when the destination is too small.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void TryGetHashAndReset_WhenDestinationIsTooSmall_ShouldReturnFalseAndPreserveState(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);
        var before = algorithm.GetCurrentHash();

        var destination = new byte[algorithm.HashLengthInBytes - 1];
        if (destination.Length == 0)
        {
            Assert.Inconclusive($"Hash length for variant '{variant}' ({algorithm.HashLengthInBytes})is too small to test undersized destination.");
            return;
        }

        var succeeded = algorithm.TryGetHashAndReset(destination, out var written);

        Assert.IsFalse(succeeded);
        Assert.AreEqual(0, written);
        CollectionAssert.AreEqual(before, algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that a successful
    /// <see cref="NonCryptographicHashAlgorithm.TryGetHashAndReset(Span{byte}, out int)" /> leaves the instance
    /// reset to initial state.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void TryGetHashAndReset_WhenSuccessful_ShouldResetInstance(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Sequential0To255);

        var destination = new byte[algorithm.HashLengthInBytes];
        Assert.IsTrue(algorithm.TryGetHashAndReset(destination, out _));

        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);
        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

}
