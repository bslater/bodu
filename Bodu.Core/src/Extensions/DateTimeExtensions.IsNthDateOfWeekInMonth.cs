// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsNthDateOfWeekInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the date component of <paramref name="dateTime" /> is the specified ordinal occurrence
    /// of <paramref name="dayOfWeek" /> within its calendar month.
    /// </summary>
    /// <param name="dateTime">The date and time to test; the time and <see cref="DateTime.Kind" /> do not affect the result.</param>
    /// <param name="dayOfWeek">The weekday whose occurrence is being tested.</param>
    /// <param name="ordinal">First through fifth occurrence, or <see cref="WeekOrdinal.Last" />.</param>
    /// <returns>
    /// <see langword="true" /> if the date is the requested weekday occurrence; otherwise <see langword="false" />.
    /// A missing fifth occurrence returns <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> or <paramref name="ordinal" /> is not a defined enum value.
    /// </exception>
    public static bool IsNthDateOfWeekInMonth(this DateTime dateTime, DayOfWeek dayOfWeek, WeekOrdinal ordinal) =>
        DateOnly.FromDateTime(dateTime).IsNthDateOfWeekInMonth(dayOfWeek, ordinal);
}
