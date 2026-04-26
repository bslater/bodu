// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IslamicNotableDateCalculatorTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Calculators;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="IslamicNotableDateCalculator" />.
/// </summary>
[TestClass]
public sealed class IslamicNotableDateCalculatorTests
{
	/// <summary>
	/// Verifies that a Hijri month outside the range 1–12 throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(13)]
	[TestMethod]
	public void Constructor_WhenHijriMonthOutOfRange_ShouldThrowArgumentOutOfRangeException(int hijriMonth)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = new IslamicNotableDateCalculator(hijriMonth, 1);
		});

		Assert.AreEqual("hijriMonth", ex.ParamName);
	}

	/// <summary>
	/// Verifies that a Hijri day outside the range 1–30 throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(31)]
	[TestMethod]
	public void Constructor_WhenHijriDayOutOfRange_ShouldThrowArgumentOutOfRangeException(int hijriDay)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = new IslamicNotableDateCalculator(1, hijriDay);
		});

		Assert.AreEqual("hijriDay", ex.ParamName);
	}

	/// <summary>
	/// Verifies that requesting a date with a Gregorian year below one throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	[TestMethod]
	public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
	{
		var sut = new IslamicNotableDateCalculator(1, 1);

		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = sut.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that Eid al-Fitr (1 Shawwal = month 10, day 1) returns known tabular dates for recent years.
	/// The HijriCalendar tabular dates are used; results may differ from moon-sighted dates by up to two days.
	/// </summary>
	[DataRow(2022, 5, 2)]
	[DataRow(2023, 4, 21)]
	[DataRow(2024, 4, 10)]
	[TestMethod]
	public void GetDate_WhenEidAlFitr_ShouldReturnExpectedTabularDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new IslamicNotableDateCalculator(10, 1);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that Eid al-Adha (10 Dhul Hijja = month 12, day 10) returns known tabular dates for recent years.
	/// </summary>
	[DataRow(2022, 7, 9)]
	[DataRow(2023, 6, 28)]
	[DataRow(2024, 6, 16)]
	[TestMethod]
	public void GetDate_WhenEidAlAdha_ShouldReturnExpectedTabularDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new IslamicNotableDateCalculator(12, 10);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that Ramadan start (1 Ramadan = month 9, day 1) returns known tabular dates for recent years.
	/// </summary>
	[DataRow(2022, 4, 2)]
	[DataRow(2023, 3, 23)]
	[DataRow(2024, 3, 11)]
	[TestMethod]
	public void GetDate_WhenRamadanStart_ShouldReturnExpectedTabularDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new IslamicNotableDateCalculator(9, 1);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		var sut = new IslamicNotableDateCalculator(10, 1);

		DateTime? result = sut.GetDate(2024);

		Assert.IsNotNull(result);
		Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
	}

	/// <summary>
	/// Verifies that supplying an explicit <see cref="SysGlob.GregorianCalendar" /> produces the same date as passing
	/// <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsGregorian_ShouldMatchDefaultResult()
	{
		var sut = new IslamicNotableDateCalculator(10, 1);

		DateTime? withNull = sut.GetDate(2024, calendar: null);
		DateTime? withGregorian = sut.GetDate(2024, new SysGlob.GregorianCalendar());

		Assert.IsNotNull(withNull);
		Assert.IsNotNull(withGregorian);
		Assert.AreEqual(withNull!.Value, withGregorian!.Value);
	}

	/// <summary>
	/// Verifies that the Hijri date advances backwards through the Gregorian year over successive years, reflecting
	/// the shorter Islamic lunar year.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenIteratingSuccessiveYears_EidAlFitrShouldAdvanceBackwardsByApproximatelyElevenDays()
	{
		var sut = new IslamicNotableDateCalculator(10, 1);

		DateTime? year2022 = sut.GetDate(2022);
		DateTime? year2023 = sut.GetDate(2023);
		DateTime? year2024 = sut.GetDate(2024);

		Assert.IsNotNull(year2022);
		Assert.IsNotNull(year2023);
		Assert.IsNotNull(year2024);

		int diff2022to2023 = (year2022!.Value - year2023!.Value).Days;
		int diff2023to2024 = (year2023!.Value - year2024!.Value).Days;

		// The Islamic calendar advances by approximately 10–12 days per Gregorian year.
		Assert.IsTrue(diff2022to2023 >= 9 && diff2022to2023 <= 13,
			$"Expected ~11-day advance from 2022 to 2023, got {diff2022to2023} days.");
		Assert.IsTrue(diff2023to2024 >= 9 && diff2023to2024 <= 13,
			$"Expected ~11-day advance from 2023 to 2024, got {diff2023to2024} days.");
	}
}
