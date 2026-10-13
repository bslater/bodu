// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RatePublication.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Decides which days of a download a rate feed can no longer revise, and how long the rest of the download may be
/// served before it is downloaded again.
/// </summary>
/// <remarks>
/// <para>
/// A day is final once it has passed - every day before the UTC date of the download - or once the source has already
/// returned a rate for it. Today, and any later day, is open: the source may not yet have published it, so it is held
/// only until the provider's refresh interval has passed and then downloaded again.
/// </para>
/// <para>
/// This type is compiled into each provider that tracks what it has loaded, so that every provider applies the same
/// rule.
/// </para>
/// </remarks>
internal static class RatePublication
{
    /// <summary>
    /// Returns the last day of a download that the source can no longer revise.
    /// </summary>
    /// <param name="lastReturnedDate">
    /// The latest date the download returned a rate for, or <see langword="null" /> when it returned none.
    /// </param>
    /// <param name="fetchedAtUtc">The instant the download was obtained from the source.</param>
    /// <returns>
    /// The later of the day before the UTC date of <paramref name="fetchedAtUtc" /> and
    /// <paramref name="lastReturnedDate" />.
    /// </returns>
    internal static DateOnly FinalThrough(DateOnly? lastReturnedDate, DateTimeOffset fetchedAtUtc)
    {
        var today = DateOnly.FromDateTime(fetchedAtUtc.UtcDateTime);
        DateOnly closedThrough = today == DateOnly.MinValue ? today : today.AddDays(-1);

        return lastReturnedDate is { } last && last > closedThrough ? last : closedThrough;
    }

    /// <summary>
    /// Returns the instant after which the open days of a download are downloaded again.
    /// </summary>
    /// <param name="fetchedAtUtc">The instant the download was obtained from the source.</param>
    /// <param name="refreshInterval">How long the open days of a download may be served.</param>
    /// <returns>
    /// <paramref name="fetchedAtUtc" /> plus <paramref name="refreshInterval" />, saturating at
    /// <see cref="DateTimeOffset.MaxValue" /> and never earlier than <paramref name="fetchedAtUtc" />.
    /// </returns>
    internal static DateTimeOffset ExpiresAt(DateTimeOffset fetchedAtUtc, TimeSpan refreshInterval)
    {
        if (refreshInterval <= TimeSpan.Zero)
            return fetchedAtUtc;

        return refreshInterval >= DateTimeOffset.MaxValue - fetchedAtUtc
            ? DateTimeOffset.MaxValue
            : fetchedAtUtc + refreshInterval;
    }
}
