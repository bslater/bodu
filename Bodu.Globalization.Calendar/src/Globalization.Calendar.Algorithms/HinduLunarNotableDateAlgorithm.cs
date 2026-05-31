// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduLunarNotableDateAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides an algorithm for determining the approximate Gregorian date of a Hindu festival defined by a lunar month,
/// paksha (fortnight), and tithi (lunar day).
/// </summary>
/// <remarks>
/// <para>
/// Hindu festivals are traditionally defined in terms of the Hindu lunisolar (panchanga) calendar. A date in this
/// calendar is specified as a tithi (one of thirty lunar days in a synodic month) within a paksha (bright or dark
/// fortnight) of a named lunar month. The lunar month itself is identified by the solar sign (rashi) that the sun
/// occupies at the new moon — a calculation that requires both solar and lunar positional astronomy.
/// </para>
/// <para>
/// <strong>Calculation approach:</strong> This algorithm uses the following approximation method, accurate to within
/// one or two calendar days for years in the range 1900–2100:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// Estimate the Gregorian month in which the target Hindu lunar month is likely to begin, using a fixed month-offset
/// table (since Hindu months advance by approximately 11 days per Gregorian year in a regular 19-year Metonic cycle,
/// but reset by the intercalary month approximately every 32 months, the offset is treated as approximately fixed for
/// single-year lookup purposes).
/// </description>
/// </item>
/// <item>
/// <description>
/// Find the new moon on or after the first day of that estimated Gregorian month. This new moon is taken as the start
/// of the Amanta (new-moon reckoning) lunar month.
/// </description>
/// </item>
/// <item>
/// <description>
/// Add the tithi offset: for <see cref="HinduPaksha.Shukla" />, add (tithi - 1) synodic days from the new moon; for
/// <see cref="HinduPaksha.Krishna" />, find the full moon (approximately 15 synodic days after the new moon) and add
/// (tithi - 1) synodic days from it. One synodic day (tithi) is 29.53058 / 30 ≈ 0.9843 Gregorian days.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Accuracy note:</strong> The actual tithi at any moment depends on the precise instantaneous positions of the
/// sun and moon. The method used here approximates tithis as equal subdivisions of the synodic month and approximates
/// the month start by an astronomical new moon rather than the precise panchanga calculation based on sunrise at a
/// specific geographic location. Results may differ from locally computed Indian panchanga dates by zero, one, or
/// (rarely) two calendar days. Consumers who require panchanga-exact dates should register a custom implementation
/// using a full Surya Siddhanta or Drik calculation.
/// </para>
/// </remarks>
public sealed class HinduLunarNotableDateAlgorithm
    : INotableDateAlgorithm
{
    /// <summary>
    /// The duration of a single tithi in days, computed as the mean synodic month divided by 30 tithis.
    /// </summary>
    private const double TithiDays = 29.530588861 / 30.0;

    /// <summary>
    /// The Hindu lunar month in which the festival falls.
    /// </summary>
    private readonly HinduLunarMonth _month;

    /// <summary>
    /// The fortnight (bright or dark) within the month in which the tithi falls.
    /// </summary>
    private readonly HinduPaksha _paksha;

    /// <summary>
    /// The tithi number (1–15) within the paksha.
    /// </summary>
    private readonly int _tithi;

    /// <summary>
    /// Initializes a new instance of the <see cref="HinduLunarNotableDateAlgorithm" /> class for the given Hindu lunar
    /// month, paksha, and tithi.
    /// </summary>
    /// <param name="month">The Hindu lunar month in which the festival falls.</param>
    /// <param name="paksha">The fortnight (bright or dark) in which the tithi falls.</param>
    /// <param name="tithi">The tithi number (1–15) within the paksha.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="tithi" /> is less than 1 or greater than 15.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="month" /> or <paramref name="paksha" /> is not a defined enumeration value.
    /// </exception>
    public HinduLunarNotableDateAlgorithm(HinduLunarMonth month, HinduPaksha paksha, int tithi)
    {
        if (!Enum.IsDefined(typeof(HinduLunarMonth), month))
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Arg_Invalid_HinduLunarMonthUndefined, month), nameof(month));
        if (!Enum.IsDefined(typeof(HinduPaksha), paksha))
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Arg_Invalid_HinduPakshaUndefined, paksha), nameof(paksha));
        if (tithi is < 1 or > 15)
            throw new ArgumentOutOfRangeException(nameof(tithi), CalendarResourceStrings.Arg_OutOfRange_Tithi);

        _month = month;
        _paksha = paksha;
        _tithi = tithi;
    }

    /// <summary>
    /// Computes the approximate Gregorian date of the festival for the specified year.
    /// </summary>
    /// <param name="year">The year for which to calculate the date. Must be greater than or equal to 1.</param>
    /// <param name="calendar">
    /// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. When
    /// <see langword="null" />, <see cref="SysGlobal.GregorianCalendar" /> is assumed.
    /// </param>
    /// <returns>
    /// A <see cref="DateTime" /> representing the approximate date of the festival, or <see langword="null" /> if the
    /// date cannot be determined. The returned <see cref="DateTime.Kind" /> is always
    /// <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="year" /> is less than 1 or greater than 9999.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="calendar" /> type is unsupported.
    /// </exception>
    public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
    {
        CalendarThrowHelper.ThrowIfYearOutOfRange(year);

        // Find the new moon that begins the target lunar month.
        var searchMonth = GetSearchMonth(_month);
        var searchYear = searchMonth == 1 ? year - 1 : year;
        if (searchYear < 1)
            searchYear = year;

        var searchStart = new DateTime(searchYear, searchMonth, 1);
        DateTime? monthNewMoon = LunarPhaseAlgorithm.GetNewMoonOnOrAfter(searchStart);
        if (monthNewMoon is null)
            return null;

        DateTime result;

        if (_paksha == HinduPaksha.Shukla)
        {
            // Shukla Paksha: count tithis forward from the new moon.
            // Tithi 1 (Pratipada) begins immediately after the new moon.
            result = monthNewMoon.Value.AddDays((_tithi - 1) * TithiDays);
        }
        else
        {
            // Krishna Paksha: count tithis forward from the full moon (approximately 15 tithis
            // after the new moon, i.e. 15 × TithiDays ≈ 14.76 days).
            DateTime fullMoonApprox = monthNewMoon.Value.AddDays(15 * TithiDays);
            DateTime? fullMoon = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(fullMoonApprox.AddDays(-2));
            if (fullMoon is null)
                return null;

            result = fullMoon.Value.AddDays((_tithi - 1) * TithiDays);
        }

        var dateOnly = DateTime.SpecifyKind(result.Date, DateTimeKind.Unspecified);

        SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

        return targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar)
            ? dateOnly
            : new DateTime(
            targetCalendar.GetYear(dateOnly),
            targetCalendar.GetMonth(dateOnly),
            targetCalendar.GetDayOfMonth(dateOnly),
            0, 0, 0, 0,
            targetCalendar,
            DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Returns the approximate Gregorian month in which the new moon starting the given Hindu lunar month typically
    /// falls. Used to seed the new moon search with a sensible start date.
    /// </summary>
    /// <param name="month">The Hindu lunar month whose approximate Gregorian start month is required.</param>
    /// <returns>
    /// The Gregorian month number (1–12) that is the best search-start approximation for the given lunar month.
    /// </returns>
    private static int GetSearchMonth(HinduLunarMonth month) =>
        month switch
        {
            HinduLunarMonth.Chaitra => 3,       // March/April
            HinduLunarMonth.Vaisakha => 4,      // April/May
            HinduLunarMonth.Jyeshtha => 5,      // May/June
            HinduLunarMonth.Ashadha => 6,       // June/July
            HinduLunarMonth.Shravana => 7,      // July/August
            HinduLunarMonth.Bhadrapada => 8,    // August/September
            HinduLunarMonth.Ashvin => 9,        // September/October
            HinduLunarMonth.Kartik => 10,       // October/November
            HinduLunarMonth.Margashirsha => 11, // November/December
            HinduLunarMonth.Pausha => 12,       // December/January → search in prev year's December
            HinduLunarMonth.Magha => 1,         // January/February → search in Jan of target year
            HinduLunarMonth.Phalguna => 2,      // February/March
            _ => 3,
        };
}
