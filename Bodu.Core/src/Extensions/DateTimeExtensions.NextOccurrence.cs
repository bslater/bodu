// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the next occurrence of a recurring event that starts at
    /// <paramref name="dateTime" /> and repeats every <paramref name="interval" />, occurring strictly after the
    /// specified <paramref name="after" /> timestamp.
    /// </summary>
    /// <param name="dateTime">
    /// The date and time value representing the initial reference point of the recurring event.
    /// </param>
    /// <param name="interval">
    /// The fixed <see cref="TimeSpan" /> between successive occurrences. Must be greater than
    /// <see cref="TimeSpan.Zero" />.
    /// </param>
    /// <param name="after">The point in time after which the next occurrence must fall.</param>
    /// <returns>
    /// An object whose value is the first occurrence of the event that falls strictly after <paramref name="after" />,
    /// based on the supplied <paramref name="dateTime" /> and recurring <paramref name="interval" />, with the original
    /// <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="after" /> is earlier than or equal to <paramref name="dateTime" />, the method returns
    /// <paramref name="dateTime" />. Otherwise, it computes the smallest multiple of <paramref name="interval" /> added
    /// to <paramref name="dateTime" /> that occurs strictly after <paramref name="after" />. When
    /// <paramref name="after" /> falls exactly on an occurrence boundary, that occurrence is excluded and the following
    /// one is returned.
    /// </para>
    /// <para>
    /// <b>Example:</b>
    /// </para>
    /// <code language="csharp">
    ///<![CDATA[
    /// var start = new DateTime(2025, 7, 7, 9, 0, 0);          // 09:00
    /// var interval = TimeSpan.FromHours(1);                    // every hour
    /// var after = new DateTime(2025, 7, 7, 10, 45, 0);         // 10:45
    /// var next = start.NextOccurrence(interval, after);        // → 11:00
    ///]]>
    /// </code>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="interval" /> is less than or equal to <see cref="TimeSpan.Zero" />.
    /// </exception>
    public static DateTime NextOccurrence(this DateTime dateTime, TimeSpan interval, DateTime after)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);

        if (after <= dateTime)
            return dateTime;

        long elapsedTicks = (after - dateTime).Ticks;
        long intervalTicks = interval.Ticks;

        // Use integer arithmetic to avoid floating-point imprecision on long spans (a double mantissa
        // cannot represent tick counts past ~28.5 years exactly). Stepping to quotient + 1 also honours
        // the "strictly after" contract: when after lands exactly on an occurrence boundary the
        // remainder is zero and the following occurrence is returned, never after itself.
        long nextIntervalCount = (elapsedTicks / intervalTicks) + 1;

        return dateTime.AddTicks(nextIntervalCount * intervalTicks);
    }
}
