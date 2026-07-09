// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilderTests.ToSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateTableBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.ToSeries" /> on an empty table returns an empty list.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenEmpty_ShouldReturnEmptyList()
    {
        RateTableBuilder table = new();

        IReadOnlyList<RateSeries> snapshots = table.ToSeries();

        Assert.IsEmpty(snapshots);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.ToSeries" /> produces one snapshot per non-empty series.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenSeriesPopulated_ShouldReturnSnapshotPerSeries()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        table.Upsert(s_usdJpy, "BoJ", new DateOnly(2026, 6, 1), 110m);

        IReadOnlyList<RateSeries> snapshots = table.ToSeries();

        Assert.HasCount(2, snapshots);
    }

    /// <summary>
    /// Verifies that empty builders are skipped because immutable series cannot be empty.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenSomeBuildersEmpty_ShouldSkipThem()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        table.GetOrAddSeries(s_usdJpy, "BoJ"); // intentionally empty

        IReadOnlyList<RateSeries> snapshots = table.ToSeries();

        Assert.HasCount(1, snapshots);
        Assert.AreEqual(s_usdAud, snapshots[0].Pair);
    }
}
