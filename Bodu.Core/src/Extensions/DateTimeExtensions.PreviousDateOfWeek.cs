// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.PreviousDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the previous calendar occurrence of the specified
    /// <see cref="DayOfWeek" /> before the given <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search backward.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate. For example, <see cref="DayOfWeek.Monday" /> returns the previous
    /// Monday.
    /// </param>
    /// <returns>
    /// An object whose value is set to the previous occurrence of <paramref name="dayOfWeek" /> preceding
    /// <paramref name="dateTime" />, with the original time-of-day and <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime" /> already falls on the specified <paramref name="dayOfWeek" />, the result is
    /// exactly seven days earlier. The method moves backward in time and never returns the original date.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateTime PreviousDateOfWeek(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return dateTime.AddTicks(
            dateTime.DayOfWeek == dayOfWeek
                ? -TicksPerWeek
                : GetTicksToPreviousDayOfWeek(dateTime, dayOfWeek));
    }
}
