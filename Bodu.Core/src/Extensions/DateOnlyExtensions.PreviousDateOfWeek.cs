// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.PreviousDateOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the previous calendar occurrence of the specified <see cref="DayOfWeek"/> before the given <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The starting date value from which to search backward.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate. For example, <see cref="DayOfWeek.Monday"/> returns the previous Monday.</param>
    /// <returns>A <see cref="DateOnly"/> value set to the previous occurrence of <paramref name="dayOfWeek"/> preceding <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>If <paramref name="date"/> already falls on the specified <paramref name="dayOfWeek"/>, the result is exactly seven days earlier. The method moves backward in time and never returns the original date.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.</exception>
    public static DateOnly PreviousDateOfWeek(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return date.AddDays(-(((7 + date.DayOfWeek - dayOfWeek) % 7) switch { 0 => 7, int d => d }));
    }
}
