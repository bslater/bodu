// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilderTests.SnapshotReuse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RateTableBuilderTests
{
    /// <summary>The builder mutations exercised between two snapshots, by name: each changes the AUD series' second day or adds a third.</summary>
    private static readonly Dictionary<string, Action<RateTableBuilder, RateSeriesBuilder>> s_mutations = new(StringComparer.Ordinal)
    {
        ["Add"] = static (_, builder) => builder.Add(new DateOnly(2026, 6, 3), 1.53m),
        ["TryAdd"] = static (_, builder) => builder.TryAdd(new DateOnly(2026, 6, 3), 1.53m),
        ["Set"] = static (_, builder) => builder.Set(new DateOnly(2026, 6, 2), 1.99m),
        ["TrySet"] = static (_, builder) => builder.TrySet(new DateOnly(2026, 6, 2), 1.99m),
        ["Upsert"] = static (_, builder) => builder.Upsert(new DateOnly(2026, 6, 2), 1.99m),
        ["Remove"] = static (_, builder) => builder.Remove(new DateOnly(2026, 6, 2)),
        ["AddRange"] = static (_, builder) => builder.AddRange([new RateObservation(new DateOnly(2026, 6, 3), 1.53m)]),
        ["UpsertRange"] = static (_, builder) => builder.UpsertRange([new RateObservation(new DateOnly(2026, 6, 2), 1.99m)]),
        ["AddRangeTuples"] = static (_, builder) => builder.AddRange([(new DateOnly(2026, 6, 3), 1.53m)]),
        ["UpsertRangeTuples"] = static (_, builder) => builder.UpsertRange([(new DateOnly(2026, 6, 2), 1.99m)]),
        ["FetchedAtUtc"] = static (_, builder) => builder.FetchedAtUtc = new DateTimeOffset(2026, 6, 4, 0, 0, 0, TimeSpan.Zero),
        ["TableUpsert"] = static (table, _) => table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 2), 1.99m),
    };

    /// <summary>
    /// Verifies that every kind of change made to a series' builder between two calls of
    /// <see cref="RateTableBuilder.ToSeries" />, through the table or through a builder it handed out, is reflected in
    /// the second call's snapshot of that series.
    /// </summary>
    /// <param name="mutation">The name of the builder mutation applied between the calls.</param>
    [TestMethod]
    [DataRow("Add")]
    [DataRow("TryAdd")]
    [DataRow("Set")]
    [DataRow("TrySet")]
    [DataRow("Upsert")]
    [DataRow("Remove")]
    [DataRow("AddRange")]
    [DataRow("UpsertRange")]
    [DataRow("AddRangeTuples")]
    [DataRow("UpsertRangeTuples")]
    [DataRow("FetchedAtUtc")]
    [DataRow("TableUpsert")]
    public void ToSeries_AfterBuilderMutation_ShouldReflectTheMutation(string mutation)
    {
        RateTableBuilder table = new();
        DateOnly day1 = new(2026, 6, 1);
        DateOnly day2 = new(2026, 6, 2);
        table.Upsert(s_usdAud, "RBA", day1, 1.50m);
        table.Upsert(s_usdAud, "RBA", day2, 1.51m);
        table.Upsert(s_usdJpy, "BoJ", day1, 110m);
        _ = table.ToSeries();
        RateSeriesBuilder builder = table.GetOrAddSeries(s_usdAud, "RBA");

        s_mutations[mutation](table, builder);

        RateSeries expected = builder.ToSeries();
        RateSeries actual = table.ToSeries().Single(series => series.Pair == s_usdAud);

        CollectionAssert.AreEqual(expected.GetObservations().ToArray(), actual.GetObservations().ToArray());
        Assert.AreEqual(expected.FetchedAtUtc, actual.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a series removed and added again between two calls of <see cref="RateTableBuilder.ToSeries" /> is
    /// snapshotted from its new builder.
    /// </summary>
    [TestMethod]
    public void ToSeries_AfterSeriesRemovedAndAddedAgain_ShouldSnapshotTheNewBuilder()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        _ = table.ToSeries();

        _ = table.Remove(s_usdAud, "RBA");
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 7, 1), 1.70m);
        RateSeries actual = table.ToSeries().Single();

        Assert.AreEqual(1, actual.Count);
        Assert.IsTrue(actual.TryGetRate(new DateOnly(2026, 7, 1), RateLookupOptions.Exact, out _, out decimal rate));
        Assert.AreEqual(1.70m, rate);
    }

    /// <summary>
    /// Verifies that a series emptied between two calls of <see cref="RateTableBuilder.ToSeries" /> is left out of the
    /// second call.
    /// </summary>
    [TestMethod]
    public void ToSeries_AfterSeriesEmptied_ShouldSkipIt()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        table.Upsert(s_usdJpy, "BoJ", new DateOnly(2026, 6, 1), 110m);
        _ = table.ToSeries();

        _ = table.GetOrAddSeries(s_usdAud, "RBA").Remove(new DateOnly(2026, 6, 1));

        Assert.AreEqual(s_usdJpy, table.ToSeries().Single().Pair);
    }

    /// <summary>
    /// Verifies that a snapshot taken after one series changed copies only that series: the unchanged series' snapshots
    /// are reused, so a provider that rebuilds its book after every load pays for what the load changed rather than for
    /// everything it holds.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenOneOfManySeriesChanged_ShouldCopyOnlyThatSeries()
    {
        RateTableBuilder table = new();
        CurrencyCode[] quotes = Enum.GetValues<CurrencyCode>()
            .Where(code => code is not CurrencyCode.None and not CurrencyCode.USD)
            .Take(40)
            .ToArray();
        DateOnly start = new(2020, 1, 1);
        foreach (CurrencyCode quote in quotes)
        {
            for (int day = 0; day < 1000; day++)
                table.Upsert(new CurrencyPair(CurrencyCode.USD, quote), "TEST", start.AddDays(day), 1.5m);
        }

        CurrencyPair changed = new(CurrencyCode.USD, quotes[0]);
        _ = table.ToSeries();
        table.Upsert(changed, "TEST", start.AddDays(1000), 1.6m);
        _ = table.ToSeries();
        table.Upsert(changed, "TEST", start.AddDays(1001), 1.6m);

        long before = GC.GetAllocatedBytesForCurrentThread();
        IReadOnlyList<RateSeries> snapshots = table.ToSeries();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // One series of about a thousand days copies about 20 KB; all forty copy about 800 KB.
        Assert.HasCount(40, snapshots);
        Assert.IsLessThan(100_000L, allocated);
    }
}
