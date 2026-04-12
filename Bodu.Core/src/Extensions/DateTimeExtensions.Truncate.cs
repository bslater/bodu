// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.Truncate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the specified <paramref name="dateTime" /> truncated to the given
    /// <paramref name="resolution" />, with all smaller time components set to zero.
    /// </summary>
    /// <param name="dateTime">The <see cref="DateTime" /> value to truncate.</param>
    /// <param name="resolution">The <see cref="DateTimeResolution" /> level to truncate to.</param>
    /// <returns>
    /// An object whose value is set to the <paramref name="dateTime" /> truncated to the specified <paramref name="resolution" />,
    /// preserving the original <see cref="DateTime.Kind" />.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="resolution" /> is not a valid member of the <see cref="DateTimeResolution" /> enumeration.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Truncation resets all components smaller than the specified resolution to their minimum values. For example, truncating to
    /// <see cref="DateTimeResolution.Minute" /> clears seconds and fractional seconds.
    /// </para>
    /// <para>The following examples show the result of truncating <c>2024-04-18T14:37:56.7891234</c>:</para>
    /// <list type="table">
    /// <listheader>
    /// <term>Resolution</term>
    /// <description>Result</description>
    /// </listheader>
    /// <item>
    /// <term><see cref="DateTimeResolution.Year" /></term>
    /// <description><c>2024-01-01T00:00:00.0000000</c></description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Month" /></term>
    /// <description><c>2024-04-01T00:00:00.0000000</c></description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Day" /></term>
    /// <description><c>2024-04-18T00:00:00.0000000</c></description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Hour" /></term>
    /// <description><c>2024-04-18T14:00:00.0000000</c></description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Minute" /></term>
    /// <description><c>2024-04-18T14:37:00.0000000</c></description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Second" /></term>
    /// <description><c>2024-04-18T14:37:56.0000000</c></description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Millisecond" /></term>
    /// <description><c>2024-04-18T14:37:56.7890000</c></description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Tick" /></term>
    /// <description><c>2024-04-18T14:37:56.7891234</c> (unchanged)</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static DateTime Truncate(this DateTime dateTime, DateTimeResolution resolution)
    {
        switch (resolution)
        {
            case DateTimeResolution.Year:
                return new DateTime(GetDateTicks(dateTime.Year, 1, 1), dateTime.Kind);

            case DateTimeResolution.Month:
                return new DateTime(GetDateTicks(dateTime.Year, dateTime.Month, 1), dateTime.Kind);

            case DateTimeResolution.Day:
                return new DateTime(TruncateToDateTicks(dateTime), dateTime.Kind);

            case DateTimeResolution.Hour:
                return dateTime.AddTicks(-(dateTime.Ticks % TicksPerHour));

            case DateTimeResolution.Minute:
                return dateTime.AddTicks(-(dateTime.Ticks % TicksPerMinute));

            case DateTimeResolution.Second:
                return dateTime.AddTicks(-(dateTime.Ticks % TicksPerSecond));

            case DateTimeResolution.Millisecond:
                return dateTime.AddTicks(-(dateTime.Ticks % TicksPerMillisecond));

            case DateTimeResolution.Tick:
                return dateTime;

            default:
                throw new ArgumentException(
                    string.Format(ResourceStrings.Arg_OutOfRangeException_EnumValue, resolution, nameof(DateTimeResolution)),
                    nameof(resolution));
        }
    }
}
