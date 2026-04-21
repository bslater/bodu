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
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> returns the expected hash value at each
    /// stage of incremental hashing as bytes from <c>0x00</c> through <c>0xFF</c> are appended in sequence.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void GetCurrentHash_WhenAppendingIncrementalData_ShouldReturnExpectedHashValueAtEachStep(TVariant variant)
    {
        using NonCryptographicHashAlgorithm algorithm = CreateHashAlgorithm(variant);

        string[] expectedValues = GetIncrementalHashValue(variant).ToArray();

        Assert.AreEqual(
            257,
            expectedValues.Length,
            $"{typeof(TAlgorithm).Name} variant '{variant}' must provide 257 expected values: one for empty input and 256 for appended byte values 0x00 through 0xFF.");

        for (int i = 0; i < 256; i++)
        {
            byte[] expected = Convert.FromHexString(expectedValues[i]);

            algorithm.Append([(byte)i]);
            var actual = algorithm.GetCurrentHash();
            TestHelpers.TraceWriteIfNotEqual(expected, actual);

            CollectionAssert.AreEqual(expected, actual,
                $"Hash mismatch for {typeof(TAlgorithm).Name} variant '{variant}' at incremental length {i}.");
        }
    }
}