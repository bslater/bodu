// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NextOrSameDateOfWeek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the next calendar occurrence of the specified
    /// <see cref="DayOfWeek" /> at or after the given <paramref name="dateTime" />.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search forward.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate. For example, <see cref="DayOfWeek.Monday" /> returns the next Monday on
    /// or after <paramref name="dateTime" />.
    /// </param>
    /// <returns>
    /// An object whose value is set to the next occurrence of <paramref name="dayOfWeek" /> at or after
    /// <paramref name="dateTime" />, with the original time-of-day and <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="dateTime" /> already falls on the specified <paramref name="dayOfWeek" />, the result is
    /// <paramref name="dateTime" /> itself. This is the on-or-after counterpart of
    /// <see cref="NextDateOfWeek(DateTime, DayOfWeek)" />, which always advances by at least one day.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateTime NextOrSameDateOfWeek(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return dateTime.AddTicks(GetTicksUntilNextOrSameDayOfWeek(dateTime, dayOfWeek));
    }
}
