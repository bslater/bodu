// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarThrowHelper.CallerExpression.cs" company="PlaceholderCompany". />
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Bodu.Globalization.Calendar;

internal static partial class CalendarThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="endDate" /> is earlier than
    /// <paramref name="startDate" />.
    /// </summary>
    /// <param name="startDate">The lower bound of the date range.</param>
    /// <param name="endDate">The upper bound of the date range.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    internal static void ThrowIfEndDateBeforeStartDate(
        DateTime startDate,
        DateTime endDate,
        [CallerArgumentExpression(nameof(endDate))] string? paramName = null)
    {
        if (endDate < startDate)
            throw new ArgumentException(
                CalendarResourceStrings.Arg_Invalid_EndDateBeforeStartDate,
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="value" /> is <see langword="null" />, empty, or
    /// contains only white-space characters.
    /// </summary>
    /// <param name="value">The string value to test.</param>
    /// <param name="message">The exception message describing the violated contract.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is null, empty, or white-space.
    /// </exception>
    internal static void ThrowIfNullOrWhiteSpace(
        string? value,
        string message,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(message, paramName);
    }
}

#endif
