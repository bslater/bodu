// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.PreviousWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the previous calendar weekday before the given <paramref name="date"/>,
    /// based on the specified <paramref name="weekend"/> definition.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly"/> from which to search backward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that defines which days are considered weekends.</param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the previous weekday prior to <paramref name="date"/>, excluding days defined as
    /// weekends by <paramref name="weekend"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a valid <see cref="CalendarWeekendDefinition"/> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="date"/> falls on a weekday, the result will be the previous calendar day that is also a weekday according to
    /// the specified <paramref name="weekend"/> pattern.
    /// </para>
    /// <para>The returned value does not include any time or kind metadata and is purely a calendar date.</para>
    /// </remarks>
    public static DateOnly PreviousWeekday(this DateOnly date, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        int dayNumber = date.DayNumber;
        do
        {
            dayNumber -= 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), weekend));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the previous calendar weekday before the given <paramref name="date"/>,
    /// based on the specified <paramref name="weekend"/> definition and optional <paramref name="provider"/>.
    /// </summary>
    /// <param name="date">The starting <see cref="DateOnly"/> from which to search backward.</param>
    /// <param name="weekend">The <see cref="CalendarWeekendDefinition"/> that defines the default pattern of weekend days.</param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider"/> that can override the weekend definition with custom logic. If
    /// <c>null</c>, the default behaviour based on <paramref name="weekend"/> is used.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the previous weekday according to the specified <paramref name="weekend"/> definition
    /// and optional <paramref name="provider"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a valid <see cref="CalendarWeekendDefinition"/> value.
    /// </exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="date"/> falls on a weekday, the result will be the previous calendar day that is also a weekday according to
    /// the specified <paramref name="weekend"/> pattern or the custom rules provided by <paramref name="provider"/>.
    /// </para>
    /// <para>The returned value is a pure calendar date and does not include any time or kind metadata.</para>
    /// </remarks>
    public static DateOnly PreviousWeekday(this DateOnly date, CalendarWeekendDefinition weekend, IWeekendDefinitionProvider? provider)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);

        int dayNumber = date.DayNumber;
        do
        {
            dayNumber -= 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), weekend, provider));

        return DateOnly.FromDayNumber(dayNumber);
    }
}