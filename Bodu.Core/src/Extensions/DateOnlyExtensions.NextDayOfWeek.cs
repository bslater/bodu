// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.NextDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next calendar occurrence of the specified <see cref="DayOfWeek" /> after
    /// the given <paramref name="date" />.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly" /> from which to search forward.</param>
    /// <param name="dayOfWeek">
    /// The desired <see cref="DayOfWeek" /> to locate. For example, <see cref="DayOfWeek.Monday" /> will return the next Monday.
    /// </param>
    /// <returns>A <see cref="DateOnly" /> representing the next occurrence of <paramref name="dayOfWeek" /> after <paramref name="date" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a valid <see cref="DayOfWeek" /> enum value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="date" /> already falls on the specified <paramref name="dayOfWeek" />, the result will be exactly 7 days
    /// later (i.e., the next calendar occurrence).
    /// </para>
    /// <para>The returned value is a pure calendar date with no time or kind metadata.</para>
    /// </remarks>
    public static DateOnly NextDayOfWeek(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return date.AddDays((((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7) is 0 ? 7 : ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7);
    }
}
