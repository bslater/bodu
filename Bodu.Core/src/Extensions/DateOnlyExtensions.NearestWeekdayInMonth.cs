// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NearestWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the nearest Monday-to-Friday date to <paramref name="date" /> without crossing its calendar month.
    /// </summary>
    /// <param name="date">The date to adjust.</param>
    /// <returns>
    /// The same date if it is Monday through Friday; otherwise the preceding Friday for Saturday, or the following
    /// Monday for Sunday, except at month boundaries where the nearest weekday inside the month is returned.
    /// </returns>
    /// <remarks>
    /// A Saturday on the first day of a month maps to Monday the third. A Sunday on the last day of a month maps
    /// to the preceding Friday. Weekdays mean Monday through Friday, independent of culture or locale.
    /// This operation does not consult local time-zone or holiday calendars.
    /// </remarks>
    public static DateOnly NearestWeekdayInMonth(this DateOnly date) =>
        date.DayOfWeek switch
        {
            DayOfWeek.Saturday => date.AddDays(date.Day == 1 ? 2 : -1),
            DayOfWeek.Sunday => date.AddDays(date.Day == DateTime.DaysInMonth(date.Year, date.Month) ? -2 : 1),
            _ => date,
        };
}
