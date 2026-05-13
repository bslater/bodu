// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VesakNotableDateAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="VesakNotableDateAlgorithm" />.
/// </summary>
[TestClass]
public sealed class VesakNotableDateAlgorithmTests
{
	private readonly VesakNotableDateAlgorithm _algorithm = new();

	/// <summary>
	/// Verifies that requesting Vesak with a year below one throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	[TestMethod]
	public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = _algorithm.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that Vesak returns known correct dates for recent years. The expected dates match the Thai Visakha
	/// Bucha public holiday, which is the first full moon on or after 1 May.
	/// </summary>
	[DataRow(2022, 5, 16)]
	[DataRow(2023, 5, 5)]
	[DataRow(2024, 5, 23)]
	[DataRow(2025, 5, 12)]
	[TestMethod]
	public void GetDate_WhenGivenKnownYears_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		DateTime? result = _algorithm.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value,
			$"Vesak {year}: expected {expectedMonth:D2}/{expectedDay:D2}, got {result.Value:yyyy-MM-dd}");
	}

	/// <summary>
	/// Verifies that for every year in the range 1901–2100 the result falls in May or early June, consistent with
	/// the Visakha Bucha definition of the first full moon on or after 1 May.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallInMayOrEarlyJune()
	{
		for (int year = 1901; year <= 2100; year++)
		{
			DateTime? result = _algorithm.GetDate(year);

			Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
			Assert.IsTrue(result!.Value.Month is 5 or 6,
				$"Expected May or June for year {year}, got month {result.Value.Month}.");
			Assert.IsTrue(result.Value >= new DateTime(year, 5, 1),
				$"Expected date on or after May 1 for year {year}, got {result.Value:yyyy-MM-dd}.");
		}
	}

	/// <summary>
	/// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		DateTime? result = _algorithm.GetDate(2024);

		Assert.IsNotNull(result);
		Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
	}

	/// <summary>
	/// Verifies that supplying an explicit <see cref="System.Globalization.GregorianCalendar" /> matches the default
	/// (null) calendar path.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsExplicitlyGregorian_ShouldMatchDefaultPath()
	{
		DateTime? withDefault = _algorithm.GetDate(2024);
		DateTime? withGregorian = _algorithm.GetDate(2024, new System.Globalization.GregorianCalendar());

		Assert.AreEqual(withDefault, withGregorian);
	}

	/// <summary>
	/// Verifies that supplying a <see cref="System.Globalization.JulianCalendar" /> projects the result through the
	/// non-Gregorian projection branch.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsJulian_ShouldProjectThroughTargetCalendar()
	{
		System.Globalization.JulianCalendar julian = new();

		DateTime? result = _algorithm.GetDate(2024, julian);

		Assert.IsNotNull(result);
		Assert.AreEqual(2024, julian.GetYear(result!.Value));
	}
}
