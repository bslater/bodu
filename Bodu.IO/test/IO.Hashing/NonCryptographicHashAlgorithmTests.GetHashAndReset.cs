// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.GetHashAndReset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" /> returns the same digest that
    /// <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> would have produced at the same point.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetHashAndReset_AfterAppend_ShouldReturnFinalDigest(TVariant variant)
    {
        NonCryptographicHashAlgorithm snapshot = CreateAlgorithm(variant);
        snapshot.Append(SharedInputs["ABC"]);
        byte[] expected = snapshot.GetCurrentHash();

        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(SharedInputs["ABC"]);

        byte[] actual = algorithm.GetHashAndReset();

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetHashAndReset()" /> leaves the algorithm in the
    /// same state as a freshly constructed instance.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetHashAndReset_AfterAppend_ShouldResetInstance(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(SharedInputs["QuickBrownFox"]);
        _ = algorithm.GetHashAndReset();

        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);

        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetHashAndReset(Span{byte})" /> writes the digest
    /// into the destination, returns the number of bytes written, and resets the instance state.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetHashAndReset_WhenWritingToSpan_ShouldReturnHashAndResetInstance(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(SharedInputs["Sequential_0_255"]);

        byte[] destination = new byte[algorithm.HashLengthInBytes];
        int written = algorithm.GetHashAndReset(destination);

        Assert.AreEqual(algorithm.HashLengthInBytes, written);

        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);
        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }
}
