// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class RateTableBuilderTests
{
    private static readonly CurrencyPair s_usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);
    private static readonly CurrencyPair s_usdJpy = new(CurrencyCode.USD, CurrencyCode.JPY);

    /// <summary>
    /// Verifies that the smoke-tier happy path constructs a table, upserts one rate across two series, snapshots,
    /// and reports the expected pair-count.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Table_WhenUpsertingTwoSeriesAndSnapshotting_ShouldProduceImmutableSnapshots()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        table.Upsert(s_usdJpy, "BoJ", new DateOnly(2026, 6, 1), 110m);

        IReadOnlyList<RateSeries> snapshots = table.ToSeries();

        Assert.AreEqual(2, table.Count);
        Assert.HasCount(2, snapshots);
    }

    /// <summary>
    /// Verifies that a fresh table reports zero series.
    /// </summary>
    [TestMethod]
    public void Count_WhenNewTable_ShouldBeZero()
    {
        RateTableBuilder table = new();

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Remove" /> reports success when removing an existing series.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSeriesExists_ShouldReturnTrue()
    {
        RateTableBuilder table = new();
        table.GetOrAddSeries(s_usdAud, "RBA").Add(new DateOnly(2026, 6, 1), 1.50m);

        bool removed = table.Remove(s_usdAud, "RBA");

        Assert.IsTrue(removed);
        Assert.IsFalse(table.ContainsSeries(s_usdAud, "RBA"));
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Remove" /> reports failure when the series does not exist.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSeriesMissing_ShouldReturnFalse()
    {
        RateTableBuilder table = new();

        bool removed = table.Remove(s_usdAud, "RBA");

        Assert.IsFalse(removed);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.ContainsSeries" /> validates the provider argument.
    /// </summary>
    [TestMethod]
    public void ContainsSeries_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        RateTableBuilder table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = table.ContainsSeries(s_usdAud, null!);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.TryGetBuilder" /> returns <see langword="false" /> when the
    /// series does not exist.
    /// </summary>
    [TestMethod]
    public void TryGetBuilder_WhenSeriesMissing_ShouldReturnFalse()
    {
        RateTableBuilder table = new();

        bool found = table.TryGetBuilder(s_usdAud, "RBA", out RateSeriesBuilder? builder);

        Assert.IsFalse(found);
        Assert.IsNull(builder);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.TryGetBuilder" /> returns the existing builder when present.
    /// </summary>
    [TestMethod]
    public void TryGetBuilder_WhenSeriesExists_ShouldReturnTrueAndBuilder()
    {
        RateTableBuilder table = new();
        RateSeriesBuilder seeded = table.GetOrAddSeries(s_usdAud, "RBA");
        seeded.Add(new DateOnly(2026, 6, 1), 1.50m);

        bool found = table.TryGetBuilder(s_usdAud, "RBA", out RateSeriesBuilder? builder);

        Assert.IsTrue(found);
        Assert.AreSame(seeded, builder);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Keys" /> exposes the currently tracked series keys.
    /// </summary>
    [TestMethod]
    public void Keys_WhenSeriesAdded_ShouldEnumerateKeys()
    {
        RateTableBuilder table = new();
        table.GetOrAddSeries(s_usdAud, "RBA");
        table.GetOrAddSeries(s_usdJpy, "BoJ");

        RateSeriesKey[] keys = table.Keys.ToArray();

        Assert.HasCount(2, keys);
        CollectionAssert.Contains(keys, new RateSeriesKey(s_usdAud, "RBA"));
        CollectionAssert.Contains(keys, new RateSeriesKey(s_usdJpy, "BoJ"));
    }
}
