// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateCalculatorTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Calculators
{
	/// <summary>
	/// Verifies the correctness and behavior of <see cref="EasterSundayNotableDateCalculator" />.
	/// </summary>
	[TestClass]
	public sealed class EasterSundayNotableDateCalculatorTests
	{
		private readonly EasterSundayNotableDateCalculator _calculator = new();

		/// <summary>
		/// Provides known year, expected Easter Sunday dates, and calendar system for validation. Grouped logically into Boundary, Julian,
		/// and Gregorian.
		/// </summary>
		public static IEnumerable<object[]> GetKnownEasterSundayTestData()
		{
			// Helper only for non-null calendars
			static SysGlob.Calendar CalendarOf(string type) => type switch
			{
				"Gregorian" => new SysGlob.GregorianCalendar(),
				"Julian" => new SysGlob.JulianCalendar(),
				_ => throw new ArgumentException($"Unsupported calendar type: {type}", nameof(type))
			};

			// Boundary Tests
			yield return new object[] { 1, new DateTime(1, 4, 5, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1, new DateTime(1, 4, 5, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
			yield return new object[] { 9999, new DateTime(9999, 4, 12, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 9999, new DateTime(9999, 4, 12, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };

			// Julian Calendar
			yield return new object[] { 1000, new DateTime(1000, 4, 9, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1000, new DateTime(1000, 4, 9, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
			yield return new object[] { 1200, new DateTime(1200, 4, 3, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1200, new DateTime(1200, 4, 3, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
			yield return new object[] { 1500, new DateTime(1500, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1500, new DateTime(1500, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };

			// Gregorian Calendar
			yield return new object[] { 1583, new DateTime(1583, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1583, new DateTime(1583, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
			yield return new object[] { 1600, new DateTime(1600, 4, 2, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1600, new DateTime(1600, 4, 2, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
			yield return new object[] { 1700, new DateTime(1700, 4, 11, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1700, new DateTime(1700, 4, 11, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
			yield return new object[] { 1800, new DateTime(1800, 4, 13, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1800, new DateTime(1800, 4, 13, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
			yield return new object[] { 1900, new DateTime(1900, 4, 15, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 1900, new DateTime(1900, 4, 15, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
			yield return new object[] { 2000, new DateTime(2000, 4, 23, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 2000, new DateTime(2000, 4, 23, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
			yield return new object[] { 2024, new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 2024, new DateTime(2024, 3, 31, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };
			yield return new object[] { 2024, new DateTime(2024, 4, 7, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Julian") };
			yield return new object[] { 2025, new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 2025, new DateTime(2025, 4, 20, 0, 0, 0, DateTimeKind.Unspecified), CalendarOf("Gregorian") };

			// Future Years
			yield return new object[] { 2030, new DateTime(2030, 4, 21, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 2040, new DateTime(2040, 4, 1, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 2050, new DateTime(2050, 4, 10, 0, 0, 0, DateTimeKind.Unspecified), null! };
			yield return new object[] { 2100, new DateTime(2100, 3, 28, 0, 0, 0, DateTimeKind.Unspecified), null! };
		}

		/// <summary>
		/// Verifies that Easter Sunday is correctly calculated across a known set of years and calendars.
		/// </summary>
		[DataTestMethod]
		[DynamicData(nameof(GetKnownEasterSundayTestData), DynamicDataSourceType.Method)]
		public void GetDate_WhenGivenKnownYears_ShouldReturnExpectedDate(int year, DateTime expectedDate, SysGlob.Calendar? calendar)
		{
			var calculator = new EasterSundayNotableDateCalculator();
			var result = calculator.GetDate(year, calendar);

			Assert.IsNotNull(result);
			Assert.AreEqual(expectedDate, result);
		}

		/// <summary>
		/// Verifies that requesting Easter Sunday for year 0 throws an <see cref="ArgumentOutOfRangeException" />.
		/// </summary>
		[DataTestMethod]
		[DataRow(-1)]
		[DataRow(0)]
		public void GetDate_WhenYearInvalid_ShouldThrowArgumentOutOfRangeException(int year)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				_ = _calculator.GetDate(year, null);
			});
		}

		/// <summary>
		/// Verifies that repeated calls to GetDate for the same year and calendar return the same cached result.
		/// </summary>
		[TestMethod]
		public void GetDate_WhenCalledTwice_ShouldReturnCachedResult()
		{
			int year = 2026;

			var firstCall = _calculator.GetDate(year, null);
			var secondCall = _calculator.GetDate(year, null);

			Assert.AreEqual(firstCall, secondCall);
		}

		/// <summary>
		/// Verifies that when no calendar is provided, the resulting <see cref="DateTime" /> has <see cref="DateTimeKind.Unspecified" />.
		/// </summary>
		[TestMethod]
		public void GetDate_WhenNullCalendar_ShouldReturnUnspecifiedKindDate()
		{
			int year = 2030;

			var result = _calculator.GetDate(year, null);

			Assert.AreEqual(DateTimeKind.Unspecified, result?.Kind);
		}
	}
}