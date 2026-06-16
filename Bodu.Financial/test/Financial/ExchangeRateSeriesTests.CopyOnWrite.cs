// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesTests.CopyOnWrite.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesTests
{
    private static ExchangeRateObservation[] CopyOnWriteSampleObservations() =>
    [
        new(new DateOnly(2026, 6, 1), 1.50m),
        new(new DateOnly(2026, 6, 3), 1.52m),
        new(new DateOnly(2026, 6, 5), 1.54m),
    ];

    /// <summary>
    /// Verifies that the observation-based constructor stores the supplied observations.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenObservationsSupplied_ShouldStoreObservations()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        Assert.AreEqual(3, series.Count);
        CollectionAssert.AreEqual(CopyOnWriteSampleObservations(), series.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.ToBuilder" /> returns a builder seeded with the same
    /// observations as the source series.
    /// </summary>
    [TestMethod]
    public void ToBuilder_WhenCalled_ShouldReturnBuilderWithSameObservations()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExchangeRateSeriesBuilder builder = series.ToBuilder();

        Assert.AreEqual(s_usdAud, builder.Pair);
        Assert.AreEqual("RBA", builder.Provider);
        CollectionAssert.AreEqual(CopyOnWriteSampleObservations(), builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that mutating a builder seeded from a series does not affect the source series.
    /// </summary>
    [TestMethod]
    public void ToBuilder_WhenBuilderMutated_ShouldNotAffectOriginalSeries()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());
        ExchangeRateSeriesBuilder builder = series.ToBuilder();

        builder.Upsert(new DateOnly(2026, 6, 1), 99m);
        builder.Add(new DateOnly(2026, 6, 7), 1.60m);

        Assert.AreEqual(3, series.Count);
        Assert.IsTrue(series.TryGetRate(new DateOnly(2026, 6, 1), ExchangeRateLookupOptions.Exact, out _, out decimal originalRate));
        Assert.AreEqual(1.50m, originalRate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.WithRate" /> on a missing date inserts a new observation.
    /// </summary>
    [TestMethod]
    public void WithRate_WhenDateMissing_ShouldReturnSeriesWithNewObservation()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExchangeRateSeries updated = series.WithRate(new DateOnly(2026, 6, 4), 1.53m);

        Assert.AreEqual(4, updated.Count);
        Assert.IsTrue(updated.TryGetRate(new DateOnly(2026, 6, 4), ExchangeRateLookupOptions.Exact, out _, out decimal rate));
        Assert.AreEqual(1.53m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.WithRate" /> on an existing date replaces the rate.
    /// </summary>
    [TestMethod]
    public void WithRate_WhenDateExists_ShouldReturnSeriesWithReplacedRate()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExchangeRateSeries updated = series.WithRate(new DateOnly(2026, 6, 1), 2.00m);

        Assert.AreEqual(3, updated.Count);
        Assert.IsTrue(updated.TryGetRate(new DateOnly(2026, 6, 1), ExchangeRateLookupOptions.Exact, out _, out decimal rate));
        Assert.AreEqual(2.00m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.WithRate" /> validates the rate.
    /// </summary>
    [TestMethod]
    public void WithRate_WhenRateInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = series.WithRate(new DateOnly(2026, 6, 1), 0m);
            },
            "rate");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.WithoutRate" /> on an existing date removes it.
    /// </summary>
    [TestMethod]
    public void WithoutRate_WhenDateExists_ShouldReturnSmallerSeries()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExchangeRateSeries updated = series.WithoutRate(new DateOnly(2026, 6, 3));

        Assert.AreEqual(2, updated.Count);
        Assert.IsFalse(updated.TryGetRate(new DateOnly(2026, 6, 3), ExchangeRateLookupOptions.Exact, out _, out _));
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.WithoutRate" /> on a missing date returns a series with the same
    /// observations.
    /// </summary>
    [TestMethod]
    public void WithoutRate_WhenDateMissing_ShouldReturnEquivalentSeries()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExchangeRateSeries updated = series.WithoutRate(new DateOnly(2026, 6, 15));

        Assert.AreEqual(3, updated.Count);
        CollectionAssert.AreEqual(CopyOnWriteSampleObservations(), updated.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.WithoutRate" /> removing the last observation throws
    /// <see cref="InvalidOperationException" /> because immutable series must be non-empty.
    /// </summary>
    [TestMethod]
    public void WithoutRate_WhenRemovingLastObservation_ShouldThrowInvalidOperationException()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", [new ExchangeRateObservation(new DateOnly(2026, 6, 1), 1.50m)]);

        Assert.ThrowsExactly<InvalidOperationException>(() => series.WithoutRate(new DateOnly(2026, 6, 1)));
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeries.GetObservations" /> yields the observations in ascending date
    /// order.
    /// </summary>
    [TestMethod]
    public void GetObservations_WhenCalled_ShouldYieldInAscendingOrder()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExchangeRateObservation[] observations = series.GetObservations().ToArray();

        Assert.AreEqual(3, observations.Length);
        for (int i = 1; i < observations.Length; i++)
        {
            Assert.IsTrue(observations[i - 1].Date < observations[i].Date);
        }
    }

    /// <summary>
    /// Verifies that calling <see cref="ExchangeRateSeries.GetObservations" /> twice produces equivalent sequences.
    /// </summary>
    [TestMethod]
    public void GetObservations_WhenCalledTwice_ShouldYieldEquivalentSequences()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", CopyOnWriteSampleObservations());

        ExchangeRateObservation[] first = series.GetObservations().ToArray();
        ExchangeRateObservation[] second = series.GetObservations().ToArray();

        CollectionAssert.AreEqual(first, second);
    }
}
