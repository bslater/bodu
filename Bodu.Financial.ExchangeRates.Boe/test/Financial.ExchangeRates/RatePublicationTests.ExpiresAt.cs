// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RatePublicationTests.ExpiresAt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class RatePublicationTests
{
    /// <summary>
    /// Verifies that the open days of a download expire one refresh interval after it was obtained.
    /// </summary>
    [TestMethod]
    public void ExpiresAt_WhenIntervalIsPositive_ShouldAddTheInterval()
    {
        Assert.AreEqual(s_fetchedAt.AddHours(12), RatePublication.ExpiresAt(s_fetchedAt, TimeSpan.FromHours(12)));
    }

    /// <summary>
    /// Verifies that an interval reaching past the last representable instant saturates rather than throwing.
    /// </summary>
    [TestMethod]
    public void ExpiresAt_WhenIntervalOverflows_ShouldReturnMaxValue()
    {
        Assert.AreEqual(DateTimeOffset.MaxValue, RatePublication.ExpiresAt(s_fetchedAt, TimeSpan.MaxValue));
    }

    /// <summary>
    /// Verifies that a zero or negative interval expires the open days at once, never before the download.
    /// </summary>
    /// <param name="hours">The interval, in hours.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void ExpiresAt_WhenIntervalIsNotPositive_ShouldReturnTheDownloadInstant(int hours)
    {
        Assert.AreEqual(s_fetchedAt, RatePublication.ExpiresAt(s_fetchedAt, TimeSpan.FromHours(hours)));
    }
}
