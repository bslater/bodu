// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.NextOccurrence.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Calculates the next occurrence of a periodic event based on a given start date and recurrence interval, relative to a specified
    /// point in time.
    /// </summary>
    /// <param name="start">The <see cref="DateOnly" /> representing the initial start date of the recurring event.</param>
    /// <param name="intervalDays">The number of days between each recurrence. Must be greater than zero.</param>
    /// <param name="after">A <see cref="DateOnly" /> after which the next occurrence should be found.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> representing the next occurrence of the event after the specified <paramref name="after" /> date.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="intervalDays" /> is less than or equal to zero.</exception>
    /// <remarks>
    /// The returned date is aligned with the <paramref name="start" /> date and repeats every <paramref name="intervalDays" /> days.
    /// </remarks>
    /// <example>
    /// The following example finds the next occurrence of an event that starts on January 1, 2024 and repeats every 5 days, after a
    /// given reference date:
    /// <code>
    ///<![CDATA[
    /// var start = new DateOnly(2024, 1, 1);
    /// var after = new DateOnly(2024, 1, 12);
    /// int intervalDays = 5;
    ///
    /// var next = start.NextOccurrence(intervalDays, after);
    /// // Output: 2024-01-16
    ///]]>
    /// </code>
    /// </example>
    public static DateOnly NextOccurrence(this DateOnly start, int intervalDays, DateOnly after)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(intervalDays, 0);

        if (after < start)
            return start;

        int daysPassed = after.DayNumber - start.DayNumber;
        int intervalsPassed = (daysPassed + intervalDays) / intervalDays;
        return start.AddDays(intervalsPassed * intervalDays);
    }
}
