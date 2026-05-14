// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GregorianEasterSundayNotableDateProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.Algorithms;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Providers;

/// <summary>
/// Resolves Gregorian Easter Sunday dates.
/// </summary>
/// <remarks>
/// This provider always uses the Gregorian computus. The optional <see cref="SysGlobal.Calendar" /> parameter is accepted only when it
/// is <see langword="null" /> or a <see cref="SysGlobal.GregorianCalendar" />.
/// </remarks>
public sealed class GregorianEasterSundayNotableDateProvider
    : EasterSundayNotableDateProviderBase
{
    /// <inheritdoc />
    public override int MinSupportedYear => 1583;
    /// <inheritdoc />
    protected override string Name => "Easter Sunday";

    /// <inheritdoc />
    protected override NotableDateCategory Category => NotableDateCategory.Religious;

    /// <inheritdoc />
    protected override Type? DefaultCalendarType => typeof(SysGlobal.GregorianCalendar);

    /// <inheritdoc />
    protected override string? Comment => "Calculated using the Gregorian computus.";

    /// <inheritdoc />
    protected override void ValidateCalendar(SysGlobal.Calendar? calendar)
    {
        if (calendar is null || calendar is SysGlobal.GregorianCalendar)
        {
            return;
        }

        throw new NotSupportedException(
            string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.NotSupportedException_CalendarTypeNotSupported,
                calendar.GetType().FullName, GetType().Name, typeof(SysGlobal.GregorianCalendar).FullName));
    }

    /// <inheritdoc />
    protected override DateTime CalculateDate(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
    }
}