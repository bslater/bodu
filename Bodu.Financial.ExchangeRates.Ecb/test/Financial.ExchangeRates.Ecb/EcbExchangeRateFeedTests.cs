// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateFeedTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Ecb;

/// <summary>
/// Verifies the coverage and selection behavior of <see cref="EcbExchangeRateFeed" />.
/// </summary>
[TestClass]
public class EcbExchangeRateFeedTests
{
    /// <summary>
    /// A fixed reference date used so coverage assertions are deterministic.
    /// </summary>
    private static readonly DateOnly s_asOf = new(2026, 6, 13);

    /// <summary>
    /// Verifies that the default catalogue is ordered from the narrowest look-back to the full history.
    /// </summary>
    [TestMethod]
    public void Default_ShouldBeOrderedNarrowestToWidest()
    {
        var feeds = EcbExchangeRateFeed.Default;

        Assert.AreEqual(2, feeds.Count);
        Assert.AreEqual(90, feeds[0].LookbackDays);
        Assert.IsTrue(feeds[1].IsFullHistory);
    }

    /// <summary>
    /// Verifies that the full-history feed reports the euro reference-rate epoch as its earliest date.
    /// </summary>
    [TestMethod]
    public void EarliestDate_WhenFullHistory_ShouldReturnEpoch()
    {
        Assert.AreEqual(new DateOnly(1999, 1, 4), EcbExchangeRateFeed.Full.EarliestDate(s_asOf));
    }

    /// <summary>
    /// Verifies that a look-back feed shifts its earliest date relative to the reference date.
    /// </summary>
    [TestMethod]
    public void EarliestDate_WhenLookback_ShouldShiftFromReferenceDate()
    {
        Assert.AreEqual(s_asOf.AddDays(-90), EcbExchangeRateFeed.Last90Days.EarliestDate(s_asOf));
    }

    /// <summary>
    /// Verifies that a look-back feed covers a date inside its window but not one before it.
    /// </summary>
    [TestMethod]
    public void Covers_WhenLookback_ShouldRespectWindow()
    {
        Assert.IsTrue(EcbExchangeRateFeed.Last90Days.Covers(s_asOf.AddDays(-30), s_asOf));
        Assert.IsFalse(EcbExchangeRateFeed.Last90Days.Covers(s_asOf.AddDays(-120), s_asOf));
    }

    /// <summary>
    /// Verifies that a recent date resolves to the narrowest covering feed.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenRecentDate_ShouldSelectNarrowestFeed()
    {
        var feed = EcbExchangeRateFeed.ForDate(s_asOf.AddDays(-10), EcbExchangeRateFeed.Default, s_asOf);

        Assert.AreSame(EcbExchangeRateFeed.Last90Days, feed);
    }

    /// <summary>
    /// Verifies that an older date falls through to the full-history feed.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenOlderDate_ShouldSelectFullHistory()
    {
        var feed = EcbExchangeRateFeed.ForDate(new DateOnly(2010, 5, 1), EcbExchangeRateFeed.Default, s_asOf);

        Assert.AreSame(EcbExchangeRateFeed.Full, feed);
    }

    /// <summary>
    /// Verifies that a date before the euro reference-rate epoch is not covered by any default feed.
    /// </summary>
    [TestMethod]
    public void ForDate_WhenBeforeEpoch_ShouldReturnNull()
    {
        var feed = EcbExchangeRateFeed.ForDate(new DateOnly(1998, 1, 1), EcbExchangeRateFeed.Default, s_asOf);

        Assert.IsNull(feed);
    }

    /// <summary>
    /// Verifies that constructing a feed with a <see langword="null" /> name throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new EcbExchangeRateFeed(null!, "eurofxref-hist.xml", null);
        });
    }

    /// <summary>
    /// Verifies that constructing a feed with a negative look-back throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenLookbackIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EcbExchangeRateFeed("bad", "bad.xml", -1);
        });
    }
}
