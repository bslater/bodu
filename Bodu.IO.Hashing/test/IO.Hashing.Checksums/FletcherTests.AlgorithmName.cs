// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.AlgorithmName.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public abstract partial class FletcherTests<TTest, TAlgorithm>
{

    /// <summary>
    /// Verifies that <see cref="Fletcher{T}.AlgorithmName" /> begins with the <c>Fletcher-</c> prefix and ends
    /// with the expected bit width for the concrete variant under test.
    /// </summary>
    [TestMethod]
    public void AlgorithmName_WhenConstructed_ShouldMatchSpecification()
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(DefaultVariant);
        TAlgorithm algorithm = CreateAlgorithm();

        Assert.IsTrue(
            algorithm.AlgorithmName.StartsWith("Fletcher-", StringComparison.Ordinal),
            $"Expected algorithm name to start with 'Fletcher-', was '{algorithm.AlgorithmName}'.");

        if (specification.AlgorithmName is { } expected)
            Assert.AreEqual(expected, algorithm.AlgorithmName);
    }

}