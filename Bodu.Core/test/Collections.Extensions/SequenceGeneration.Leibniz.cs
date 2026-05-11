// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGeneration.Leibniz.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

[TestClass]
public class LeibnizTests
{
    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Leibniz" /> throws <see cref="ArgumentOutOfRangeException" /> when min is negative.
    /// </summary>
    [TestMethod]
    public void Leibniz_WhenMinIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = SequenceGenerator.Leibniz(-0.1, 1.0).ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Leibniz" /> throws <see cref="ArgumentOutOfRangeException" /> when max is negative.
    /// </summary>
    [TestMethod]
    public void Leibniz_WhenMaxIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = SequenceGenerator.Leibniz(0.0, -1.0).ToList();
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Leibniz" /> throws <see cref="ArgumentException" /> when min is greater than max.
    /// </summary>
    [TestMethod]
    public void Leibniz_WhenMinIsGreaterThanMax_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = SequenceGenerator.Leibniz(0.9, 0.1).ToList();
        });
    }

    /// <summary>
    /// Verifies that the first three terms of the Leibniz series are 1, -1/3 and 1/5 with the natural alternating sign pattern.
    /// </summary>
    [TestMethod]
    public void Leibniz_WhenWindowCoversFirstTerms_ShouldReturnExpectedAlternatingSigns()
    {
        var actual = SequenceGenerator.Leibniz(0.0, 1.1).Take(3).ToArray();

        Assert.AreEqual(3, actual.Length);
        Assert.AreEqual(1.0, actual[0], 1e-12);
        Assert.AreEqual(-1.0 / 3.0, actual[1], 1e-12);
        Assert.AreEqual(1.0 / 5.0, actual[2], 1e-12);
    }

    /// <summary>
    /// Verifies that the magnitudes of the emitted terms always sit within the [min, max) window.
    /// </summary>
    [TestMethod]
    public void Leibniz_WhenWindowExcludesLargeTerms_ShouldSkipThemAndYieldOnlySmallerMagnitudes()
    {
        const double min = 0.0;
        const double max = 0.3;

        var actual = SequenceGenerator.Leibniz(min, max).Take(5).ToArray();

        Assert.IsTrue(actual.Length > 0, "Expected at least one term within the window.");
        foreach (double term in actual)
        {
            double magnitude = Math.Abs(term);
            Assert.IsTrue(magnitude >= min, $"Magnitude {magnitude} fell below the inclusive lower bound {min}.");
            Assert.IsTrue(magnitude < max, $"Magnitude {magnitude} reached or exceeded the exclusive upper bound {max}.");
        }
    }

    /// <summary>
    /// Verifies that approximating π using the Leibniz partial sums converges to the expected approximate value.
    /// </summary>
    [TestMethod]
    public void Leibniz_WhenSummingManyTerms_ShouldApproximatePiOverFour()
    {
        double partial = 0;
        foreach (double term in SequenceGenerator.Leibniz(0.0, 2.0).Take(2000))
            partial += term;

        double pi = partial * 4;
        Assert.AreEqual(Math.PI, pi, 0.01);
    }

    /// <summary>
    /// Verifies that <see cref="SequenceGenerator.Leibniz" /> stops yielding once a term's magnitude reaches the exclusive upper bound.
    /// </summary>
    [TestMethod]
    public void Leibniz_WhenFirstTermExceedsMax_ShouldReturnEmptySequence()
    {
        var actual = SequenceGenerator.Leibniz(0.0, 0.9).Take(1).ToArray();

        Assert.AreEqual(0, actual.Length);
    }
}
