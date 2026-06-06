// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Provides working-day, traversal, and notable-date query extension methods over <see cref="DateTimeOffset" />,
/// delegating to the <see cref="DateOnly" /> surface on the offset-local date component.
/// </summary>
/// <remarks>
/// <para>
/// The date component is the offset-local wall-clock date (<see cref="DateTimeOffset.DateTime" />). Traversal methods
/// that return a moved <see cref="DateTimeOffset" /> preserve the original time-of-day and
/// <see cref="DateTimeOffset.Offset" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// INotableDateService service = AmericasCalendarData.CreateService("US");
/// DateTimeOffset stamp = new(2026, 7, 3, 14, 0, 0, TimeSpan.FromHours(-5)); // observed Independence Day
///
/// bool working = stamp.IsWorkingDay(service, "US"); // false
///
/// // The next working day keeps the original time-of-day and -05:00 offset.
/// DateTimeOffset resume = stamp.NextWorkingDay(service, "US");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateOnlyExtensions" /> <seealso cref="INotableDateService" />
/// <seealso href="../guides/calendar/working-days.html">Working-day arithmetic (guide)</seealso>
public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Reattaches the time-of-day and offset of an original <see cref="DateTimeOffset" /> to a computed date.
    /// </summary>
    /// <param name="original">The original value supplying the time-of-day and offset.</param>
    /// <param name="date">The computed date.</param>
    /// <returns>The computed date combined with the original time-of-day and offset.</returns>
    private static DateTimeOffset WithTimeOf(DateTimeOffset original, DateOnly date) =>
        new(date.ToDateTime(TimeOnly.FromTimeSpan(original.TimeOfDay)), original.Offset);
}
