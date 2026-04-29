// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsalhaPujaNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides an algorithm for determining the Gregorian date of Asalha Puja (Dharma Day) for a given year.
/// </summary>
/// <remarks>
/// <para>
/// Asalha Puja (Thai: Asanha Bucha) commemorates the Buddha's first sermon at Deer Park in Sarnath. In the Theravada tradition it
/// falls on the full moon of the month of Asalha, which is the eighth month of the Thai lunar calendar — approximately two synodic
/// months (about 59 days) after Vesak, placing it in June or July.
/// </para>
/// <para>
/// This algorithm returns the first full moon that falls on or after 15 June of the given year, which consistently matches the
/// Thai Asanha Bucha observance. The next-day observance that begins Vassa (Buddhist Lent) is expressed as an
/// <c>OffsetFromAnchor</c> rule (+1 day from Asalha Puja) in the XML resource and requires no separate algorithm.
/// </para>
/// <para>
/// The full moon date is computed using the Meeus Chapter 49 algorithm via <see cref="LunarPhaseAlgorithm" />, accurate to within
/// one calendar day for years in the range 1900–2100.
/// </para>
/// </remarks>
public sealed class AsalhaPujaNotableDateAlgorithm
	: INotableDateAlgorithm
{
	/// <summary>
	/// Computes the date of Asalha Puja for the specified year.
	/// </summary>
	/// <param name="year">The year for which to calculate Asalha Puja. Must be greater than or equal to 1.</param>
	/// <param name="calendar">
	/// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. When <see langword="null" />,
	/// <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// A <see cref="DateTime" /> representing the date of Asalha Puja, or <see langword="null" /> if the date cannot be determined.
	/// The returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">Thrown when the specified <paramref name="calendar" /> type is unsupported.</exception>
	public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
	{
		if (year < 1)
			throw new ArgumentOutOfRangeException(nameof(year), CalendarStrings.ArgumentOutOfRangeException_YearOutOfRange);

		DateTime? fullMoon = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(new DateTime(year, 6, 15));
		if (fullMoon is null)
			return null;

		SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

		if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
			return DateTime.SpecifyKind(fullMoon.Value, DateTimeKind.Unspecified);

		return new DateTime(
			targetCalendar.GetYear(fullMoon.Value),
			targetCalendar.GetMonth(fullMoon.Value),
			targetCalendar.GetDayOfMonth(fullMoon.Value),
			0, 0, 0, 0,
			targetCalendar,
			DateTimeKind.Unspecified);
	}
}
