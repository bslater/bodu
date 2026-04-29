// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.GetCurrentHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> returns the expected
    /// hash value at each stage of true incremental hashing as bytes from <c>0x00</c> onwards are
    /// appended in sequence.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <returns>A task that completes when all incremental stages have been verified.</returns>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public Task GetCurrentHash_WhenUsingIncrementalInput_ShouldMatchExpected(TVariant variant) =>
        AssertIncrementalCurrentHashAsync(variant,
            (algorithm, source) =>
            {
                algorithm.Append(source);
                return Task.FromResult(algorithm.GetCurrentHash());
            });

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> is non-destructive and
    /// returns the same digest when invoked repeatedly against the same accumulator state.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetCurrentHash_WhenCalledRepeatedly_ShouldReturnSameDigest(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        byte[] first = algorithm.GetCurrentHash();
        byte[] second = algorithm.GetCurrentHash();
        byte[] third = algorithm.GetCurrentHash();

        CollectionAssert.AreEqual(first, second);
        CollectionAssert.AreEqual(second, third);
    }

    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash(Span{byte})" /> writes the digest
    /// into a caller-supplied buffer and returns the number of bytes written.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetCurrentHash_WhenWritingToSpan_ShouldMatchArrayOverload(TVariant variant)
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.QuickBrownFox);

        byte[] expected = algorithm.GetCurrentHash();

        byte[] destination = new byte[algorithm.HashLengthInBytes];
        int written = algorithm.GetCurrentHash(destination);

        Assert.AreEqual(algorithm.HashLengthInBytes, written);
        CollectionAssert.AreEqual(expected, destination);
    }
}
