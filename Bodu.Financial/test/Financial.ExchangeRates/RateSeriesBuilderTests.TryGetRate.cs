// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBuilderTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.TryGetRate" /> returns the recorded rate on an exact-date
    /// match.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDateExists_ShouldReturnTrueAndRate()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.TryGetRate" /> returns <see langword="false" /> and a
    /// default rate when no observation exists on the supplied date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDateMissing_ShouldReturnFalseAndDefault()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsFalse(builder.TryGetRate(new DateOnly(2026, 6, 2), out decimal rate));
        Assert.AreEqual(0m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.TryGetRate" /> on an empty builder returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenEmpty_ShouldReturnFalse()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");

        Assert.IsFalse(builder.TryGetRate(new DateOnly(2026, 6, 1), out _));
    }
}
