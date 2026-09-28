// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaEngineTests.Decompose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class MLDsaEngineTests
{
    /// <summary>
    /// Verifies that the multiply-and-shift decomposition matches the division-based reference for both values of γ₂ at
    /// the ends of [0, q), around every multiple of 2γ₂, and at seeded coefficients.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [DataRow(WideGamma2)]
    [DataRow(NarrowGamma2)]
    public void Decompose_WhenCoefficientIsInRange_ShouldMatchTheReference(int gamma2)
    {
        var random = new Random(0x0204_0006 + gamma2);
        IEnumerable<int> multiples = Enumerable.Range(0, (Q / (2 * gamma2)) + 1)
            .SelectMany(m => new[] { (2 * gamma2 * m) - 1, 2 * gamma2 * m, (2 * gamma2 * m) + 1, (2 * gamma2 * m) + gamma2, (2 * gamma2 * m) + gamma2 + 1 });
        IEnumerable<int> values = new[] { 0, 1, Q - 2, Q - 1 }
            .Concat(multiples.Where(value => value >= 0 && value < Q))
            .Concat(Enumerable.Range(0, 100_000).Select(_ => random.Next(Q)));

        foreach (int value in values)
            AssertDecomposeMatches(gamma2, value);
    }

    /// <summary>
    /// Verifies that the multiply-and-shift decomposition matches the division-based reference for both values of γ₂ at
    /// every coefficient in [0, q).
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow(WideGamma2)]
    [DataRow(NarrowGamma2)]
    public void Decompose_WhenEveryCoefficientIsTried_ShouldMatchTheReference(int gamma2)
    {
        for (int value = 0; value < Q; value++)
            AssertDecomposeMatches(gamma2, value);
    }

    /// <summary>
    /// Asserts that the engine's decomposition of a coefficient equals the reference's.
    /// </summary>
    /// <param name="gamma2">The parameter γ₂.</param>
    /// <param name="value">The coefficient in [0, q).</param>
    private static void AssertDecomposeMatches(int gamma2, int value)
    {
        MLDsaReference.Decompose(gamma2, value, out int expectedHigh, out int expectedLow);
        MLDsaEngine.Decompose(gamma2, value, out int actualHigh, out int actualLow);

        if (expectedHigh != actualHigh || expectedLow != actualLow)
            Assert.Fail($"Decompose({gamma2}, {value}) = ({actualHigh}, {actualLow}), not ({expectedHigh}, {expectedLow}).");
    }
}
