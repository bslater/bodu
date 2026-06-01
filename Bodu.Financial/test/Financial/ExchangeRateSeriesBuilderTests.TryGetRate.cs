// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilderTests.TryGetRate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TryGetRate" /> returns the recorded rate on an exact-date
    /// match.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDateExists_ShouldReturnTrueAndRate()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out var rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TryGetRate" /> returns <see langword="false" /> and a
    /// default rate when no observation exists on the supplied date.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenDateMissing_ShouldReturnFalseAndDefault()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsFalse(builder.TryGetRate(new DateOnly(2026, 6, 2), out var rate));
        Assert.AreEqual(0m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TryGetRate" /> on an empty builder returns
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenEmpty_ShouldReturnFalse()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        Assert.IsFalse(builder.TryGetRate(new DateOnly(2026, 6, 1), out _));
    }
}
