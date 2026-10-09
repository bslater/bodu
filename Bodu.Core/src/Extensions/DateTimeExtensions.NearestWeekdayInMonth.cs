// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NearestWeekdayInMonth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the nearest Monday-to-Friday date to <paramref name="dateTime" /> without crossing its calendar month.
    /// </summary>
    /// <param name="dateTime">The date and time to adjust.</param>
    /// <returns>
    /// The nearest weekday in the same month, preserving the time of day and original <see cref="DateTime.Kind" />.
    /// On equal-distance weekend ties, Saturday normally moves to Friday and Sunday to Monday, adjusted at
    /// month boundaries to remain within the month.
    /// </returns>
    /// <remarks>
    /// Weekdays mean Monday through Friday regardless of culture or locale. This does not account for public holidays.
    /// </remarks>
    public static DateTime NearestWeekdayInMonth(this DateTime dateTime)
    {
        DateOnly result = DateOnly.FromDateTime(dateTime).NearestWeekdayInMonth();
        return dateTime.AddDays(result.Day - dateTime.Day);
    }
}
