// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduLunarNotableDateAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="HinduLunarNotableDateAlgorithm" />.
/// </summary>
/// <remarks>
/// <para>
/// Expected dates are taken from widely published Indian panchanga sources. Results from this algorithm may differ
/// from panchanga-exact dates by zero, one, or occasionally two calendar days due to the approximate tithi calculation
/// method used. Test assertions therefore allow a tolerance of ±2 days for astronomical festivals.
/// </para>
/// </remarks>
[TestClass]
public sealed class HinduLunarNotableDateAlgorithmTests
{
	/// <summary>
	/// Verifies that an undefined <see cref="HinduLunarMonth" /> value throws <see cref="ArgumentException" />.
	/// </summary>
	[TestMethod]
	public void Constructor_WhenMonthIsUndefined_ShouldThrowArgumentException()
	{
		var ex = Assert.ThrowsExactly<ArgumentException>(() =>
		{
			_ = new HinduLunarNotableDateAlgorithm((HinduLunarMonth)99, HinduPaksha.Shukla, 1);
		});

		Assert.AreEqual("month", ex.ParamName);
	}

	/// <summary>
	/// Verifies that an undefined <see cref="HinduPaksha" /> value throws <see cref="ArgumentException" />.
	/// </summary>
	[TestMethod]
	public void Constructor_WhenPakshaIsUndefined_ShouldThrowArgumentException()
	{
		var ex = Assert.ThrowsExactly<ArgumentException>(() =>
		{
			_ = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, (HinduPaksha)99, 1);
		});

		Assert.AreEqual("paksha", ex.ParamName);
	}

	/// <summary>
	/// Verifies that a tithi outside the range 1–15 throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(16)]
	[TestMethod]
	public void Constructor_WhenTithiOutOfRange_ShouldThrowArgumentOutOfRangeException(int tithi)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, tithi);
		});

		Assert.AreEqual("tithi", ex.ParamName);
	}

	/// <summary>
	/// Verifies that requesting a date with a year below one throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(-1)]
	[TestMethod]
	public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
	{
		var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);

		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = sut.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that Diwali (Amavasya / Krishna Paksha Chaturdashi of Kartik, i.e. the new moon) falls
	/// within ±2 days of the known panchanga date. The approximation method targets the day on which
	/// the new moon transition occurs near the end of Krishna Paksha.
	/// </summary>
	[DataRow(2022, 10, 24)]
	[DataRow(2023, 11, 13)]
	[DataRow(2024, 11, 1)]
	[TestMethod]
	public void GetDate_WhenDiwali_ShouldFallWithinTwoDaysOfKnownPanchangaDate(int year, int knownMonth, int knownDay)
	{
		// Diwali is on Amavasya (new moon day) of Kartik: Krishna Paksha, tithi 15 (Amavasya).
		var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);

		DateTime? result = sut.GetDate(year);
		DateTime expected = new DateTime(year, knownMonth, knownDay);

		Assert.IsNotNull(result);
		int dayDiff = Math.Abs((result!.Value - expected).Days);
		Assert.IsTrue(dayDiff <= 2,
			$"Diwali {year}: expected within ±2 days of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}.");
	}

	/// <summary>
	/// Verifies that Holi (Purnima of Phalguna — Shukla Paksha, tithi 15) falls within ±2 days of the known date.
	/// </summary>
	[DataRow(2022, 3, 18)]
	[DataRow(2023, 3, 7)]
	[DataRow(2024, 3, 25)]
	[TestMethod]
	public void GetDate_WhenHoli_ShouldFallWithinTwoDaysOfKnownPanchangaDate(int year, int knownMonth, int knownDay)
	{
		// Holi main day (Holika Dahan) is on Purnima (full moon) of Phalguna.
		var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Phalguna, HinduPaksha.Shukla, 15);

		DateTime? result = sut.GetDate(year);
		DateTime expected = new DateTime(year, knownMonth, knownDay);

		Assert.IsNotNull(result);
		int dayDiff = Math.Abs((result!.Value - expected).Days);
		Assert.IsTrue(dayDiff <= 2,
			$"Holi {year}: expected within ±2 days of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}.");
	}

	/// <summary>
	/// Verifies that Navaratri start (Shukla Paksha Pratipada of Ashvin, i.e. tithi 1) falls within ±2 days
	/// of the known panchanga date.
	/// </summary>
	[DataRow(2022, 9, 26)]
	[DataRow(2023, 10, 15)]
	[DataRow(2024, 10, 3)]
	[TestMethod]
	public void GetDate_WhenNavaratri_ShouldFallWithinTwoDaysOfKnownPanchangaDate(int year, int knownMonth, int knownDay)
	{
		var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Ashvin, HinduPaksha.Shukla, 1);

		DateTime? result = sut.GetDate(year);
		DateTime expected = new DateTime(year, knownMonth, knownDay);

		Assert.IsNotNull(result);
		int dayDiff = Math.Abs((result!.Value - expected).Days);
		Assert.IsTrue(dayDiff <= 2,
			$"Navaratri {year}: expected within ±2 days of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}.");
	}

	/// <summary>
	/// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		var sut = new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15);

		DateTime? result = sut.GetDate(2024);

		Assert.IsNotNull(result);
		Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
	}
}
