// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CubeHashTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CubeHashTests
{
    /// <summary>
    /// Verifies that <see cref="CubeHash.AlgorithmName" />, when UsingVariant, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(CubeHashVariants variant)
    {
        using CubeHash algorithm = CreateAlgorithm(variant);
        string expected = GetAlgorithmName(algorithm);

        Assert.AreEqual(expected, algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="CubeHash.AlgorithmName" />, when UsingCustomRounds, returns the expected value.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenUsingCustomRounds_ShouldReturnCorrectlyFormattedString()
    {
        using var algorithm = new CubeHash
        {
            InitializationRounds = 3,
            Rounds = 5
        };
        string expected = GetAlgorithmName(algorithm);

        Assert.AreEqual(expected, algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that <see cref="CubeHash.AlgorithmName" /> returns the documented format
    /// <c>CubeHash{r}+{b}/{w}+{f}-{h}</c> using the algorithm's current property values.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenReadAfterCustomConfiguration_ShouldFormatAllParameters()
    {
        using var algorithm = new CubeHash
        {
            InitializationRounds = 16,
            Rounds = 16,
            TransformBlockSize = 32,
            FinalizationRounds = 32,
            HashSize = 256,
        };

        Assert.AreEqual("CubeHash16+16/32+32-256", algorithm.AlgorithmName);
    }

    private static string GetAlgorithmName(CubeHash algorithm) =>
        $"CubeHash{algorithm.InitializationRounds}+{algorithm.Rounds}/{algorithm.TransformBlockSize}+{algorithm.FinalizationRounds}-{algorithm.HashSize}";
}
