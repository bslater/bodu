// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GregorianEasterSundayNotableDateProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
    protected override NotableDateCategory Category => NotableDateCategory.Cultural;

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
            $"The calendar type '{calendar.GetType().FullName}' is not supported by {GetType().Name}. " +
            $"Only {typeof(SysGlobal.GregorianCalendar).FullName} is supported.");
    }

    /// <inheritdoc />
    protected override DateTime CalculateDate(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - (7 * m) + 114) / 31;
        int day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
    }
}