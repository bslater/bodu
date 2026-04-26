// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HebrewNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a algorithm for determining the Gregorian date of an observance defined by a fixed month and day in the Hebrew
/// (Jewish) lunisolar calendar.
/// </summary>
/// <remarks>
/// <para>
/// The Hebrew calendar is a lunisolar calendar whose months follow the lunar cycle but whose years are periodically adjusted with
/// an intercalary month (Adar II) to keep in step with the solar year. This intercalation causes the month numbers of Nisan and all
/// later months to shift by one in leap years. This algorithm accepts stable <see cref="HebrewMonth" /> values and resolves them to
/// the correct internal <see cref="SysGlobal.HebrewCalendar" /> month number at runtime.
/// </para>
/// <para>
/// The <see cref="HebrewMonth.LastAdar" /> value resolves to Adar (month 6) in standard years and to Adar II (month 7) in leap
/// years, enabling correct calculation of observances such as Purim that are always celebrated in the last Adar.
/// </para>
/// <para>
/// Because the Hebrew year begins in September or October, a single Gregorian year spans parts of two Hebrew years. The algorithm
/// tries both Hebrew years that overlap the requested Gregorian year and returns the first match.
/// </para>
/// </remarks>
public sealed class HebrewNotableDateAlgorithm
	: INotableDateAlgorithm
{
	private static readonly SysGlobal.HebrewCalendar HebrewCalendar = new SysGlobal.HebrewCalendar();

	private readonly HebrewMonth _month;
	private readonly int _day;

	/// <summary>
	/// Initialises a new instance of the <see cref="HebrewNotableDateAlgorithm" /> class for the given Hebrew month and day.
	/// </summary>
	/// <param name="month">The Hebrew month, using the stable <see cref="HebrewMonth" /> enumeration.</param>
	/// <param name="day">The day within the month (1–30).</param>
	/// <exception cref="ArgumentOutOfRangeException"><paramref name="day" /> is less than 1 or greater than 30.</exception>
	/// <exception cref="ArgumentException"><paramref name="month" /> is not a defined <see cref="HebrewMonth" /> value.</exception>
	public HebrewNotableDateAlgorithm(HebrewMonth month, int day)
	{
		if (!Enum.IsDefined(typeof(HebrewMonth), month))
			throw new ArgumentException($"'{month}' is not a defined HebrewMonth value.", nameof(month));
		if (day < 1 || day > 30)
			throw new ArgumentOutOfRangeException(nameof(day), "Day must be between 1 and 30.");

		_month = month;
		_day = day;
	}

	/// <summary>
	/// Computes the Gregorian date corresponding to the configured Hebrew month and day for the specified Gregorian year.
	/// </summary>
	/// <param name="year">
	/// The Gregorian year for which to calculate the date. Must be greater than or equal to 1.
	/// </param>
	/// <param name="calendar">
	/// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use for the returned
	/// <see cref="DateTime" />. When <see langword="null" />, <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// A <see cref="DateTime" /> representing the observance date in the specified calendar system, or <see langword="null" /> if the
	/// date does not fall in the given Gregorian year or the configured month does not exist in a particular Hebrew year (for example,
	/// <see cref="HebrewMonth.AdarII" /> in a non-leap year). The returned <see cref="DateTime.Kind" /> is always
	/// <see cref="DateTimeKind.Unspecified" />.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">Thrown when the specified <paramref name="calendar" /> type is unsupported.</exception>
	public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
	{
		if (year < 1)
			throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than or equal to 1.");

		SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

		int hebrewYearForJan1 = HebrewCalendar.GetYear(new DateTime(year, 1, 1));

		for (int h = hebrewYearForJan1; h <= hebrewYearForJan1 + 1; h++)
		{
			bool isLeapYear = HebrewCalendar.GetMonthsInYear(h) == 13;
			int monthNumber = ResolveMonthNumber(_month, isLeapYear);
			if (monthNumber < 0)
				continue;

			int daysInMonth = HebrewCalendar.GetDaysInMonth(h, monthNumber);
			if (_day > daysInMonth)
				continue;

			DateTime candidate;
			try
			{
				candidate = HebrewCalendar.ToDateTime(h, monthNumber, _day, 0, 0, 0, 0);
			}
			catch (ArgumentOutOfRangeException)
			{
				continue;
			}

			if (candidate.Year != year)
				continue;

			if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
				return DateTime.SpecifyKind(candidate.Date, DateTimeKind.Unspecified);

			return new DateTime(
				targetCalendar.GetYear(candidate),
				targetCalendar.GetMonth(candidate),
				targetCalendar.GetDayOfMonth(candidate),
				0, 0, 0, 0,
				targetCalendar,
				DateTimeKind.Unspecified);
		}

		return null;
	}

	/// <summary>
	/// Resolves a <see cref="HebrewMonth" /> value to the internal <see cref="SysGlobal.HebrewCalendar" /> month number for the
	/// given leap-year state, or returns -1 if the month does not exist in that year type.
	/// </summary>
	private static int ResolveMonthNumber(HebrewMonth month, bool isLeapYear) =>
		month switch
		{
			// Months 1–5 (Tishri through Shevat) are always at fixed positions.
			HebrewMonth.Tishri => 1,
			HebrewMonth.Heshvan => 2,
			HebrewMonth.Kislev => 3,
			HebrewMonth.Tevet => 4,
			HebrewMonth.Shevat => 5,

			// Adar I is always month 6. In non-leap years it is the only Adar.
			HebrewMonth.AdarI => 6,

			// Adar II (month 7) exists only in leap years.
			HebrewMonth.AdarII => isLeapYear ? 7 : -1,

			// LastAdar resolves to Adar II in leap years and to Adar I in standard years.
			HebrewMonth.LastAdar => isLeapYear ? 7 : 6,

			// Months from Nisan onward shift by one in leap years due to the intercalated Adar II.
			HebrewMonth.Nisan => isLeapYear ? 8 : 7,
			HebrewMonth.Iyar => isLeapYear ? 9 : 8,
			HebrewMonth.Sivan => isLeapYear ? 10 : 9,
			HebrewMonth.Tammuz => isLeapYear ? 11 : 10,
			HebrewMonth.Av => isLeapYear ? 12 : 11,
			HebrewMonth.Elul => isLeapYear ? 13 : 12,

			_ => -1,
		};
}
