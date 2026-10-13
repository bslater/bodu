// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RatePublicationTests.FinalThrough.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class RatePublicationTests
{
    /// <summary>
    /// Verifies that a download that returned no rate is final through the day before the UTC date it was obtained.
    /// </summary>
    [TestMethod]
    public void FinalThrough_WhenNothingReturned_ShouldReturnTheDayBeforeTheDownload()
    {
        Assert.AreEqual(new DateOnly(2023, 1, 3), RatePublication.FinalThrough(null, s_fetchedAt));
    }

    /// <summary>
    /// Verifies that a download whose latest rate is dated before the day it was obtained is still final through the day
    /// before that day, since the days in between have passed.
    /// </summary>
    [TestMethod]
    public void FinalThrough_WhenLastRateIsOlder_ShouldReturnTheDayBeforeTheDownload()
    {
        Assert.AreEqual(new DateOnly(2023, 1, 3), RatePublication.FinalThrough(new DateOnly(2022, 12, 30), s_fetchedAt));
    }

    /// <summary>
    /// Verifies that a download that already returned a rate for the day it was obtained, or a later day, is final through
    /// that rate's date.
    /// </summary>
    /// <param name="year">The year of the latest returned rate.</param>
    /// <param name="month">The month of the latest returned rate.</param>
    /// <param name="day">The day of the latest returned rate.</param>
    [TestMethod]
    [DataRow(2023, 1, 4)]
    [DataRow(2023, 1, 5)]
    public void FinalThrough_WhenLastRateIsTodayOrLater_ShouldReturnThatDate(int year, int month, int day)
    {
        DateOnly last = new(year, month, day);

        Assert.AreEqual(last, RatePublication.FinalThrough(last, s_fetchedAt));
    }

    /// <summary>
    /// Verifies that a download obtained on the first representable day does not step before it.
    /// </summary>
    [TestMethod]
    public void FinalThrough_WhenObtainedOnFirstDay_ShouldNotUnderflow()
    {
        Assert.AreEqual(DateOnly.MinValue, RatePublication.FinalThrough(null, DateTimeOffset.MinValue));
    }
}
