// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.TryGetHashAndReset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        snapshot.Append(SharedInputs["ABC"]);
        byte[] expected = snapshot.GetCurrentHash();

        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(SharedInputs["ABC"]);

        byte[] destination = new byte[algorithm.HashLengthInBytes];
        bool succeeded = algorithm.TryGetHashAndReset(destination, out int written);

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
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(SharedInputs["ABC"]);
        byte[] before = algorithm.GetCurrentHash();

        byte[] destination = new byte[algorithm.HashLengthInBytes - 1];
        if (destination.Length == 0)
        {
            Assert.Inconclusive($"Hash length for variant '{variant}' is too small to test undersized destination.");
            return;
        }

        bool succeeded = algorithm.TryGetHashAndReset(destination, out int written);

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
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(SharedInputs["Sequential_0_255"]);

        byte[] destination = new byte[algorithm.HashLengthInBytes];
        Assert.IsTrue(algorithm.TryGetHashAndReset(destination, out _));

        NonCryptographicHashAlgorithm baseline = CreateAlgorithm(variant);
        CollectionAssert.AreEqual(baseline.GetCurrentHash(), algorithm.GetCurrentHash());
    }
}
