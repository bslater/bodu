// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests{T,T,T}.GetHashAndReset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" /> leaves the algorithm in the
    /// same state as a freshly constructed instance.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void GetHashAndReset_AfterAppend_ShouldResetInstance(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.QuickBrownFox);
        _ = algorithm.GetHashAndReset();

        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);

        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" /> returns the same digest that
    /// <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> would have produced at the same point.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void GetHashAndReset_AfterAppend_ShouldReturnFinalDigest(TVariant variant)
    {
        NonCryptographicHashAlgorithm snapshot = CreateAlgorithm(variant);
        snapshot.Append(NonCryptographicHashSharedInputs.Abc);
        byte[] expected = snapshot.GetCurrentHash();

        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        byte[] actual = algorithm.GetHashAndReset();

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the synchronous append-and-finalise path produces the correct hash value for
    /// incrementally growing inputs from empty input through one byte past the spec's coverage threshold.
    /// </summary>
    /// <param name="variant">The variant identifier supplied by the dynamic data source.</param>
    /// <returns>A task that completes when all incremental lengths have been verified.</returns>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public Task GetHashAndReset_WhenUsingIncrementalInput_ShouldMatchExpected(TVariant variant) =>
        AssertIncrementalInputAsync(variant,
            (algorithm, input, byteCount) =>
            {
                algorithm.Reset();
                algorithm.Append(input.AsSpan(0, byteCount));
                return Task.FromResult(algorithm.GetHashAndReset());
            });

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetHashAndReset(Span{byte})" /> writes the digest
    /// into the destination, returns the number of bytes written, and resets the instance state.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void GetHashAndReset_WhenWritingToSpan_ShouldReturnHashAndResetInstance(TVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Sequential0To255);

        byte[] destination = new byte[algorithm.HashLengthInBytes];
        int written = algorithm.GetHashAndReset(destination);

        Assert.AreEqual(algorithm.HashLengthInBytes, written);

        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);
        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

}
