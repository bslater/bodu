// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChineseLunisolarNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a algorithm for determining the Gregorian date of a festival defined by a fixed month and day in the Chinese lunisolar
/// calendar.
/// </summary>
/// <remarks>
/// <para>
/// Many traditional East Asian festivals are defined in terms of a specific day within a specific month of the Chinese lunisolar
/// calendar. This algorithm converts a given (lunarMonth, lunarDay) pair into the corresponding Gregorian date for a specified year
/// using the <see cref="SysGlobal.ChineseLunisolarCalendar" />.
/// </para>
/// <para>
/// The <paramref name="year" /> parameter passed to <see cref="GetDate" /> is interpreted as the Gregorian year in which the Chinese
/// lunisolar year of the same number begins. For months 1–9 the resulting Gregorian date falls within that same year; for months 10–12
/// the result may also fall within the same year or very early in the following year, depending on the calendar alignment.
/// </para>
/// <para>
/// The supported range is determined by <see cref="SysGlobal.ChineseLunisolarCalendar.MinSupportedDateTime" /> and
/// <see cref="SysGlobal.ChineseLunisolarCalendar.MaxSupportedDateTime" />, which covers Gregorian years 1901–2100.
/// </para>
/// </remarks>
public sealed class ChineseLunisolarNotableDateAlgorithm
	: INotableDateAlgorithm
{
	private static readonly SysGlobal.ChineseLunisolarCalendar ChineseCalendar = new SysGlobal.ChineseLunisolarCalendar();

	private readonly int _lunarMonth;
	private readonly int _lunarDay;

	/// <summary>
	/// Initialises a new instance of the <see cref="ChineseLunisolarNotableDateAlgorithm" /> class for the given lunar month and day.
	/// </summary>
	/// <param name="lunarMonth">The Chinese lunisolar month number (1–13). Months 1–12 always exist; month 13 only exists in leap lunar years.</param>
	/// <param name="lunarDay">The Chinese lunisolar day number within the month (1–30).</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="lunarMonth" /> is less than 1 or greater than 13, or <paramref name="lunarDay" /> is less than 1 or greater
	/// than 30.
	/// </exception>
	public ChineseLunisolarNotableDateAlgorithm(int lunarMonth, int lunarDay)
	{
		if (lunarMonth < 1 || lunarMonth > 13)
			throw new ArgumentOutOfRangeException(nameof(lunarMonth), "Lunar month must be between 1 and 13.");
		if (lunarDay < 1 || lunarDay > 30)
			throw new ArgumentOutOfRangeException(nameof(lunarDay), "Lunar day must be between 1 and 30.");

		_lunarMonth = lunarMonth;
		_lunarDay = lunarDay;
	}

	/// <summary>
	/// Computes the Gregorian date corresponding to the configured lunar month and day for the specified year.
	/// </summary>
	/// <param name="year">
	/// The Gregorian year for which to calculate the date. Must be greater than or equal to 1. Only years in the range supported by
	/// <see cref="SysGlobal.ChineseLunisolarCalendar" /> (1901–2100) produce a non-<see langword="null" /> result.
	/// </param>
	/// <param name="calendar">
	/// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use for the returned
	/// <see cref="DateTime" />. When <see langword="null" />, <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// A <see cref="DateTime" /> representing the festival date in the specified calendar system, or <see langword="null" /> if the year
	/// is outside the supported range or the configured lunar month does not exist in that year (e.g. month 13 in a non-leap lunar year).
	/// The returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">Thrown when the specified <paramref name="calendar" /> type is unsupported.</exception>
	public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
	{
		if (year < 1)
			throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than or equal to 1.");

		if (year < ChineseCalendar.MinSupportedDateTime.Year || year >= ChineseCalendar.MaxSupportedDateTime.Year)
			return null;

		int monthsInYear = ChineseCalendar.GetMonthsInYear(year);
		if (_lunarMonth > monthsInYear)
			return null;

		int daysInMonth = ChineseCalendar.GetDaysInMonth(year, _lunarMonth);
		if (_lunarDay > daysInMonth)
			return null;

		DateTime result = ChineseCalendar.ToDateTime(year, _lunarMonth, _lunarDay, 0, 0, 0, 0);

		SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

		if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
			return DateTime.SpecifyKind(result.Date, DateTimeKind.Unspecified);

		return new DateTime(
			targetCalendar.GetYear(result),
			targetCalendar.GetMonth(result),
			targetCalendar.GetDayOfMonth(result),
			0, 0, 0, 0,
			targetCalendar,
			DateTimeKind.Unspecified);
	}
}
