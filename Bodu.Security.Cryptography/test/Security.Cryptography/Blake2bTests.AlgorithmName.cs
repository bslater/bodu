// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Blake2bTests
{
    /// <summary>
    /// Verifies that <see cref="Blake2b.AlgorithmName" /> returns a string formatted as
    /// <c>"BLAKE2b-<i>n</i>"</c>, where <i>n</i> is the configured digest size in bits.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(Blake2bVariant variant)
    {
        using Blake2b algorithm = CreateAlgorithm(variant);

        Assert.AreEqual($"BLAKE2b-{algorithm.HashSize}", algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that setting <see cref="Blake2b.HashSize" /> to a supported value on a fresh instance succeeds
    /// and updates <see cref="System.Security.Cryptography.HashAlgorithm.HashSize" /> accordingly.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetOnFreshInstanceToSupportedValue_ShouldUpdateHashSize()
    {
        using var algorithm = new Blake2b();

        algorithm.HashSize = 256;

        Assert.AreEqual(256, algorithm.HashSize);
    }
}
