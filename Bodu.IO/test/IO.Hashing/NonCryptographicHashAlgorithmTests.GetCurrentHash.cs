// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.GetCurrentHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using Bodu.Test;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> returns the expected hash
    /// value at each stage of incremental hashing as bytes from <c>0x00</c> onwards are appended in sequence.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <remarks>
    /// Entry <c>i</c> in the expected sequence corresponds to the digest after <c>i</c> bytes have been
    /// appended (so entry <c>0</c> is the empty-input hash). Variants that supply an empty expected sequence
    /// are reported as inconclusive for this particular check.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetCurrentHash_WhenAppendingIncrementalData_ShouldReturnExpectedHashValueAtEachStep(TVariant variant)
    {
        string[] expectedValues = GetIncrementalHashValue(variant).ToArray();
        if (expectedValues.Length == 0)
        {
            Assert.Inconclusive($"No incremental hash values defined for variant '{variant}'.");
            return;
        }

        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm(variant);

        for (int i = 0; i < expectedValues.Length; i++)
        {
            byte[] expected = Convert.FromHexString(expectedValues[i]);
            byte[] actual = algorithm.GetCurrentHash();
            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(
                expected,
                actual,
                $"Hash mismatch for {typeof(TAlgorithm).Name} variant '{variant}' at incremental length {i}.");

            if (i < expectedValues.Length - 1)
                algorithm.Append(new[] { (byte)i });
        }
    }

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
