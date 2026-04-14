// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.ToDateTimeOffset.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTimeOffset"/> representing the same moment as the specified <paramref name="dateTime"/>, with the
    /// offset inferred from its <see cref="DateTime.Kind"/>.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to convert.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing the same point in time as <paramref name="dateTime"/>, with the offset derived from
    /// its <see cref="DateTime.Kind"/> (i.e., <c>Local</c>, <c>Utc</c>, or <c>Unspecified</c>).
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the resulting UTC time is outside the supported range of <see cref="DateTimeOffset"/>.</exception>
    /// <remarks>
    /// <para>The behavior depends on the <see cref="DateTime.Kind"/> of the input:</para>
    /// <list type="bullet">
    /// <item>
    /// <term><see cref="DateTimeKind.Utc"/></term>
    /// <description>Applies a zero offset (UTC).</description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeKind.Local"/></term>
    /// <description>Applies the system’s local time zone offset.</description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeKind.Unspecified"/></term>
    /// <description>Treats the value as local time and applies the system’s local offset.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime) => new DateTimeOffset(dateTime);

    /// <summary>
    /// Returns a new <see cref="DateTimeOffset"/> representing the same clock time as the specified <paramref name="dateTime"/>, with the
    /// specified <paramref name="offset"/> applied.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime"/> value to convert.</param>
    /// <param name="offset">The UTC <see cref="TimeSpan"/> offset to associate with the resulting <see cref="DateTimeOffset"/>.</param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> with the same local time as <paramref name="dateTime"/>, and the specified
    /// <paramref name="offset"/> applied.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="offset"/> is not within the range ±14 hours, or if it is incompatible with the
    /// <see cref="DateTime.Kind"/> of <paramref name="dateTime"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the resulting UTC time is outside the supported range of <see cref="DateTimeOffset"/>.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload to explicitly associate a non-local offset—such as when dealing with fixed time zones, historical data, or
    /// offset-based scheduling.
    /// </para>
    /// <para>The <paramref name="offset"/> must be within the range ±14:00.</para>
    /// </remarks>
    public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime, TimeSpan offset) => new DateTimeOffset(dateTime, offset);
}
