// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedUnit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Records what a file feed's download unit (a feed, an era, a month) held when it was loaded: the days it can no
/// longer revise, and when its open days must be downloaded again.
/// </summary>
/// <param name="FinalThrough">
/// The last day the unit can no longer revise; <see cref="DateOnly.MaxValue" /> for a unit whose every day was final
/// when it was loaded.
/// </param>
/// <param name="ExpiresAt">
/// The instant after which a request that reaches past <paramref name="FinalThrough" /> loads the unit again.
/// </param>
internal readonly record struct LoadedUnit(DateOnly FinalThrough, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Records a unit just loaded.
    /// </summary>
    /// <param name="unitEnd">
    /// The last day the unit can ever hold, or <see langword="null" /> for a unit that keeps growing (the RBA's current
    /// era, any ECB feed).
    /// </param>
    /// <param name="lastReturnedDate">
    /// The latest date the unit returned a rate for, or <see langword="null" /> when it returned none.
    /// </param>
    /// <param name="fetchedAtUtc">The instant the unit was obtained from the source.</param>
    /// <param name="refreshInterval">How long the unit's open days may be served before it is loaded again.</param>
    /// <returns>The record of the loaded unit.</returns>
    internal static LoadedUnit Create(DateOnly? unitEnd, DateOnly? lastReturnedDate, DateTimeOffset fetchedAtUtc, TimeSpan refreshInterval)
    {
        DateOnly finalThrough = RatePublication.FinalThrough(lastReturnedDate, fetchedAtUtc);

        // A unit that ended before its first open day can never change again.
        return unitEnd is { } end && end <= finalThrough
            ? new LoadedUnit(DateOnly.MaxValue, DateTimeOffset.MaxValue)
            : new LoadedUnit(finalThrough, RatePublication.ExpiresAt(fetchedAtUtc, refreshInterval));
    }

    /// <summary>
    /// Reports whether the unit as loaded still serves a request that reaches a given day.
    /// </summary>
    /// <param name="through">The last day the request needs.</param>
    /// <param name="nowUtc">The current instant.</param>
    /// <returns>
    /// <see langword="true" /> when every day the request needs is final, or the unit's open days have not yet expired;
    /// otherwise <see langword="false" />.
    /// </returns>
    internal bool Covers(DateOnly through, DateTimeOffset nowUtc) =>
        through <= FinalThrough || nowUtc < ExpiresAt;
}
