// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarThrowHelper.NetStandard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

internal static partial class CalendarThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="endDate" /> is earlier than
    /// <paramref name="startDate" />.
    /// </summary>
    /// <param name="startDate">The lower bound of the date range.</param>
    /// <param name="endDate">The upper bound of the date range.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="endDate" /> is earlier than <paramref name="startDate" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfEndDateBeforeStartDate(DateTime startDate, DateTime endDate, string? paramName = null)
    {
        if (endDate < startDate)
            throw new ArgumentException(
                CalendarResourceStrings.Arg_Invalid_EndDateBeforeStartDate,
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="key" /> is <see langword="null" />, empty, or
    /// contains only white-space characters, using the standard registry-key message.
    /// </summary>
    /// <param name="key">The registry key value to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="key" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfKeyNullOrWhiteSpace(string? key, string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException(
                CalendarResourceStrings.Arg_Invalid_KeyNullOrWhiteSpace,
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when <paramref name="anchorRuleName" /> is <see langword="null" />,
    /// empty, or contains only white-space characters.
    /// </summary>
    /// <param name="anchorRuleName">The anchor-rule name to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="anchorRuleName" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfAnchorRuleNameNullOrWhiteSpace(string? anchorRuleName, string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(anchorRuleName))
            throw new ArgumentException(
                CalendarResourceStrings.Arg_Invalid_AnchorRuleNameNullOrWhiteSpace,
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> when <paramref name="year" /> is outside the supported
    /// range <c>[1, 9999]</c>.
    /// </summary>
    /// <param name="year">The year value to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="year" /> is less than 1 or greater than 9999.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfYearOutOfRange(int year, string? paramName = null)
    {
        if (year < 1 || year > 9999)
            throw new ArgumentOutOfRangeException(
                paramName,
                year,
                CalendarResourceStrings.Arg_OutOfRange_Year);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> when <paramref name="workingWeek" /> is not a defined
    /// member of <see cref="WorkingDaysOfWeek" />.
    /// </summary>
    /// <param name="workingWeek">The working-week pattern to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingWeek" /> is not a defined <see cref="WorkingDaysOfWeek" /> value.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfWorkingWeekUndefined(WorkingDaysOfWeek workingWeek, string? paramName = null)
    {
        if (!Enum.IsDefined(typeof(WorkingDaysOfWeek), workingWeek))
            throw new ArgumentOutOfRangeException(
                paramName,
                workingWeek,
                CalendarResourceStrings.Arg_OutOfRange_WorkingWeekUndefined);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> when <paramref name="workingWeek" /> selects no days.
    /// </summary>
    /// <param name="workingWeek">The working-week pattern to validate.</param>
    /// <param name="paramName">The parameter name reported in the exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingWeek" /> selects no days.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfWorkingWeekEmpty(WeekPattern workingWeek, string? paramName = null)
    {
        if (workingWeek.Count == 0)
            throw new ArgumentOutOfRangeException(
                paramName,
                CalendarResourceStrings.Arg_OutOfRange_WorkingWeekEmpty);
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException" /> when <paramref name="calendar" /> is not <see langword="null" />
    /// and is not an instance of <paramref name="supportedCalendarType" />.
    /// </summary>
    /// <param name="calendar">The calendar to validate; <see langword="null" /> is accepted as the provider default.</param>
    /// <param name="providerType">The Easter provider type that issued the validation, used in the exception message.</param>
    /// <param name="supportedCalendarType">The single calendar type accepted by <paramref name="providerType" />.</param>
    /// <exception cref="NotSupportedException">
    /// Thrown when <paramref name="calendar" /> is not <see langword="null" /> and is not assignable to
    /// <paramref name="supportedCalendarType" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfUnsupportedCalendarType(
        SysGlobal.Calendar? calendar,
        Type providerType,
        Type supportedCalendarType)
    {
        if (calendar is null || supportedCalendarType.IsInstanceOfType(calendar))
            return;

        throw new NotSupportedException(
            string.Format(
                CultureInfo.InvariantCulture,
                CalendarResourceStrings.Op_NotSupported_CalendarType,
                calendar.GetType().FullName,
                providerType.Name,
                supportedCalendarType.FullName));
    }
}

#endif
