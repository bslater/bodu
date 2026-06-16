// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkeinTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
}
