// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NextDateOfWeek.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next calendar occurrence of the specified
    /// <see cref="DayOfWeek" /> after the given <paramref name="date" />.
    /// </summary>
    /// <param name="date">The starting date value from which to search forward.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek" /> to locate. For example, <see cref="DayOfWeek.Monday" /> returns the next Monday.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the next occurrence of <paramref name="dayOfWeek" /> following
    /// <paramref name="date" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="date" /> already falls on the specified <paramref name="dayOfWeek" />, the result is exactly
    /// seven days later. The method advances forward in time and never returns the original date.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration.
    /// </exception>
    public static DateOnly NextDateOfWeek(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return date.AddDays((((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7) is 0 ? 7 : ((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7);
    }
}
