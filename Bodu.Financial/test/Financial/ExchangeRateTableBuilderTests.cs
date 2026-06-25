// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTableBuilderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateTableBuilderTests
{
    private static readonly ExchangeRatePair s_usdAud = new(CurrencyCode.USD, CurrencyCode.AUD);
    private static readonly ExchangeRatePair s_usdJpy = new(CurrencyCode.USD, CurrencyCode.JPY);

    /// <summary>
    /// Verifies that the smoke-tier happy path constructs a table, upserts one rate across two series, snapshots,
    /// and reports the expected pair-count.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Table_WhenUpsertingTwoSeriesAndSnapshotting_ShouldProduceImmutableSnapshots()
    {
        ExchangeRateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);
        table.Upsert(s_usdJpy, "BoJ", new DateOnly(2026, 6, 1), 110m);

        IReadOnlyList<ExchangeRateSeries> snapshots = table.ToSeries();

        Assert.AreEqual(2, table.Count);
        Assert.HasCount(2, snapshots);
    }

    /// <summary>
    /// Verifies that a fresh table reports zero series.
    /// </summary>
    [TestMethod]
    public void Count_WhenNewTable_ShouldBeZero()
    {
        ExchangeRateTableBuilder table = new();

        Assert.AreEqual(0, table.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Remove" /> reports success when removing an existing series.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSeriesExists_ShouldReturnTrue()
    {
        ExchangeRateTableBuilder table = new();
        table.GetOrAddSeries(s_usdAud, "RBA").Add(new DateOnly(2026, 6, 1), 1.50m);

        bool removed = table.Remove(s_usdAud, "RBA");

        Assert.IsTrue(removed);
        Assert.IsFalse(table.ContainsSeries(s_usdAud, "RBA"));
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Remove" /> reports failure when the series does not exist.
    /// </summary>
    [TestMethod]
    public void Remove_WhenSeriesMissing_ShouldReturnFalse()
    {
        ExchangeRateTableBuilder table = new();

        bool removed = table.Remove(s_usdAud, "RBA");

        Assert.IsFalse(removed);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.ContainsSeries" /> validates the provider argument.
    /// </summary>
    [TestMethod]
    public void ContainsSeries_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        ExchangeRateTableBuilder table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = table.ContainsSeries(s_usdAud, null!);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.TryGetBuilder" /> returns <see langword="false" /> when the
    /// series does not exist.
    /// </summary>
    [TestMethod]
    public void TryGetBuilder_WhenSeriesMissing_ShouldReturnFalse()
    {
        ExchangeRateTableBuilder table = new();

        bool found = table.TryGetBuilder(s_usdAud, "RBA", out ExchangeRateSeriesBuilder? builder);

        Assert.IsFalse(found);
        Assert.IsNull(builder);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.TryGetBuilder" /> returns the existing builder when present.
    /// </summary>
    [TestMethod]
    public void TryGetBuilder_WhenSeriesExists_ShouldReturnTrueAndBuilder()
    {
        ExchangeRateTableBuilder table = new();
        ExchangeRateSeriesBuilder seeded = table.GetOrAddSeries(s_usdAud, "RBA");
        seeded.Add(new DateOnly(2026, 6, 1), 1.50m);

        bool found = table.TryGetBuilder(s_usdAud, "RBA", out ExchangeRateSeriesBuilder? builder);

        Assert.IsTrue(found);
        Assert.AreSame(seeded, builder);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Keys" /> exposes the currently tracked series keys.
    /// </summary>
    [TestMethod]
    public void Keys_WhenSeriesAdded_ShouldEnumerateKeys()
    {
        ExchangeRateTableBuilder table = new();
        table.GetOrAddSeries(s_usdAud, "RBA");
        table.GetOrAddSeries(s_usdJpy, "BoJ");

        ExchangeRateSeriesKey[] keys = table.Keys.ToArray();

        Assert.HasCount(2, keys);
        CollectionAssert.Contains(keys, new ExchangeRateSeriesKey(s_usdAud, "RBA"));
        CollectionAssert.Contains(keys, new ExchangeRateSeriesKey(s_usdJpy, "BoJ"));
    }
}
