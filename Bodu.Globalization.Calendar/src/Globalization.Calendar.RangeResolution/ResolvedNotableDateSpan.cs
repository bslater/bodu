// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolvedNotableDateSpan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Represents the resolved span of a single occurrence: its effective start date and inclusive duration in days.
/// </summary>
/// <param name="StartDate">The effective start date of the occurrence.</param>
/// <param name="DurationDays">The inclusive number of days the occurrence spans; always at least one.</param>
internal readonly record struct ResolvedNotableDateSpan(DateOnly StartDate, int DurationDays)
{
    /// <summary>
    /// Gets the inclusive end date of the span.
    /// </summary>
    /// <value>
    /// The last day the occurrence spans, equal to <c><see cref="StartDate" /> + <see cref="DurationDays" /> - 1</c>.
    /// </value>
    public DateOnly EndDate =>
        StartDate.AddDays(DurationDays - 1);
}
