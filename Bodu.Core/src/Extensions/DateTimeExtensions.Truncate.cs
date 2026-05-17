// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.Truncate.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> obtained by truncating the specified <paramref name="dateTime" /> to the
    /// supplied <paramref name="resolution" />, with all smaller time components reset to zero.
    /// </summary>
    /// <param name="dateTime">The date and time value to truncate.</param>
    /// <param name="resolution">The <see cref="DateTimeResolution" /> level to truncate to.</param>
    /// <returns>
    /// An object whose value is the result of truncating <paramref name="dateTime" /> to the supplied
    /// <paramref name="resolution" />, with the original <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Truncation resets all components smaller than the supplied resolution to their minimum values. For example,
    /// truncating to <see cref="DateTimeResolution.Minute" /> clears seconds and fractional seconds.
    /// </para>
    /// <para>
    /// The following examples show the result of truncating <c>2024-04-18T14:37:56.7891234</c>:
    /// </para>
    /// <list type="table">
    /// <listheader><term>Resolution</term>
    /// <description>
    /// Result
    /// </description>
    /// </listheader>
    /// <item>
    /// <term><see cref="DateTimeResolution.Year" /></term>
    /// <description>
    /// <c>2024-01-01T00:00:00.0000000</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Month" /></term>
    /// <description>
    /// <c>2024-04-01T00:00:00.0000000</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Day" /></term>
    /// <description>
    /// <c>2024-04-18T00:00:00.0000000</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Hour" /></term>
    /// <description>
    /// <c>2024-04-18T14:00:00.0000000</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Minute" /></term>
    /// <description>
    /// <c>2024-04-18T14:37:00.0000000</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Second" /></term>
    /// <description>
    /// <c>2024-04-18T14:37:56.0000000</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Millisecond" /></term>
    /// <description>
    /// <c>2024-04-18T14:37:56.7890000</c>
    /// </description>
    /// </item>
    /// <item>
    /// <term><see cref="DateTimeResolution.Tick" /></term>
    /// <description>
    /// <c>2024-04-18T14:37:56.7891234</c> (unchanged)
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="resolution" /> is not a defined value of the <see cref="DateTimeResolution" />
    /// enumeration.
    /// </exception>
    public static DateTime Truncate(this DateTime dateTime, DateTimeResolution resolution)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(resolution);

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
                throw new ArgumentOutOfRangeException(
                    nameof(resolution),
                    string.Format(ResourceStrings.Arg_OutOfRange_EnumValue, resolution, nameof(DateTimeResolution)));
        }
    }
}
