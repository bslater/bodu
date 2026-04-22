// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.AlgorithmName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

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