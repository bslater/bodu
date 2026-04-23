// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.PreviousDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the most recent occurrence of the specified <see cref="DayOfWeek"/> prior to the
    /// given <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly"/> from which to search backward.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate. For example, <see cref="DayOfWeek.Friday"/> returns the previous Friday before the given date.
    /// </param>
    /// <returns>A <see cref="DateOnly"/> representing the most recent occurrence of <paramref name="dayOfWeek"/> prior to <paramref name="date"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="date"/> already falls on the specified <paramref name="dayOfWeek"/>, the result will be exactly 7 days earlier
    /// (the previous calendar occurrence).
    /// </para>
    /// <para>The returned value is a calendar date and does not include any time or kind metadata.</para>
    /// </remarks>
    public static DateOnly PreviousDayOfWeek(this DateOnly date, DayOfWeek dayOfWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return date.AddDays(-(((7 + date.DayOfWeek - dayOfWeek) % 7) switch { 0 => 7, int d => d }));
    }
}
