// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OrthodoxEasterSundayNotableDateProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;
using System.Globalization;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Providers;

/// <summary>
/// Resolves Orthodox Easter Sunday dates.
/// </summary>
/// <remarks>
/// This provider uses the Julian computus and returns the Gregorian-equivalent <see cref="DateTime" /> for the resolved Julian calendar
/// date. The optional <see cref="SysGlobal.Calendar" /> parameter is accepted only when it is <see langword="null" /> or a
/// <see cref="SysGlobal.JulianCalendar" />.
/// </remarks>
public sealed class OrthodoxEasterSundayNotableDateProvider
    : EasterSundayNotableDateProviderBase
{
    /// <inheritdoc />
    public override int MinSupportedYear => 1;

    /// <summary>A shared <see cref="SysGlobal.JulianCalendar" /> instance used to project Julian Easter dates to Gregorian equivalents.</summary>
    private static readonly SysGlobal.JulianCalendar JulianCalendar = new();

    /// <inheritdoc />
    protected override string Name => "Orthodox Easter Sunday";

    /// <inheritdoc />
    protected override NotableDateCategory Category => NotableDateCategory.Religious;

    /// <inheritdoc />
    protected override Type? DefaultCalendarType => typeof(SysGlobal.JulianCalendar);

    /// <inheritdoc />
    protected override string? Comment => "Calculated using the Julian computus and projected to a Gregorian DateTime.";

    /// <inheritdoc />
    protected override void ValidateCalendar(SysGlobal.Calendar? calendar)
    {
        if (calendar is null || calendar is SysGlobal.JulianCalendar)
        {
            return;
        }

        throw new NotSupportedException(
            string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.NotSupportedException_CalendarTypeNotSupported,
                calendar.GetType().FullName, GetType().Name, typeof(SysGlobal.JulianCalendar).FullName));
    }

    /// <inheritdoc />
    protected override DateTime CalculateDate(int year)
    {
        int a = year % 4;
        int b = year % 7;
        int c = year % 19;
        int d = (19 * c + 15) % 30;
        int e = (2 * a + (4 * b) - d + 34) % 7;
        int f = d + e + 114;
        int month = f / 31;
        int day = (f % 31) + 1;

        return JulianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
    }
}