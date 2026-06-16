// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTableBuilderTests.Upsert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateTableBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Upsert" /> creates a series on first call and inserts the rate.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenSeriesMissing_ShouldCreateAndInsert()
    {
        ExchangeRateTableBuilder table = new();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(table.TryGetBuilder(s_usdAud, "RBA", out ExchangeRateSeriesBuilder? builder));
        Assert.IsTrue(builder!.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Upsert" /> replaces an existing observation in place.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenSeriesExists_ShouldReplaceRate()
    {
        ExchangeRateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.75m);

        Assert.IsTrue(table.TryGetBuilder(s_usdAud, "RBA", out ExchangeRateSeriesBuilder? builder));
        Assert.AreEqual(1, builder!.Count);
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.75m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Upsert" /> validates the rate.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenRateInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateTableBuilder table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 0m);
            },
            "rate");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Upsert" /> validates the provider argument.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenProviderIsEmpty_ShouldThrowArgumentException()
    {
        ExchangeRateTableBuilder table = new();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                table.Upsert(s_usdAud, "  ", new DateOnly(2026, 6, 1), 1.50m);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateTableBuilder.Upsert" /> records a supplied fetch instant at the series grain
    /// so the materialized series carries it.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenFetchInstantSupplied_ShouldStampSeries()
    {
        DateTimeOffset fetchedAt = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        ExchangeRateTableBuilder table = new();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m, fetchedAt);

        Assert.IsTrue(table.TryGetSeries(s_usdAud, "RBA", out ExchangeRateSeries? series));
        Assert.AreEqual(fetchedAt, series!.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that the four-argument <see cref="ExchangeRateTableBuilder.Upsert" /> overload leaves the materialized
    /// series' fetch instant <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenFetchInstantOmitted_ShouldLeaveSeriesInstantNull()
    {
        ExchangeRateTableBuilder table = new();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(table.TryGetSeries(s_usdAud, "RBA", out ExchangeRateSeries? series));
        Assert.IsNull(series!.FetchedAtUtc);
    }
}
