// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public abstract partial class FnvTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="Fnv{T}.AlgorithmName" /> returns a string of the form
    /// <c>FNV-{variant}-{bits}</c> matching the variant under test.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(SingleTestVariant variant)
    {
        TAlgorithm algorithm = CreateAlgorithm(variant);
        var typeName = algorithm.GetType().Name;

        var expectedVariant = typeName.Contains("1a", StringComparison.OrdinalIgnoreCase) ? "1a" : "1";
        var bits = algorithm.HashLengthInBytes * 8;
        var expected = $"FNV-{expectedVariant}-{bits}";

        Assert.AreEqual(expected, algorithm.AlgorithmName);
    }
}
