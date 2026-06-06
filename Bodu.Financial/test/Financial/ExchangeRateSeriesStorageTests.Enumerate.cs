// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesStorageTests.Enumerate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesStorageTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesStorage.Enumerate" /> emits observations in strictly ascending
    /// date order regardless of input order.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvoked_ShouldYieldInAscendingOrder()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            [Obs(1020, 1.6m), Obs(1000, 1.4m), Obs(1010, 1.5m)],
            ObservationsParam);

        var observations = storage.Enumerate().ToArray();

        Assert.AreEqual(3, observations.Length);
        Assert.IsTrue(observations[0].Date < observations[1].Date);
        Assert.IsTrue(observations[1].Date < observations[2].Date);
    }

    /// <summary>
    /// Verifies that calling <see cref="ExchangeRateSeriesStorage.Enumerate" /> twice on the same storage produces
    /// equivalent sequences.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvokedTwice_ShouldYieldEquivalentSequences()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            [Obs(1000, 1.4m), Obs(1010, 1.5m), Obs(1020, 1.6m)],
            ObservationsParam);

        var first = storage.Enumerate().ToArray();
        var second = storage.Enumerate().ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    /// <summary>
    /// Verifies that each emitted <see cref="ExchangeRateObservation" /> carries both the recorded date and the
    /// recorded rate.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvoked_ShouldEmitDateAndRateTogether()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            [Obs(1000, 1.4m), Obs(1010, 1.5m)],
            ObservationsParam);

        var observations = storage.Enumerate().ToArray();

        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(1.4m, observations[0].Rate);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[1].Date);
        Assert.AreEqual(1.5m, observations[1].Rate);
    }
}
