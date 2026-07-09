// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesStorageTests.Enumerate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesStorageTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeriesStorage.Enumerate" /> emits observations in strictly ascending
    /// date order regardless of input order.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvoked_ShouldYieldInAscendingOrder()
    {
        var storage = RateSeriesStorage.Create(
            [Obs(1020, 1.6m), Obs(1000, 1.4m), Obs(1010, 1.5m)],
            ObservationsParam);

        RateObservation[] observations = storage.Enumerate().ToArray();

        Assert.HasCount(3, observations);
        Assert.IsLessThan(observations[1].Date, observations[0].Date);
        Assert.IsLessThan(observations[2].Date, observations[1].Date);
    }

    /// <summary>
    /// Verifies that calling <see cref="RateSeriesStorage.Enumerate" /> twice on the same storage produces
    /// equivalent sequences.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvokedTwice_ShouldYieldEquivalentSequences()
    {
        var storage = RateSeriesStorage.Create(
            [Obs(1000, 1.4m), Obs(1010, 1.5m), Obs(1020, 1.6m)],
            ObservationsParam);

        RateObservation[] first = storage.Enumerate().ToArray();
        RateObservation[] second = storage.Enumerate().ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that each emitted <see cref="RateObservation" /> carries both the recorded date and the
    /// recorded rate.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvoked_ShouldEmitDateAndRateTogether()
    {
        var storage = RateSeriesStorage.Create(
            [Obs(1000, 1.4m), Obs(1010, 1.5m)],
            ObservationsParam);

        RateObservation[] observations = storage.Enumerate().ToArray();

        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(1.4m, observations[0].Rate);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[1].Date);
        Assert.AreEqual(1.5m, observations[1].Rate);
    }
}
