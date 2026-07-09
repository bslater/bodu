// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilderTests.Upsert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

public partial class RateTableBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Upsert" /> creates a series on first call and inserts the rate.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenSeriesMissing_ShouldCreateAndInsert()
    {
        RateTableBuilder table = new();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(table.TryGetBuilder(s_usdAud, "RBA", out RateSeriesBuilder? builder));
        Assert.IsTrue(builder!.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Upsert" /> replaces an existing observation in place.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenSeriesExists_ShouldReplaceRate()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.75m);

        Assert.IsTrue(table.TryGetBuilder(s_usdAud, "RBA", out RateSeriesBuilder? builder));
        Assert.AreEqual(1, builder!.Count);
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.75m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Upsert" /> validates the rate.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenRateInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        RateTableBuilder table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 0m);
            },
            "rate");
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Upsert" /> validates the provider argument.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenProviderIsEmpty_ShouldThrowArgumentException()
    {
        RateTableBuilder table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                table.Upsert(s_usdAud, "  ", new DateOnly(2026, 6, 1), 1.50m);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that <see cref="RateTableBuilder.Upsert" /> records a supplied fetch instant at the series grain
    /// so the materialized series carries it.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenFetchInstantSupplied_ShouldStampSeries()
    {
        DateTimeOffset fetchedAt = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        RateTableBuilder table = new();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m, fetchedAt);

        Assert.IsTrue(table.TryGetSeries(s_usdAud, "RBA", out RateSeries? series));
        Assert.AreEqual(fetchedAt, series!.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that the four-argument <see cref="RateTableBuilder.Upsert" /> overload leaves the materialized
    /// series' fetch instant <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenFetchInstantOmitted_ShouldLeaveSeriesInstantNull()
    {
        RateTableBuilder table = new();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(table.TryGetSeries(s_usdAud, "RBA", out RateSeries? series));
        Assert.IsNull(series!.FetchedAtUtc);
    }
}
