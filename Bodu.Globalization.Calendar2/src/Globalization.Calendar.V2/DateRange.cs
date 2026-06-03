// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Represents an inclusive range of calendar days bounded by a start and end date.
/// </summary>
/// <param name="StartDate">The first day of the range, inclusive.</param>
/// <param name="EndDate">The last day of the range, inclusive.</param>
public readonly record struct DateRange(DateOnly StartDate, DateOnly EndDate)
{
    /// <summary>
    /// Gets a value indicating whether the range is well-formed, with a start no later than its end.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the start is on or before the end; otherwise <see langword="false" />.
    /// </returns>
    public bool IsValid =>
        this.StartDate <= this.EndDate;

    /// <summary>
    /// Gets the number of days the range spans, inclusive of both endpoints.
    /// </summary>
    /// <returns>The inclusive day count, or zero when the range is not well-formed.</returns>
    public int DayCount =>
        this.IsValid ? (this.EndDate.DayNumber - this.StartDate.DayNumber) + 1 : 0;

    /// <summary>
    /// Determines whether the range contains the supplied date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns>
    /// <see langword="true" /> if the date falls within the range; otherwise <see langword="false" />.
    /// </returns>
    public bool Contains(DateOnly date) =>
        date >= this.StartDate && date <= this.EndDate;
}
