// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public abstract partial class SkeinTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="Skein{T}.AlgorithmName" /> returns the documented
    /// <c>"Skein-{state}-{hashSize}"</c> format using the algorithm's current configuration.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenReadOnDefaultInstance_ShouldFollowSkeinNamingConvention()
    {
        using var skein = new TAlgorithm();
        string name = skein.AlgorithmName;

        Assert.IsTrue(name.StartsWith("Skein-", StringComparison.Ordinal),
            $"Expected Skein algorithm name to start with 'Skein-', got '{name}'.");
        Assert.IsTrue(name.EndsWith("-" + skein.HashSize, StringComparison.Ordinal),
            $"Expected Skein algorithm name to end with '-{skein.HashSize}', got '{name}'.");
    }

    /// <summary>
    /// Verifies that reading <see cref="Skein{T}.AlgorithmName" /> after disposal throws
    /// <see cref="ObjectDisposedException" /> rather than returning a stale or partially-zeroed
    /// string.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenAccessedAfterDispose_ShouldThrowExactly()
    {
        var skein = new TAlgorithm();
        skein.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = skein.AlgorithmName;
        });
    }
}
