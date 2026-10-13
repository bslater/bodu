// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.AddTicksWithinRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns the value a signed number of ticks from <paramref name="dateTime" />, throwing an exception that names
    /// the caller's date parameter when the result lies outside the range of <see cref="DateTime" />.
    /// </summary>
    /// <param name="dateTime">The value to move from.</param>
    /// <param name="ticks">The signed number of ticks to move, at most one week in either direction.</param>
    /// <param name="paramName">The name of the caller's parameter that supplied <paramref name="dateTime" />.</param>
    /// <returns>
    /// The value <paramref name="ticks" /> ticks from <paramref name="dateTime" />, with its
    /// <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The result is earlier than <see cref="DateTime.MinValue" /> or later than <see cref="DateTime.MaxValue" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static DateTime AddTicksWithinRange(DateTime dateTime, long ticks, string paramName)
    {
        if ((ulong)(dateTime.Ticks + ticks) > (ulong)DateTime.MaxValue.Ticks)
            ThrowResultOutsideRange(paramName, nameof(DateTime));

        return dateTime.AddTicks(ticks);
    }

    /// <summary>
    /// Throws the <see cref="ArgumentOutOfRangeException" /> reported when a date computed from an argument lies
    /// outside the range of its type.
    /// </summary>
    /// <param name="paramName">The name of the parameter that supplied the date.</param>
    /// <param name="typeName">The name of the date type whose range the result leaves.</param>
    /// <exception cref="ArgumentOutOfRangeException">Always thrown.</exception>
    [DoesNotReturn]
    internal static void ThrowResultOutsideRange(string paramName, string typeName) =>
        throw new ArgumentOutOfRangeException(
            paramName,
            string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, typeName));
}
