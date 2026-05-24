// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.UnixTime(Epoch).cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the calendar date corresponding to the specified Unix
    /// timestamp, expressed in milliseconds since 1970-01-01T00:00:00Z.
    /// </summary>
    /// <param name="timestamp">The number of milliseconds that have elapsed since the Unix epoch.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value representing the calendar date in UTC corresponding to
    /// <paramref name="timestamp" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="ToUnixTimeMilliseconds(DateOnly)" /> to perform the inverse conversion.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="timestamp" /> is outside the range supported for conversion to <see cref="DateOnly" />
    /// .
    /// </exception>
    /// <seealso cref="ToUnixTimeMilliseconds(DateOnly)" />
    public static DateOnly FromUnixTimeMilliseconds(long timestamp)
    {
        ThrowHelper.ThrowIfOutOfRange(timestamp, DateTimeExtensions.MinEpochMilliseconds, DateTimeExtensions.MaxEpochMilliseconds);
        return DateOnly.FromDayNumber((int)((DateTimeExtensions.UnixEpochTicks + (timestamp * DateTimeExtensions.TicksPerMillisecond)) / DateTimeExtensions.TicksPerDay));
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the calendar date corresponding to the specified Unix
    /// timestamp, expressed in seconds since 1970-01-01T00:00:00Z.
    /// </summary>
    /// <param name="timestamp">The number of seconds that have elapsed since the Unix epoch.</param>
    /// <returns>
    /// A <see cref="DateOnly" /> value representing the calendar date in UTC corresponding to
    /// <paramref name="timestamp" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="ToUnixTimeSeconds(DateOnly)" /> to perform the inverse conversion.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="timestamp" /> is outside the range supported for conversion to <see cref="DateOnly" />
    /// .
    /// </exception>
    /// <seealso cref="ToUnixTimeSeconds(DateOnly)" />
    public static DateOnly FromUnixTimeSeconds(long timestamp)
    {
        ThrowHelper.ThrowIfOutOfRange(timestamp, DateTimeExtensions.MinEpochSeconds, DateTimeExtensions.MaxEpochSeconds);
        return DateOnly.FromDayNumber((int)((DateTimeExtensions.UnixEpochTicks + (timestamp * DateTimeExtensions.TicksPerSecond)) / DateTimeExtensions.TicksPerDay));
    }

    /// <summary>
    /// Returns the number of milliseconds that have elapsed between the Unix epoch (1970-01-01T00:00:00Z) and the
    /// specified <see cref="DateOnly" />.
    /// </summary>
    /// <param name="date">The date value to convert. The value is interpreted as UTC.</param>
    /// <returns>The total number of milliseconds since the Unix epoch.</returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="FromUnixTimeMilliseconds(long)" /> to perform the inverse conversion.
    /// </para>
    /// </remarks>
    /// <seealso cref="FromUnixTimeMilliseconds(long)" />
    public static long ToUnixTimeMilliseconds(this DateOnly date) => ((date.DayNumber * DateTimeExtensions.TicksPerDay) - DateTimeExtensions.UnixEpochTicks) / DateTimeExtensions.TicksPerMillisecond;

    /// <summary>
    /// Returns the number of seconds that have elapsed between the Unix epoch (1970-01-01T00:00:00Z) and the specified
    /// <see cref="DateOnly" />.
    /// </summary>
    /// <param name="date">The date value to convert. The value is interpreted as UTC.</param>
    /// <returns>The total number of seconds since the Unix epoch.</returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="FromUnixTimeSeconds(long)" /> to perform the inverse conversion.
    /// </para>
    /// </remarks>
    /// <seealso cref="FromUnixTimeSeconds(long)" />
    public static long ToUnixTimeSeconds(this DateOnly date) => ((date.DayNumber * DateTimeExtensions.TicksPerDay) - DateTimeExtensions.UnixEpochTicks) / DateTimeExtensions.TicksPerSecond;
}
