// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VassaStartNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a algorithm for determining the Gregorian date on which Vassa (the Buddhist Rains Retreat) begins.
/// </summary>
/// <remarks>
/// <para>
/// Vassa (Pali; Thai: Khao Phansa) is the three-month rains retreat observed by monks and novices in Theravada countries. It begins
/// on the day immediately following Asalha Puja (the full moon of Asalha) and ends with the Pavarana ceremony on the full moon of
/// Ashvin (approximately three months later).
/// </para>
/// <para>
/// This algorithm delegates to <see cref="AsalhaPujaNotableDateAlgorithm" /> and adds one calendar day to obtain the Vassa start
/// date. The result is the first day after the Asalha Puja full moon, which in Thailand is a public holiday (Khao Phansa).
/// </para>
/// </remarks>
public sealed class VassaStartNotableDateAlgorithm
	: INotableDateAlgorithm
{
	private readonly AsalhaPujaNotableDateAlgorithm _asalhaPuja = new AsalhaPujaNotableDateAlgorithm();

	/// <summary>
	/// Computes the first day of Vassa for the specified year.
	/// </summary>
	/// <param name="year">The year for which to calculate the Vassa start date. Must be greater than or equal to 1.</param>
	/// <param name="calendar">
	/// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. When <see langword="null" />,
	/// <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// A <see cref="DateTime" /> representing the start of Vassa (one day after Asalha Puja), or <see langword="null" /> if the
	/// Asalha Puja date cannot be determined. The returned <see cref="DateTime.Kind" /> is always
	/// <see cref="DateTimeKind.Unspecified" />.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">Thrown when the specified <paramref name="calendar" /> type is unsupported.</exception>
	public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
	{
		if (year < 1)
			throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than or equal to 1.");

		// Always compute Asalha Puja in Gregorian first, then project to the target calendar
		// after adding one day, so that "day + 1" arithmetic is unambiguous.
		DateTime? asalha = _asalhaPuja.GetDate(year, calendar: null);
		if (asalha is null)
			return null;

		DateTime vassaStart = asalha.Value.AddDays(1);

		SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

		if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
			return DateTime.SpecifyKind(vassaStart, DateTimeKind.Unspecified);

		return new DateTime(
			targetCalendar.GetYear(vassaStart),
			targetCalendar.GetMonth(vassaStart),
			targetCalendar.GetDayOfMonth(vassaStart),
			0, 0, 0, 0,
			targetCalendar,
			DateTimeKind.Unspecified);
	}
}
