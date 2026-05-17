// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.PreviousOccurrence.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the previous occurrence of a recurring event that starts at
    /// <paramref name="dateTime" /> and repeats every <paramref name="interval" />, occurring strictly before the
    /// specified <paramref name="before" /> timestamp.
    /// </summary>
    /// <param name="dateTime">
    /// The date and time value representing the initial reference point of the recurring event.
    /// </param>
    /// <param name="interval">
    /// The fixed <see cref="TimeSpan" /> between successive occurrences. Must be greater than
    /// <see cref="TimeSpan.Zero" />.
    /// </param>
    /// <param name="before">The point in time before which the previous occurrence must fall.</param>
    /// <returns>
    /// An object whose value is the last occurrence of the event that falls strictly before <paramref name="before" />,
    /// based on the supplied <paramref name="dateTime" /> and recurring <paramref name="interval" />, with the original
    /// <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="before" /> is earlier than or equal to <paramref name="dateTime" />, the method returns the
    /// occurrence immediately prior to <paramref name="dateTime" />. Otherwise, it computes the largest multiple of
    /// <paramref name="interval" /> added to <paramref name="dateTime" /> that remains strictly less than
    /// <paramref name="before" />. When <paramref name="before" /> falls exactly on an occurrence boundary, the
    /// occurrence at that boundary is excluded and the preceding one is returned.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code>
    ///<![CDATA[
    /// var start    = new DateTime(2025, 7, 7, 9, 0, 0);             // 09:00
    /// var interval = TimeSpan.FromHours(1);                          // every hour
    ///
    /// // before is between two occurrences — returns the occurrence at 10:00
    /// var before1  = new DateTime(2025, 7, 7, 10, 45, 0);            // 10:45
    /// var prev1    = start.PreviousOccurrence(interval, before1);    // → 10:00
    ///
    /// // before falls exactly on an occurrence — returns the one before it
    /// var before2  = new DateTime(2025, 7, 7, 11, 0, 0);             // 11:00 (on boundary)
    /// var prev2    = start.PreviousOccurrence(interval, before2);    // → 10:00
    ///]]>
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="interval" /> is less than or equal to <see cref="TimeSpan.Zero" />.
    /// </exception>
    public static DateTime PreviousOccurrence(this DateTime dateTime, TimeSpan interval, DateTime before)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        if (before <= dateTime)
            return dateTime.AddTicks(-interval.Ticks);

        var elapsedTicks = (before - dateTime).Ticks;
        var intervalTicks = interval.Ticks;

        // Use integer arithmetic to avoid floating-point imprecision.
        // When before lands exactly on an occurrence boundary, the remainder is zero and we step
        // back one interval so the result is always strictly less than before.
        var quotient = elapsedTicks / intervalTicks;
        var remainder = elapsedTicks % intervalTicks;

        var previousIntervalCount = remainder == 0 ? quotient - 1 : quotient;

        return dateTime.AddTicks(previousIntervalCount * intervalTicks);
    }
}
