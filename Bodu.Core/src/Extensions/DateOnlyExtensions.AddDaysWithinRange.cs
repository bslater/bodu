// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.AddDaysWithinRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the date a signed number of days from <paramref name="date" />, throwing an exception that names the
    /// caller's date parameter when the result lies outside the range of <see cref="DateOnly" />.
    /// </summary>
    /// <param name="date">The date to move from.</param>
    /// <param name="days">The signed number of days to move, at most seven in either direction.</param>
    /// <param name="paramName">The name of the caller's parameter that supplied <paramref name="date" />.</param>
    /// <returns>The date <paramref name="days" /> days from <paramref name="date" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The result is earlier than <see cref="DateOnly.MinValue" /> or later than <see cref="DateOnly.MaxValue" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DateOnly AddDaysWithinRange(DateOnly date, int days, string paramName)
    {
        int dayNumber = date.DayNumber + days;
        if ((uint)dayNumber > (uint)DateOnly.MaxValue.DayNumber)
            DateTimeExtensions.ThrowResultOutsideRange(paramName, nameof(DateOnly));

        return DateOnly.FromDayNumber(dayNumber);
    }
}
