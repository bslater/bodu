// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides working-day, traversal, and notable-date query extension methods over <see cref="DateTime" />, delegating
/// to the <see cref="DateOnly" /> surface on the date component.
/// </summary>
/// <remarks>
/// <para>
/// Predicate and query methods evaluate the date component only. Traversal methods that return a moved
/// <see cref="DateTime" /> preserve the original time-of-day and <see cref="DateTime.Kind" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
/// DateTime stamp = new(2026, 12, 25, 9, 30, 0, DateTimeKind.Local); // Christmas morning
///
/// bool working = stamp.IsWorkingDay(service, "US"); // false
///
/// // The next working day keeps the original 09:30 local time-of-day.
/// DateTime resume = stamp.NextWorkingDay(service, "US");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateOnlyExtensions" /> <seealso cref="INotableDateService" />
/// <seealso href="../guides/calendar/working-days.html">Working-day arithmetic (guide)</seealso>
public static partial class NotableDateTimeExtensions
{
    /// <summary>
    /// Reattaches the time-of-day and kind of an original <see cref="DateTime" /> to a computed date.
    /// </summary>
    /// <param name="original">The original value supplying the time-of-day and kind.</param>
    /// <param name="date">The computed date.</param>
    /// <returns>The computed date combined with the original time-of-day and kind.</returns>
    private static DateTime WithTimeOf(DateTime original, DateOnly date) =>
        date.ToDateTime(TimeOnly.FromDateTime(original), original.Kind);
}
