// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateBookTests.ToBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RateBookTests
{
    /// <summary>
    /// Verifies that the builder is seeded with every series and observation from the source book.
    /// </summary>
    [TestMethod]
    public void ToBuilder_ShouldCopyEverySeriesAndObservation()
    {
        RateBook book = new([BuildSeries(s_usdAud, "RBA", 1.5m), BuildSeries(s_eurAud, "ECB", 1.6m)]);

        RateTableBuilder builder = book.ToBuilder();

        Assert.AreEqual(2, builder.Count);
        Assert.IsTrue(builder.TryGetSeries(s_usdAud, "RBA", out RateSeries? usd));
        Assert.AreEqual(1.5m, usd!.GetObservations().Single().Rate);
        Assert.IsTrue(builder.TryGetSeries(s_eurAud, "ECB", out RateSeries? eur));
        Assert.AreEqual(1.6m, eur!.GetObservations().Single().Rate);
    }

    /// <summary>
    /// Verifies that a series' fetch instant survives the book-to-builder round trip.
    /// </summary>
    [TestMethod]
    public void ToBuilder_ShouldPreserveFetchedAtUtc()
    {
        DateTimeOffset fetched = new(2024, 1, 2, 3, 4, 5, TimeSpan.Zero);
        RateSeries series = new(s_usdAud, "RBA", [(new DateOnly(2024, 1, 1), 1.5m)], fetched);
        RateBook book = new([series]);

        RateTableBuilder builder = book.ToBuilder();

        Assert.IsTrue(builder.TryGetSeries(s_usdAud, "RBA", out RateSeries? copied));
        Assert.AreEqual(fetched, copied!.FetchedAtUtc);
    }

    /// <summary>
    /// Verifies that a multi-provider book round-trips through the builder back to an equal book.
    /// </summary>
    [TestMethod]
    public void ToBuilder_ThenToBook_ShouldRoundTripMultiProviderBook()
    {
        RateBook original = new([BuildSeries(s_usdAud, "RBA", 1.5m), BuildSeries(s_usdAud, "ECB", 1.51m)]);

        RateBook roundTripped = original.ToBuilder().ToBook();

        Assert.AreEqual(original.Count, roundTripped.Count);
        Assert.IsTrue(roundTripped.TryGetSeries(s_usdAud, "RBA", out RateSeries? rba));
        Assert.AreEqual(1.5m, rba!.GetObservations().Single().Rate);
        Assert.IsTrue(roundTripped.TryGetSeries(s_usdAud, "ECB", out RateSeries? ecb));
        Assert.AreEqual(1.51m, ecb!.GetObservations().Single().Rate);
    }

    /// <summary>
    /// Verifies that an empty book yields an empty builder.
    /// </summary>
    [TestMethod]
    public void ToBuilder_WhenBookIsEmpty_ShouldReturnEmptyBuilder()
    {
        RateBook book = new([]);

        Assert.AreEqual(0, book.ToBuilder().Count);
    }

    /// <summary>
    /// Verifies that mutating the returned builder never affects the source book.
    /// </summary>
    [TestMethod]
    public void ToBuilder_WhenBuilderMutated_ShouldLeaveSourceBookUntouched()
    {
        RateBook book = new([BuildSeries(s_usdAud, "RBA", 1.5m)]);

        RateTableBuilder builder = book.ToBuilder();
        builder.Upsert(s_usdAud, "RBA", new DateOnly(2024, 1, 1), 9.9m);
        builder.Upsert(s_eurAud, "ECB", new DateOnly(2024, 1, 2), 1.6m);

        Assert.AreEqual(1, book.Count);
        Assert.IsTrue(book.TryGetSeries(s_usdAud, "RBA", out RateSeries? series));
        Assert.AreEqual(1.5m, series!.GetObservations().Single().Rate);
    }
}
