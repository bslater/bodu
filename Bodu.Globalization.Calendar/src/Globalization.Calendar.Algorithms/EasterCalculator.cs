// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Computes the Gregorian date of Easter Sunday under the Western (Gregorian computus) and Eastern Orthodox (Julian
/// computus) reckonings.
/// </summary>
internal static class EasterCalculator
{
    /// <summary>
    /// Computes Western (Gregorian) Easter Sunday using the Anonymous Gregorian algorithm.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The date of Western Easter Sunday in <paramref name="year" />.</returns>
    public static DateOnly Western(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = ((19 * a) + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        var m = (a + (11 * h) + (22 * l)) / 451;
        var month = (h + l - (7 * m) + 114) / 31;
        var day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }

    /// <summary>
    /// Computes Eastern Orthodox Easter Sunday, returning its date on the Gregorian calendar.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The Gregorian date of Orthodox Easter Sunday in <paramref name="year" />.</returns>
    public static DateOnly Orthodox(int year)
    {
        var a = year % 4;
        var b = year % 7;
        var c = year % 19;
        var d = ((19 * c) + 15) % 30;
        var e = ((2 * a) + (4 * b) - d + 34) % 7;
        var month = (d + e + 114) / 31;
        var day = ((d + e + 114) % 31) + 1;

        JulianCalendar julian = new();
        var gregorian = julian.ToDateTime(year, month, day, 0, 0, 0, 0);

        return DateOnly.FromDateTime(gregorian);
    }
}
