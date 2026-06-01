// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Blake2sTests
{
    /// <summary>
    /// Verifies that <see cref="Blake2s.AlgorithmName" /> returns a string formatted as
    /// <c>"BLAKE2s-<i>n</i>"</c>, where <i>n</i> is the configured digest size in bits.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants), DynamicDataDisplayName = nameof(HashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(HashAlgorithmVariantDisplayName))]
    public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(Blake2sVariant variant)
    {
        using Blake2s algorithm = CreateAlgorithm(variant);

        Assert.AreEqual($"BLAKE2s-{algorithm.HashSize}", algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that setting <see cref="Blake2s.HashSize" /> to a supported value on a fresh instance succeeds
    /// and updates <see cref="System.Security.Cryptography.HashAlgorithm.HashSize" /> accordingly.
    /// </summary>
    [TestMethod]
    public void HashSize_WhenSetOnFreshInstanceToSupportedValue_ShouldUpdateHashSize()
    {
        using var algorithm = new Blake2s();

        algorithm.HashSize = 128;

        Assert.AreEqual(128, algorithm.HashSize);
    }
}
