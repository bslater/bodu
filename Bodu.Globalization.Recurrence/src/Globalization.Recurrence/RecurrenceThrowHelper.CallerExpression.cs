// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceThrowHelper.CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Globalization.Recurrence;

internal static partial class RecurrenceThrowHelper
{
    /// <summary>
    /// Throws when <paramref name="interval" /> is not a positive integer.
    /// </summary>
    /// <param name="interval">The candidate recurrence interval to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="interval" /> is less than one.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfInvalidInterval(
        int interval,
        [CallerArgumentExpression(nameof(interval))] string? paramName = null)
    {
        if (interval < 1)
            throw new ArgumentOutOfRangeException(
                paramName,
                interval,
                RecurrenceResourceStrings.Arg_OutOfRange_RecurrenceInterval);
    }

    /// <summary>
    /// Throws when <paramref name="count" /> is specified but not a positive integer.
    /// </summary>
    /// <param name="count">The candidate recurrence count to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count" /> has a value less than one.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfInvalidCount(
        int? count,
        [CallerArgumentExpression(nameof(count))] string? paramName = null)
    {
        if (count is < 1)
            throw new ArgumentOutOfRangeException(
                paramName,
                count,
                RecurrenceResourceStrings.Arg_OutOfRange_RecurrenceCount);
    }

    /// <summary>
    /// Throws when <paramref name="interval" /> is not a positive whole number of seconds.
    /// </summary>
    /// <param name="interval">The candidate anchored interval to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception; inferred from the call site.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="interval" /> is zero or negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="interval" /> carries sub-second ticks, which the canonical iCalendar duration text
    /// cannot represent.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfInvalidAnchoredInterval(
        TimeSpan interval,
        [CallerArgumentExpression(nameof(interval))] string? paramName = null)
    {
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                paramName,
                interval,
                RecurrenceResourceStrings.Arg_OutOfRange_AnchoredIntervalNotPositive);

        if (interval.Ticks % TimeSpan.TicksPerSecond != 0)
            throw new ArgumentException(
                RecurrenceResourceStrings.Arg_Invalid_AnchoredIntervalSubSecond,
                paramName);
    }

    /// <summary>
    /// Throws a <see cref="FormatException" /> reporting that <paramref name="format" /> is not a supported specifier.
    /// </summary>
    /// <param name="format">The unsupported format specifier supplied by the caller.</param>
    /// <exception cref="FormatException">Always thrown.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DoesNotReturn]
    internal static void ThrowRecurrenceRuleFormatUnsupported(ReadOnlySpan<char> format) =>
        throw new FormatException(
            string.Format(
                CultureInfo.CurrentCulture,
                RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleFormat,
                format.ToString()));
}
