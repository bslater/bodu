// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesStorageTests.CreateFromSortedUnique.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesStorageTests
{
    /// <summary>
    /// Verifies that the trusted entry point <see cref="ExchangeRateSeriesStorage.CreateFromSortedUnique" />
    /// adopts the supplied arrays without revalidation.
    /// </summary>
    [TestMethod]
    public void CreateFromSortedUnique_WhenArraysSupplied_ShouldAdoptArrays()
    {
        int[] days = [1000, 1010, 1020];
        decimal[] rates = [1.4m, 1.5m, 1.6m];

        var storage = ExchangeRateSeriesStorage.CreateFromSortedUnique(days, rates);

        Assert.AreEqual(3, storage.Count);
        Assert.AreEqual(DateOnly.FromDayNumber(1000), storage.FirstDate);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), storage.LastDate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesStorage.CreateFromSortedUnique" /> exposes the adopted
    /// observations via <see cref="ExchangeRateSeriesStorage.Enumerate" />.
    /// </summary>
    [TestMethod]
    public void CreateFromSortedUnique_WhenEnumerated_ShouldYieldSuppliedEntries()
    {
        int[] days = [1000, 1010, 1020];
        decimal[] rates = [1.4m, 1.5m, 1.6m];

        var storage = ExchangeRateSeriesStorage.CreateFromSortedUnique(days, rates);

        ExchangeRateObservation[] observations = storage.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(1.4m, observations[0].Rate);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), observations[2].Date);
        Assert.AreEqual(1.6m, observations[2].Rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesStorage.CreateFromSortedUnique" /> supports a single-entry
    /// storage instance.
    /// </summary>
    [TestMethod]
    public void CreateFromSortedUnique_WhenSingleEntry_ShouldExposeBothFirstAndLastDate()
    {
        var storage = ExchangeRateSeriesStorage.CreateFromSortedUnique(
            [1234],
            [2.5m]);

        Assert.AreEqual(1, storage.Count);
        Assert.AreEqual(DateOnly.FromDayNumber(1234), storage.FirstDate);
        Assert.AreEqual(DateOnly.FromDayNumber(1234), storage.LastDate);
    }
}
