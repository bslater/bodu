// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHashTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SipHashTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that <see cref="SipHash.AlgorithmName" />, when UsingVariant, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(HashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(HashAlgorithmVariantDisplayName))]
    public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(SipHashVariant variant)
    {
        using TAlgorithm algorithm = CreateAlgorithm(variant);
        var expected = GetAlgorithmName(algorithm);

        Assert.AreEqual(expected, algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="SipHash.AlgorithmName" />, when UsingCustomRounds, returns the expected value.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenUsingCustomRounds_ShouldReturnCorrectlyFormattedString()
    {
        using var algorithm = new TAlgorithm
        {
            CompressionRounds = 3,
            FinalizationRounds = 5
        };
        var expected = GetAlgorithmName(algorithm);

        Assert.AreEqual(expected, algorithm.AlgorithmName);
    }

    private static string GetAlgorithmName(SipHash<TAlgorithm> algorithm) =>
        $"SipHash-{algorithm.CompressionRounds}-{algorithm.FinalizationRounds}-{algorithm.HashSize}";
}
