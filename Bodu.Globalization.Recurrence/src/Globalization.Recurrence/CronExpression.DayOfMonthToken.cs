// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.DayOfMonthToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Extensions;

namespace Bodu.Globalization.Recurrence;

public sealed partial class CronExpression
{
    /// <summary>
    /// Represents a Quartz token that stands for the whole day-of-month field, and decides which day of a month it
    /// selects.
    /// </summary>
    /// <param name="Day">
    /// The days before the last day of the month when <paramref name="FromEnd" /> is set (<c>L-n</c>, <c>L-nW</c>, zero
    /// for <c>L</c> and <c>LW</c>); otherwise the day of the month (<c>nW</c>).
    /// </param>
    /// <param name="FromEnd">
    /// <see langword="true" /> when <paramref name="Day" /> counts back from the last day of the month (<c>L</c>
    /// forms); <see langword="false" /> when it counts from the first (<c>nW</c>).
    /// </param>
    /// <param name="NearestWeekday">
    /// <see langword="true" /> when the token selects the weekday nearest the day it names (a trailing <c>W</c>);
    /// otherwise <see langword="false" />.
    /// </param>
    /// <remarks>
    /// The record's value equality is what <see cref="CronExpression.Equals(CronExpression)" /> compares, so a token is
    /// stored in its canonical form: <c>L-0</c> as <c>L</c>. A token never counts from the first day without
    /// <paramref name="NearestWeekday" />, since a plain day is a value rather than a token.
    /// </remarks>
    private readonly record struct DayOfMonthToken(int Day, bool FromEnd, bool NearestWeekday)
    {
        /// <summary>
        /// Determines whether the token selects a day.
        /// </summary>
        /// <param name="day">An instant within the day.</param>
        /// <returns>
        /// <see langword="true" /> when the token selects the day; otherwise <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// A token that names no day of a month, such as <c>L-30</c> in a month of thirty days or less or <c>30W</c> in
        /// February, selects nothing in that month.
        /// </remarks>
        internal bool Matches(DateTime day)
        {
            int daysInMonth = DateTime.DaysInMonth(day.Year, day.Month);
            int target = FromEnd ? daysInMonth - Day : Day;
            if (!NearestWeekday)
            {
                return day.Day == target;
            }

            if (target < 1 || target > daysInMonth)
            {
                return false;
            }

            return day.Day == new DateOnly(day.Year, day.Month, target).NearestWeekdayInMonth().Day;
        }

        /// <summary>
        /// Returns the token's canonical text.
        /// </summary>
        /// <returns>The text, with no zero offset.</returns>
        internal string Format() =>
            (FromEnd, NearestWeekday) switch
            {
                (true, false) => Day == 0 ? "L" : string.Create(CultureInfo.InvariantCulture, $"L-{Day}"),
                (true, true) => Day == 0 ? "LW" : string.Create(CultureInfo.InvariantCulture, $"L-{Day}W"),
                _ => string.Create(CultureInfo.InvariantCulture, $"{Day}W"),
            };
    }
}
