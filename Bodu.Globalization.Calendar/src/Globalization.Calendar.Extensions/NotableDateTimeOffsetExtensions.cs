// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateTimeOffsetExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Extensions;

/// <summary>
/// Provides time-zone-aware working-day extension methods on <see cref="DateTimeOffset" />.
/// </summary>
/// <remarks>
/// <para>
/// Each method converts the supplied <see cref="DateTimeOffset" /> instant to its civil date in the supplied
/// <see cref="TimeZoneInfo" />, performs working-day math on that civil date, and re-anchors the result back to the
/// same zone preserving the original time-of-day. The resulting <see cref="DateTimeOffset.Offset" /> reflects the
/// zone's rules for the returned civil date (which may differ from the input's offset across DST transitions).
/// </para>
/// <para>
/// The <see cref="WeekPattern" /> parameter on each method is optional. When <see langword="null" />, the
/// <see cref="INotableDateService.WorkingWeek" /> configured on the supplied service is used.
/// </para>
/// </remarks>
public static partial class NotableDateTimeOffsetExtensions
{
    /// <summary>
    /// Returns the civil <see cref="DateTime" /> equivalent of <paramref name="instant" /> in the supplied
    /// <paramref name="timeZone" />.
    /// </summary>
    /// <param name="instant">The instant to convert.</param>
    /// <param name="timeZone">The destination time zone.</param>
    /// <returns>The civil <see cref="DateTime" /> in <paramref name="timeZone" />.</returns>
    private static DateTime LocalDateTimeIn(DateTimeOffset instant, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(instant, timeZone).DateTime;

    /// <summary>
    /// Returns a <see cref="DateTimeOffset" /> with the supplied civil <paramref name="localDate" /> at the original
    /// <paramref name="timeOfDay" />, anchored to the supplied <paramref name="timeZone" /> using its UTC offset for
    /// that local date.
    /// </summary>
    /// <param name="localDate">The civil date in <paramref name="timeZone" />.</param>
    /// <param name="timeOfDay">The time-of-day to apply on <paramref name="localDate" />.</param>
    /// <param name="timeZone">The zone whose offset is consulted for the resulting instant.</param>
    /// <returns>A <see cref="DateTimeOffset" /> representing the supplied civil date and time in the zone.</returns>
    private static DateTimeOffset CombineInZone(DateOnly localDate, TimeSpan timeOfDay, TimeZoneInfo timeZone)
    {
        DateTime local = localDate.ToDateTime(TimeOnly.MinValue) + timeOfDay;
        TimeSpan offset = timeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset);
    }
}
