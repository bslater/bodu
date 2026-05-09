// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    /// <summary>
    /// Verifies that <see cref="Tiger.AlgorithmName" />, when UsingVariant, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(HashAlgorithmVariants))]
    public void AlgorithmName_WhenUsingVariant_ShouldReturnCorrectlyFormattedString(TigerVariant variant)
    {
        using var algorithm = CreateAlgorithm(variant);

        Assert.AreEqual($"Tiger/{algorithm.HashSize}", algorithm.AlgorithmName);
    }

    /// <summary>
    /// Verifies that reading <see cref="Tiger.AlgorithmName" /> after <see cref="HashAlgorithm.Dispose()" />
    /// throws <see cref="ObjectDisposedException" /> and not a generic <see cref="NullReferenceException" />.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.AlgorithmName;
        });
    }
}
