// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.GetCurrentHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
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
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.Abc);

        var first = algorithm.GetCurrentHash();
        var second = algorithm.GetCurrentHash();
        var third = algorithm.GetCurrentHash();

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
        TAlgorithm algorithm = CreateAlgorithm(variant);
        algorithm.Append(NonCryptographicHashSharedInputs.QuickBrownFox);

        var expected = algorithm.GetCurrentHash();

        var destination = new byte[algorithm.HashLengthInBytes];
        var written = algorithm.GetCurrentHash(destination);

        Assert.AreEqual(algorithm.HashLengthInBytes, written);
        CollectionAssert.AreEqual(expected, destination);
    }

    /// <summary>
    /// Verifies that two inputs that span multiple input chunks produce different hashes.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetCurrentHash_ForMultiChunkInputs_ShouldProduceDistinctHashes(TVariant variant)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        TAlgorithm algorithm = CreateAlgorithm(variant);

        var bufferSize = (specification.IncrementalCoverageBytes ?? specification.HashLengthInBytes) * 4;
        var inputA = TestHelpers.GenerateRandomNonZeroBytes(bufferSize);
        var inputB = inputA.Copy()!;
        inputB[bufferSize - 2] = 0x00;

        algorithm.Append(inputA);
        var hashA = algorithm.GetHashAndReset();

        algorithm.Append(inputB);
        var hashB = algorithm.GetHashAndReset();

        CollectionAssert.AreNotEqual(hashA, hashB,
            "Distinct multi-chunk inputs must produce different hashes.");
    }
}
