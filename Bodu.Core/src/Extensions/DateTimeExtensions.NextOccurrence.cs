// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NextOccurrence.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the next occurrence of a recurring event based on a specified
    /// <paramref name="dateTime"/> time and fixed <paramref name="interval"/>, occurring strictly after the given
    /// <paramref name="after"/> timestamp.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> representing the initial reference point for the recurring event.</param>
    /// <param name="interval">The fixed <see cref="TimeSpan"/> between occurrences. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="after">A <see cref="DateTime"/> indicating the point in time after which the next occurrence should be determined.</param>
    /// <returns>
    /// An object whose value is set to the first occurrence of the event that falls strictly after <paramref name="after"/>, based on
    /// the provided <paramref name="dateTime"/> and recurring <paramref name="interval"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="interval"/> is less than or equal to <see cref="TimeSpan.Zero"/>.</exception>
    /// <remarks>
    /// <para>If <paramref name="after"/> is earlier than or equal to <paramref name="dateTime"/>, the method returns <paramref name="dateTime"/>.</para>
    /// <para>
    /// Otherwise, the method computes the smallest multiple of <paramref name="interval"/> added to <paramref name="dateTime"/> that
    /// occurs after <paramref name="after"/>.
    /// </para>
    /// <para>
    /// The <see cref="DateTime.Kind"/> property of the returned instance matches that of the original <paramref name="dateTime"/>.
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var start = new DateTime(2025, 7, 7, 9, 0, 0);           // 09:00
    /// var interval = TimeSpan.FromHours(1);                    // Every hour
    /// var after = new DateTime(2025, 7, 7, 10, 45, 0);         // 10:45
    /// var next = start.NextOccurrence(interval, after);        // 11:00
    ///]]>
    /// </code>
    /// </remarks>
    public static DateTime NextOccurrence(this DateTime dateTime, TimeSpan interval, DateTime after)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        if (after <= dateTime)
            return dateTime;

        // Calculate how many full intervals fit between start and after
        double intervalsPassed = (double)(after - dateTime).Ticks / interval.Ticks;
        long nextIntervalCount = (long)Math.Ceiling(intervalsPassed);

        return dateTime.AddTicks(nextIntervalCount * interval.Ticks);
    }
}
