// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LunarNewYearNotableDateCalculatorTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Calculators;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="LunarNewYearNotableDateCalculator" />.
/// </summary>
[TestClass]
public sealed class LunarNewYearNotableDateCalculatorTests
{
	private readonly LunarNewYearNotableDateCalculator _calculator = new();

	/// <summary>
	/// Verifies that requesting Lunar New Year with a year below one throws
	/// <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	[TestMethod]
	public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = _calculator.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that requesting Lunar New Year outside the supported 1901–2100 range returns
	/// <see langword="null" /> rather than throwing, including the exact upper-bound boundary
	/// year 2101 (which must not slip through the guard and reach <c>ToDateTime</c>).
	/// </summary>
	[DataRow(1)]
	[DataRow(1900)]
	[DataRow(2101)]
	[DataRow(9999)]
	[TestMethod]
	public void GetDate_WhenYearOutsideSupportedRange_ShouldReturnNull(int year)
	{
		DateTime? result = _calculator.GetDate(year);

		Assert.IsNull(result);
	}

	/// <summary>
	/// Verifies that Lunar New Year is computed correctly for a set of well-known years against
	/// the default (Gregorian) calendar projection.
	/// </summary>
	[DataRow(1901, 1901, 2, 19)]
	[DataRow(1912, 1912, 2, 18)]
	[DataRow(2000, 2000, 2, 5)]
	[DataRow(2020, 2020, 1, 25)]
	[DataRow(2024, 2024, 2, 10)]
	[DataRow(2025, 2025, 1, 29)]
	[DataRow(2026, 2026, 2, 17)]
	[DataRow(2100, 2100, 2, 9)]
	[TestMethod]
	public void GetDate_WhenYearIsSupportedAndDefaultCalendar_ShouldReturnExpectedGregorianDate(
		int year,
		int expectedYear,
		int expectedMonth,
		int expectedDay)
	{
		DateTime? result = _calculator.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(expectedYear, expectedMonth, expectedDay), result.Value);
	}

	/// <summary>
	/// Verifies that the default (null) calendar path yields a <see cref="DateTime" /> with
	/// <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		DateTime? result = _calculator.GetDate(2026, null);

		Assert.IsNotNull(result);
		Assert.AreEqual(DateTimeKind.Unspecified, result.Value.Kind);
	}

	/// <summary>
	/// Verifies that supplying an explicit <see cref="SysGlob.GregorianCalendar" /> yields the
	/// same date as the default null path and preserves <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsGregorian_ShouldMatchDefaultResult()
	{
		DateTime? gregorianResult = _calculator.GetDate(2026, new SysGlob.GregorianCalendar());
		DateTime? defaultResult = _calculator.GetDate(2026, null);

		Assert.IsNotNull(gregorianResult);
		Assert.IsNotNull(defaultResult);
		Assert.AreEqual(defaultResult.Value, gregorianResult.Value);
		Assert.AreEqual(DateTimeKind.Unspecified, gregorianResult.Value.Kind);
	}

	/// <summary>
	/// Verifies that supplying an explicit <see cref="SysGlob.JulianCalendar" /> returns a
	/// <see cref="DateTime" /> whose Julian day/month/year match the underlying Gregorian date
	/// rather than re-using the Gregorian components.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsJulian_ShouldProjectGregorianDateIntoJulianComponents()
	{
		var julian = new SysGlob.JulianCalendar();

		DateTime? julianResult = _calculator.GetDate(2026, julian);

		Assert.IsNotNull(julianResult);
		// Gregorian 2026-02-17 sits 13 days ahead of Julian; the calculator re-creates the
		// DateTime using the Julian calendar so the instant re-expands back to the same UTC day.
		DateTime expectedInstant = julian.ToDateTime(
			julian.GetYear(new DateTime(2026, 2, 17)),
			julian.GetMonth(new DateTime(2026, 2, 17)),
			julian.GetDayOfMonth(new DateTime(2026, 2, 17)),
			0, 0, 0, 0);
		Assert.AreEqual(expectedInstant, julianResult.Value);
		Assert.AreEqual(DateTimeKind.Unspecified, julianResult.Value.Kind);
	}

	/// <summary>
	/// Verifies that Lunar New Year always falls between 21 January and 20 February (inclusive)
	/// across the entire supported range.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallBetweenJan21AndFeb20()
	{
		for (int year = 1901; year <= 2100; year++)
		{
			DateTime? result = _calculator.GetDate(year);

			Assert.IsNotNull(result, $"expected a date for year {year}");
			DateTime date = result.Value;
			DateTime lower = new DateTime(year, 1, 21);
			DateTime upper = new DateTime(year, 2, 20);
			Assert.IsTrue(
				date >= lower && date <= upper,
				$"year {year}: expected Lunar New Year between {lower:d} and {upper:d} but got {date:d}");
		}
	}
}
