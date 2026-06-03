// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2;

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
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = ((19 * a) + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        int m = (a + (11 * h) + (22 * l)) / 451;
        int month = (h + l - (7 * m) + 114) / 31;
        int day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }

    /// <summary>
    /// Computes Eastern Orthodox Easter Sunday, returning its date on the Gregorian calendar.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The Gregorian date of Orthodox Easter Sunday in <paramref name="year" />.</returns>
    public static DateOnly Orthodox(int year)
    {
        int a = year % 4;
        int b = year % 7;
        int c = year % 19;
        int d = ((19 * c) + 15) % 30;
        int e = ((2 * a) + (4 * b) - d + 34) % 7;
        int month = (d + e + 114) / 31;
        int day = ((d + e + 114) % 31) + 1;

        JulianCalendar julian = new();
        DateTime gregorian = julian.ToDateTime(year, month, day, 0, 0, 0, 0);

        return DateOnly.FromDateTime(gregorian);
    }
}
