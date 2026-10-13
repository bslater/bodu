// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.TryToDateTimeOffset.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>The largest offset from UTC, in ticks, that a <see cref="DateTimeOffset" /> accepts: 14 hours.</summary>
    private const long MaxOffsetTicks = 14 * TimeSpan.TicksPerHour;

    /// <summary>
    /// Attempts to create a <see cref="DateTimeOffset" /> with the same wall-clock value as
    /// <paramref name="dateTime" /> and the supplied <paramref name="offset" />.
    /// </summary>
    /// <param name="dateTime">The wall-clock date and time to convert.</param>
    /// <param name="offset">The UTC offset to assign.</param>
    /// <param name="result">The converted value on success; otherwise the default value.</param>
    /// <returns>
    /// <see langword="true" /> when the requested offset is valid, compatible with <see cref="DateTime.Kind" />, and
    /// the resulting UTC instant is representable; otherwise <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is the non-throwing counterpart of <see cref="ToDateTimeOffset(DateTime, TimeSpan)" />. As with the
    /// constructor, <see cref="DateTimeKind.Unspecified" /> permits an explicit fixed offset,
    /// <see cref="DateTimeKind.Utc" /> requires a zero offset, and <see cref="DateTimeKind.Local" /> requires the
    /// corresponding system-local offset. No offset is inferred from the machine when the date is Unspecified. The
    /// offset must also be a whole number of minutes no more than 14 hours either side of zero. Each rule is checked
    /// before the value is built, so a conversion that fails throws and catches nothing and allocates nothing.
    /// </remarks>
    public static bool TryToDateTimeOffset(this DateTime dateTime, TimeSpan offset, out DateTimeOffset result)
    {
        long offsetTicks = offset.Ticks;
        bool offsetFitsKind = dateTime.Kind switch
        {
            DateTimeKind.Utc => offsetTicks == 0,
            DateTimeKind.Local => offset == TimeZoneInfo.Local.GetUtcOffset(dateTime),
            _ => true,
        };

        bool valid = offsetFitsKind
            && offsetTicks % TimeSpan.TicksPerMinute == 0
            && offsetTicks >= -MaxOffsetTicks
            && offsetTicks <= MaxOffsetTicks
            && (ulong)(dateTime.Ticks - offsetTicks) <= (ulong)DateTime.MaxValue.Ticks;

        result = valid ? new DateTimeOffset(dateTime, offset) : default;
        return valid;
    }
}
