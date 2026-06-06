// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesStorageTests.Dates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesStorageTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesStorage.FirstDate" /> reflects the earliest observation date.
    /// </summary>
    [TestMethod]
    public void FirstDate_WhenStoragePopulated_ShouldReturnEarliestObservation()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            [Obs(1020, 1.6m), Obs(1000, 1.4m), Obs(1010, 1.5m)],
            ObservationsParam);

        Assert.AreEqual(DateOnly.FromDayNumber(1000), storage.FirstDate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesStorage.LastDate" /> reflects the latest observation date.
    /// </summary>
    [TestMethod]
    public void LastDate_WhenStoragePopulated_ShouldReturnLatestObservation()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            [Obs(1020, 1.6m), Obs(1000, 1.4m), Obs(1010, 1.5m)],
            ObservationsParam);

        Assert.AreEqual(DateOnly.FromDayNumber(1020), storage.LastDate);
    }

    /// <summary>
    /// Verifies that a single-entry storage reports the same date for both <see cref="ExchangeRateSeriesStorage.FirstDate" />
    /// and <see cref="ExchangeRateSeriesStorage.LastDate" />.
    /// </summary>
    [TestMethod]
    public void FirstAndLastDate_WhenSingleEntry_ShouldMatch()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            [Obs(1234, 2.5m)],
            ObservationsParam);

        Assert.AreEqual(storage.FirstDate, storage.LastDate);
        Assert.AreEqual(DateOnly.FromDayNumber(1234), storage.FirstDate);
    }
}
