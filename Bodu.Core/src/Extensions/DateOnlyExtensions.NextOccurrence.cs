// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next occurrence of a recurring event that starts at
    /// <paramref name="start" /> and repeats every <paramref name="intervalDays" /> days, occurring strictly after the
    /// specified <paramref name="after" /> date.
    /// </summary>
    /// <param name="start">The date value representing the initial reference point of the recurring event.</param>
    /// <param name="intervalDays">
    /// The fixed number of days between successive occurrences. Must be greater than zero.
    /// </param>
    /// <param name="after">The date after which the next occurrence must fall.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value representing the first occurrence of the event aligned with
    /// <paramref name="start" /> and recurring every <paramref name="intervalDays" /> days that falls strictly after
    /// <paramref name="after" />, or <paramref name="start" /> when <paramref name="after" /> is on or before it.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="after" /> is earlier than or equal to <paramref name="start" />, the method returns
    /// <paramref name="start" />, as <see cref="DateTimeExtensions.NextOccurrence(DateTime, TimeSpan, DateTime)" />
    /// does. Otherwise, it computes the smallest multiple of <paramref name="intervalDays" /> added to
    /// <paramref name="start" /> that occurs strictly after <paramref name="after" />. When <paramref name="after" />
    /// falls exactly on an occurrence, that occurrence is excluded and the following one is returned.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var start = new DateOnly(2024, 1, 1);
    /// var after = new DateOnly(2024, 1, 12);
    /// var next = start.NextOccurrence(intervalDays: 5, after); // → 2024-01-16
    ///]]>
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="intervalDays" /> is less than or equal to zero.
    /// </exception>
    public static DateOnly NextOccurrence(this DateOnly start, int intervalDays, DateOnly after)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(intervalDays, 0);

        if (after <= start)
            return start;

        int daysPassed = after.DayNumber - start.DayNumber;
        int intervalsPassed = (daysPassed + intervalDays) / intervalDays;
        return start.AddDays(intervalsPassed * intervalDays);
    }
}
