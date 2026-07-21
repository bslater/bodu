// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.PreviousOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the previous occurrence of a recurring event that starts at
    /// <paramref name="start" /> and repeats every <paramref name="intervalDays" /> days, occurring strictly before the
    /// specified <paramref name="before" /> date.
    /// </summary>
    /// <param name="start">The date value representing the initial reference point of the recurring event.</param>
    /// <param name="intervalDays">
    /// The fixed number of days between successive occurrences. Must be greater than zero.
    /// </param>
    /// <param name="before">The date before which the previous occurrence must fall.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value representing the last occurrence of the event that falls strictly before
    /// <paramref name="before" />, based on the supplied <paramref name="start" /> and recurring
    /// <paramref name="intervalDays" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="before" /> is earlier than or equal to <paramref name="start" />, the method returns the
    /// occurrence immediately prior to <paramref name="start" />. Otherwise, it computes the largest multiple of
    /// <paramref name="intervalDays" /> added to <paramref name="start" /> that remains strictly earlier than
    /// <paramref name="before" />. When <paramref name="before" /> falls exactly on an occurrence boundary, the
    /// occurrence at that boundary is excluded and the preceding one is returned.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var start = new DateOnly(2024, 1, 1);
    ///
    /// // before is between two occurrences — returns the occurrence at 2024-01-11
    /// var prev1 = start.PreviousOccurrence(intervalDays: 5, new DateOnly(2024, 1, 13)); // → 2024-01-11
    ///
    /// // before falls exactly on an occurrence — returns the one before it
    /// var prev2 = start.PreviousOccurrence(intervalDays: 5, new DateOnly(2024, 1, 11)); // → 2024-01-06
    ///]]>
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="intervalDays" /> is less than or equal to zero.
    /// </exception>
    public static DateOnly PreviousOccurrence(this DateOnly start, int intervalDays, DateOnly before)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(intervalDays, 0);

        if (before <= start)
            return start.AddDays(-intervalDays);

        int elapsedDays = before.DayNumber - start.DayNumber;

        // Use integer arithmetic so a boundary hit steps back one full interval: when before lands exactly on an
        // occurrence, the remainder is zero and the result must remain strictly earlier than before.
        int quotient = elapsedDays / intervalDays;
        int remainder = elapsedDays % intervalDays;

        int previousIntervalCount = remainder == 0 ? quotient - 1 : quotient;

        return start.AddDays(previousIntervalCount * intervalDays);
    }
}
