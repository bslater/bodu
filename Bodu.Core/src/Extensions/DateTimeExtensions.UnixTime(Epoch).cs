// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.UnixTime(Epoch).cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Represents the maximum number of milliseconds since the Unix epoch that can be converted to a
    /// <see cref="DateTime" />.
    /// </summary>
    internal const long MaxEpochMilliseconds = (MaxTicks / TicksPerMillisecond) - UnixEpochMilliseconds;

    /// <summary>
    /// Represents the maximum number of seconds since the Unix epoch that can be converted to a <see cref="DateTime" />
    /// .
    /// </summary>
    internal const long MaxEpochSeconds = (MaxTicks / TicksPerSecond) - UnixEpochSeconds;

    /// <summary>
    /// Represents the minimum number of milliseconds since the Unix epoch that can be converted to a
    /// <see cref="DateTime" />.
    /// </summary>
    internal const long MinEpochMilliseconds = (MinTicks / TicksPerMillisecond) - UnixEpochMilliseconds;

    /// <summary>
    /// Represents the minimum number of seconds since the Unix epoch that can be converted to a <see cref="DateTime" />
    /// .
    /// </summary>
    internal const long MinEpochSeconds = (MinTicks / TicksPerSecond) - UnixEpochSeconds;

    /// <summary>
    /// Represents the number of milliseconds between <see cref="DateTime.MinValue" /> and the Unix epoch
    /// (1970-01-01T00:00:00Z).
    /// </summary>
    internal const long UnixEpochMilliseconds = UnixEpochTicks / TicksPerMillisecond;

    /// <summary>
    /// Represents the number of seconds between <see cref="DateTime.MinValue" /> and the Unix epoch
    /// (1970-01-01T00:00:00Z).
    /// </summary>
    internal const long UnixEpochSeconds = UnixEpochTicks / TicksPerSecond;

    /// <summary>
    /// Represents the number of ticks (100 nanoseconds) between <see cref="DateTime.MinValue" /> and the Unix epoch
    /// (1970-01-01T00:00:00Z).
    /// </summary>
    internal const long UnixEpochTicks = TicksPerDay * DaysTo1970;

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the point in time corresponding to the specified Unix
    /// timestamp, expressed in milliseconds since 1970-01-01T00:00:00Z.
    /// </summary>
    /// <param name="timestamp">The number of milliseconds that have elapsed since the Unix epoch.</param>
    /// <returns>
    /// An object whose value is set to the UTC date and time corresponding to <paramref name="timestamp" />, with
    /// <see cref="DateTime.Kind" /> equal to <see cref="DateTimeKind.Utc" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="ToUnixTimeMilliseconds(DateTime)" /> to perform the inverse conversion.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="timestamp" /> is outside the range supported for conversion to <see cref="DateTime" />
    /// .
    /// </exception>
    /// <seealso cref="ToUnixTimeMilliseconds(DateTime)" />
    public static DateTime FromUnixTimeMilliseconds(long timestamp)
    {
        ThrowHelper.ThrowIfOutOfRange(timestamp, MinEpochMilliseconds, MaxEpochMilliseconds);
        return new DateTime(UnixEpochTicks + (timestamp * TicksPerMillisecond), DateTimeKind.Utc);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the point in time corresponding to the specified Unix
    /// timestamp, expressed in seconds since 1970-01-01T00:00:00Z.
    /// </summary>
    /// <param name="timestamp">The number of seconds that have elapsed since the Unix epoch.</param>
    /// <returns>
    /// An object whose value is set to the UTC date and time corresponding to <paramref name="timestamp" />, with
    /// <see cref="DateTime.Kind" /> equal to <see cref="DateTimeKind.Utc" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use <see cref="ToUnixTimeSeconds(DateTime)" /> to perform the inverse conversion.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="timestamp" /> is outside the range supported for conversion to <see cref="DateTime" />
    /// .
    /// </exception>
    /// <seealso cref="ToUnixTimeSeconds(DateTime)" />
    public static DateTime FromUnixTimeSeconds(long timestamp)
    {
        ThrowHelper.ThrowIfOutOfRange(timestamp, MinEpochSeconds, MaxEpochSeconds);
        return new DateTime(UnixEpochTicks + (timestamp * TicksPerSecond), DateTimeKind.Utc);
    }

    /// <summary>
    /// Returns the number of milliseconds that have elapsed between the Unix epoch (1970-01-01T00:00:00Z) and the
    /// specified <see cref="DateTime" />.
    /// </summary>
    /// <param name="dateTime">
    /// The date and time value to convert. The value is first normalized to UTC using
    /// <see cref="DateTime.ToUniversalTime()" />.
    /// </param>
    /// <returns>The total number of milliseconds since the Unix epoch.</returns>
    /// <remarks>
    /// <para>
    /// This method normalizes the input to UTC before computing the elapsed time. Use
    /// <see cref="FromUnixTimeMilliseconds(long)" /> to convert back.
    /// </para>
    /// </remarks>
    /// <seealso cref="FromUnixTimeMilliseconds(long)" />
    public static long ToUnixTimeMilliseconds(this DateTime dateTime) => (dateTime.ToUniversalTime().Ticks / TicksPerMillisecond) - UnixEpochMilliseconds;

    /// <summary>
    /// Returns the number of seconds that have elapsed between the Unix epoch (1970-01-01T00:00:00Z) and the specified
    /// <see cref="DateTime" />.
    /// </summary>
    /// <param name="dateTime">
    /// The date and time value to convert. The value is first normalized to UTC using
    /// <see cref="DateTime.ToUniversalTime()" />.
    /// </param>
    /// <returns>The total number of seconds since the Unix epoch.</returns>
    /// <remarks>
    /// <para>
    /// This method normalizes the input to UTC before computing the elapsed time. Use
    /// <see cref="FromUnixTimeSeconds(long)" /> to convert back.
    /// </para>
    /// </remarks>
    /// <seealso cref="FromUnixTimeSeconds(long)" />
    public static long ToUnixTimeSeconds(this DateTime dateTime) => (dateTime.ToUniversalTime().Ticks / TicksPerSecond) - UnixEpochSeconds;
}
