// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilderTests.TryGetSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateTableBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.TryGetSeries" /> returns <see langword="false" /> when the series
    /// is unknown.
    /// </summary>
    [TestMethod]
    public void TryGetSeries_WhenSeriesMissing_ShouldReturnFalse()
    {
        RateTableBuilder table = new();

        bool found = table.TryGetSeries(s_usdAud, "RBA", out RateSeries? series);

        Assert.IsFalse(found);
        Assert.IsNull(series);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.TryGetSeries" /> returns <see langword="false" /> when the
    /// builder for the key is empty, because immutable series must be non-empty.
    /// </summary>
    [TestMethod]
    public void TryGetSeries_WhenBuilderEmpty_ShouldReturnFalse()
    {
        RateTableBuilder table = new();
        table.GetOrAddSeries(s_usdAud, "RBA");

        bool found = table.TryGetSeries(s_usdAud, "RBA", out RateSeries? series);

        Assert.IsFalse(found);
        Assert.IsNull(series);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.TryGetSeries" /> returns a fresh immutable snapshot when the
    /// series exists and is non-empty.
    /// </summary>
    [TestMethod]
    public void TryGetSeries_WhenBuilderHasObservations_ShouldReturnSnapshot()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        bool found = table.TryGetSeries(s_usdAud, "RBA", out RateSeries? series);

        Assert.IsTrue(found);
        Assert.IsNotNull(series);
        Assert.AreEqual(s_usdAud, series!.Pair);
        Assert.AreEqual("RBA", series.Provider);
        Assert.AreEqual(1, series.Count);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.TryGetSeries" /> snapshots a fresh series each call so further
    /// builder mutation does not affect previously returned snapshots.
    /// </summary>
    [TestMethod]
    public void TryGetSeries_WhenBuilderMutatedAfterSnapshot_ShouldNotAffectSnapshot()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(table.TryGetSeries(s_usdAud, "RBA", out RateSeries? snapshot));
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 99m);

        Assert.IsTrue(snapshot!.TryGetRate(new DateOnly(2026, 6, 1), RateLookupOptions.Exact, out _, out decimal rate));
        Assert.AreEqual(1.50m, rate);
    }
}
