// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NextDateOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the next calendar occurrence of the specified <see cref="DayOfWeek"/> after the given <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search forward.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate. For example, <see cref="DayOfWeek.Monday"/> returns the next Monday.</param>
    /// <returns>An object whose value is set to the next occurrence of <paramref name="dayOfWeek"/> following <paramref name="dateTime"/>, with the original time-of-day and <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>If <paramref name="dateTime"/> already falls on the specified <paramref name="dayOfWeek"/>, the result is exactly seven days later. The method advances forward in time and never returns the original date.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateTime NextDateOfWeek(this DateTime dateTime, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return dateTime.AddTicks(
            dateTime.DayOfWeek == dayOfWeek
                ? TicksPerWeek
                : GetTicksUntilNextOrSameDayOfWeek(dateTime, dayOfWeek));
    }
}
