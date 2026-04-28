// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.TryGetCurrentHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.TryGetCurrentHash(Span{byte}, out int)" /> writes
    /// the digest and reports the number of bytes written when the destination is exactly
    /// <see cref="NonCryptographicHashAlgorithm.HashLengthInBytes" />.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void TryGetCurrentHash_WhenDestinationIsExactSize_ShouldReturnTrueAndWriteHash(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        byte[] expected = algorithm.GetCurrentHash();
        byte[] destination = new byte[algorithm.HashLengthInBytes];

        bool succeeded = algorithm.TryGetCurrentHash(destination, out int written);

        Assert.IsTrue(succeeded);
        Assert.AreEqual(algorithm.HashLengthInBytes, written);
        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.TryGetCurrentHash(Span{byte}, out int)" /> returns
    /// <see langword="false" /> without writing when the destination is smaller than the hash length.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void TryGetCurrentHash_WhenDestinationIsTooSmall_ShouldReturnFalse(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        byte[] destination = new byte[algorithm.HashLengthInBytes - 1];
        if (destination.Length == 0)
        {
            Assert.Inconclusive($"Hash length for variant '{variant}' ({algorithm.HashLengthInBytes})is too small to test undersized destination.");
            return;
        }

        bool succeeded = algorithm.TryGetCurrentHash(destination, out int written);

        Assert.IsFalse(succeeded);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.TryGetCurrentHash(Span{byte}, out int)" /> does
    /// not mutate accumulator state — subsequent calls return the same digest.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void TryGetCurrentHash_WhenSuccessful_ShouldNotMutateAccumulatorState(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.QuickBrownFox);

        byte[] first = new byte[algorithm.HashLengthInBytes];
        Assert.IsTrue(algorithm.TryGetCurrentHash(first, out _));

        byte[] second = algorithm.GetCurrentHash();
        CollectionAssert.AreEqual(first, second);
    }
}
