// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    /// <summary>
    /// Verifies that <see cref="Tiger.AlgorithmName" />, when UsingVariant, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(TigerVariant variant)
    {
        using Tiger algorithm = CreateAlgorithm(variant);

        Assert.AreEqual($"Tiger/{algorithm.HashSize}", algorithm.AlgorithmName);
    }
}
