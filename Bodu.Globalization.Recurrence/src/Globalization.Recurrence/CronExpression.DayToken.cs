// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.DayToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public sealed partial class CronExpression
{
    /// <summary>
    /// Represents a Quartz token that stands for a whole day field, and decides which days of a month it selects.
    /// </summary>
    /// <param name="Kind">The kind of token, or <see cref="DayTokenKind.None" /> when the field holds values.</param>
    /// <param name="Value">
    /// The days before the last day for <c>L-n</c> and <c>L-nW</c>, the day of the month for <c>nW</c>, or the weekday,
    /// Sunday as zero, for <c>dL</c> and <c>d#k</c>.
    /// </param>
    /// <param name="Ordinal">The ordinal k of a <c>d#k</c> token, from one to five; otherwise zero.</param>
    /// <remarks>
    /// The record's value equality is what <see cref="CronExpression.Equals(CronExpression)" /> compares, so a token is
    /// stored in its canonical form: <c>L-0</c> as <c>L</c>, and the weekday seven as zero.
    /// </remarks>
    private readonly record struct DayToken(DayTokenKind Kind, int Value, int Ordinal)
    {
        /// <summary>
        /// Gets a value indicating whether the field holds values rather than a token.
        /// </summary>
        /// <value><see langword="true" /> when the field holds no token; otherwise <see langword="false" />.</value>
        internal bool IsNone =>
            Kind == DayTokenKind.None;

        /// <summary>
        /// Determines whether the token selects a day.
        /// </summary>
        /// <param name="day">An instant within the day.</param>
        /// <returns>
        /// <see langword="true" /> when the token selects the day; otherwise <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// A token that names no day of a month, such as <c>L-30</c> in a month of thirty days or less, <c>30W</c> in
        /// February, or a fifth Tuesday a month does not have, selects nothing in that month.
        /// </remarks>
        internal bool Matches(DateTime day)
        {
            int daysInMonth = DateTime.DaysInMonth(day.Year, day.Month);
            return Kind switch
            {
                DayTokenKind.LastDayOfMonth => day.Day == daysInMonth - Value,
                DayTokenKind.WeekdayNearestLastDay =>
                    daysInMonth - Value >= 1 && day.Day == NearestWeekday(day, daysInMonth - Value, daysInMonth),
                DayTokenKind.WeekdayNearestDay =>
                    Value <= daysInMonth && day.Day == NearestWeekday(day, Value, daysInMonth),
                DayTokenKind.LastWeekdayOfMonth => (int)day.DayOfWeek == Value && day.Day + 7 > daysInMonth,
                DayTokenKind.NthWeekdayOfMonth => (int)day.DayOfWeek == Value && ((day.Day - 1) / 7) + 1 == Ordinal,
                _ => false,
            };
        }

        /// <summary>
        /// Returns the token's canonical text.
        /// </summary>
        /// <returns>The text, with the weekday as a number and no zero offset.</returns>
        internal string Format() =>
            Kind switch
            {
                DayTokenKind.LastDayOfMonth => Value == 0 ? "L" : string.Create(CultureInfo.InvariantCulture, $"L-{Value}"),
                DayTokenKind.WeekdayNearestLastDay => Value == 0 ? "LW" : string.Create(CultureInfo.InvariantCulture, $"L-{Value}W"),
                DayTokenKind.WeekdayNearestDay => string.Create(CultureInfo.InvariantCulture, $"{Value}W"),
                DayTokenKind.LastWeekdayOfMonth => string.Create(CultureInfo.InvariantCulture, $"{Value}L"),
                DayTokenKind.NthWeekdayOfMonth => string.Create(CultureInfo.InvariantCulture, $"{Value}#{Ordinal}"),
                _ => string.Empty,
            };

        /// <summary>
        /// Returns the weekday nearest a day of a month, without leaving the month.
        /// </summary>
        /// <param name="anyDayOfMonth">An instant within the month.</param>
        /// <param name="target">The day of the month the weekday is nearest.</param>
        /// <param name="daysInMonth">The number of days in the month.</param>
        /// <returns>
        /// The target when it is a weekday; for a Saturday, the Friday before it, or the Monday after it when the
        /// Saturday is the first; for a Sunday, the Monday after it, or the Friday before it when the Sunday is the
        /// last.
        /// </returns>
        private static int NearestWeekday(DateTime anyDayOfMonth, int target, int daysInMonth) =>
            new DateTime(anyDayOfMonth.Year, anyDayOfMonth.Month, target).DayOfWeek switch
            {
                DayOfWeek.Saturday => target == 1 ? 3 : target - 1,
                DayOfWeek.Sunday => target == daysInMonth ? target - 2 : target + 1,
                _ => target,
            };
    }
}
