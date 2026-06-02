// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTableBuilderTests.ToSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateTableBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.ToSeries" /> on an empty table returns an empty list.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenEmpty_ShouldReturnEmptyList()
    {
        ExchangeRateTableBuilder table = new();

        IReadOnlyList<ExchangeRateSeries> snapshots = table.ToSeries();

        Assert.AreEqual(0, snapshots.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.ToSeries" /> produces one snapshot per non-empty series.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenSeriesPopulated_ShouldReturnSnapshotPerSeries()
    {
        ExchangeRateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        table.Upsert(s_usdJpy, "BoJ", new DateOnly(2026, 6, 1), 110m);

        IReadOnlyList<ExchangeRateSeries> snapshots = table.ToSeries();

        Assert.AreEqual(2, snapshots.Count);
    }

    /// <summary>
    /// Verifies that empty builders are skipped because immutable series cannot be empty.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenSomeBuildersEmpty_ShouldSkipThem()
    {
        ExchangeRateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        table.GetOrAddSeries(s_usdJpy, "BoJ"); // intentionally empty

        IReadOnlyList<ExchangeRateSeries> snapshots = table.ToSeries();

        Assert.AreEqual(1, snapshots.Count);
        Assert.AreEqual(s_usdAud, snapshots[0].Pair);
    }
}
