// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateFeed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Identifies one of the <c>eurofxref</c> XML files in which the European Central Bank publishes its euro
/// foreign-exchange reference rates.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a calendar-partitioned archive, the ECB publishes a small set of overlapping feeds that each end at the most
/// recent business day and reach back a different distance: a latest-day file, a rolling ninety-day file, and the full
/// history since 1999. A feed is therefore characterized by its look-back window rather than a fixed date range,
/// exposed through <see cref="LookbackDays" />, and the earliest date it is expected to contain is computed relative to
/// a reference date via <see cref="EarliestDate(DateOnly)" />.
/// </para>
/// <para>
/// The default catalogue is exposed through <see cref="Default" />. The named singletons <see cref="Daily" />,
/// <see cref="Last90Days" />, and <see cref="Full" /> let callers compose a custom catalogue, and the file names can be
/// overridden through the provider options if the ECB changes them.
/// </para>
/// </remarks>
public sealed class EcbRateFeed
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EcbRateFeed" /> class.
    /// </summary>
    /// <param name="name">The feed label.</param>
    /// <param name="fileName">The feed file name, relative to the provider's base URL.</param>
    /// <param name="lookbackDays">
    /// The number of days back from a reference date the feed is expected to cover, or <see langword="null" /> when the
    /// feed carries the full history since <see cref="Epoch" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> or <paramref name="fileName" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="lookbackDays" /> is negative.
    /// </exception>
    public EcbRateFeed(string name, string fileName, int? lookbackDays)
    {
        ThrowHelper.ThrowIfNull(name);
        ThrowHelper.ThrowIfNull(fileName);
        if (lookbackDays is { } days)
            ThrowHelper.ThrowIfNegative(days);

        Name = name;
        FileName = fileName;
        LookbackDays = lookbackDays;
    }

    /// <summary>
    /// Gets the first date for which the ECB publishes euro reference rates.
    /// </summary>
    /// <value>4 January 1999, the start of the euro reference-rate series.</value>
    public static DateOnly Epoch { get; } = new(1999, 1, 4);

    /// <summary>
    /// Gets the latest-day feed (<c>eurofxref-daily.xml</c>), containing only the most recent published rates.
    /// </summary>
    /// <value>The latest-day feed, with a four-day look-back to tolerate weekends and holidays.</value>
    public static EcbRateFeed Daily { get; } = new("daily", "eurofxref-daily.xml", 4);

    /// <summary>
    /// Gets the rolling ninety-day feed (<c>eurofxref-hist-90d.xml</c>).
    /// </summary>
    /// <value>The ninety-day feed.</value>
    public static EcbRateFeed Last90Days { get; } = new("hist-90d", "eurofxref-hist-90d.xml", 90);

    /// <summary>
    /// Gets the full-history feed (<c>eurofxref-hist.xml</c>), containing every published day since
    /// <see cref="Epoch" />.
    /// </summary>
    /// <value>The full-history feed.</value>
    public static EcbRateFeed Full { get; } = new("hist", "eurofxref-hist.xml", null);

    /// <summary>
    /// Gets the default catalogue of ECB feeds, ordered from the narrowest look-back to the widest.
    /// </summary>
    /// <value>
    /// The ordered, immutable default feed catalogue: the ninety-day feed followed by the full-history feed.
    /// </value>
    /// <remarks>
    /// The latest-day feed is omitted from the default catalogue because the ninety-day feed already contains the most
    /// recent day; include <see cref="Daily" /> explicitly when a minimal latest-only download is preferred.
    /// </remarks>
    public static IReadOnlyList<EcbRateFeed> Default { get; } =
    [
        Last90Days,
        Full,
    ];

    /// <summary>
    /// Gets the feed label (for example, <c>hist-90d</c>).
    /// </summary>
    /// <value>The feed label.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the file name of the feed, relative to the provider's base URL.
    /// </summary>
    /// <value>The feed file name (for example, <c>eurofxref-hist.xml</c>).</value>
    public string FileName { get; }

    /// <summary>
    /// Gets the number of days back from a reference date the feed is expected to cover.
    /// </summary>
    /// <value>The look-back window in days, or <see langword="null" /> for the full-history feed.</value>
    public int? LookbackDays { get; }

    /// <summary>
    /// Gets a value indicating whether the feed carries the full history rather than a bounded look-back window.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="LookbackDays" /> is <see langword="null" />; otherwise
    /// <see langword="false" />.
    /// </value>
    public bool IsFullHistory => LookbackDays is null;

    /// <summary>
    /// Computes the earliest date the feed is expected to contain relative to a reference date.
    /// </summary>
    /// <param name="asOf">The reference date, typically the current date.</param>
    /// <returns>
    /// <see cref="Epoch" /> for the full-history feed; otherwise <paramref name="asOf" /> shifted back by
    /// <see cref="LookbackDays" /> days.
    /// </returns>
    public DateOnly EarliestDate(DateOnly asOf) =>
        LookbackDays is null ? Epoch : asOf.AddDays(-LookbackDays.Value);

    /// <summary>
    /// Determines whether the feed is expected to cover the specified date relative to a reference date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="asOf">The reference date, typically the current date.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="date" /> is on or after the feed's earliest expected date;
    /// otherwise <see langword="false" />.
    /// </returns>
    public bool Covers(DateOnly date, DateOnly asOf) =>
        date >= EarliestDate(asOf);

    /// <summary>
    /// Finds the narrowest feed in <paramref name="feeds" /> expected to cover the specified date.
    /// </summary>
    /// <param name="date">The date to resolve.</param>
    /// <param name="feeds">The feed catalogue to search, ordered from narrowest to widest look-back.</param>
    /// <param name="asOf">The reference date, typically the current date.</param>
    /// <returns>The covering feed, or <see langword="null" /> when no feed covers <paramref name="date" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="feeds" /> is <see langword="null" />.
    /// </exception>
    public static EcbRateFeed? ForDate(DateOnly date, IReadOnlyList<EcbRateFeed> feeds, DateOnly asOf)
    {
        ThrowHelper.ThrowIfNull(feeds);

        for (int i = 0; i < feeds.Count; i++)
        {
            if (feeds[i].Covers(date, asOf))
                return feeds[i];
        }

        return null;
    }
}
