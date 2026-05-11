// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Add" />, when ValidInputsProvided, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetAddTestCases))]
    public void Add_WhenValidInputsProvided_ShouldReturnExpectedResult(DateTime input, int years, int months, double days, DateTime expected)
    {
        DateTime actual = input.Add(years, months, days);
        Assert.AreEqual(expected, actual);
    }

    public static IEnumerable<object[]> GetAddTestCases() =>
    [
        // Basic date increments
        new object[] { new DateTime(2024, 01, 01), 1, 0, 0, new DateTime(2025, 01, 01) },
        [new DateTime(2024, 01, 01), 0, 1, 0, new DateTime(2024, 02, 01)],
        [new DateTime(2024, 01, 01), 0, 0, 1, new DateTime(2024, 01, 02)],

        // Basic date decrements
        [new DateTime(2024, 01, 01), -1, 0, 0, new DateTime(2023, 01, 01)],
        [new DateTime(2024, 01, 01), 0, -1, 0, new DateTime(2023, 12, 01)],
        [new DateTime(2024, 01, 01), 0, 0, -1, new DateTime(2023, 12, 31)],

        // End-of-month alignment with leap year handling
        [new DateTime(2024, 01, 31), 0, 1, 0, new DateTime(2024, 02, 29)],
        [new DateTime(2023, 01, 31), 0, 1, 0, new DateTime(2023, 02, 28)],
        [new DateTime(2024, 02, 29), 1, 0, 0, new DateTime(2025, 02, 28)],

        // Composite negative offsets
        [new DateTime(2025, 05, 01), -1, -2, -1, new DateTime(2024, 02, 29)],

        // Fractional day offsets
        [new DateTime(2024, 01, 01), 0, 0, 1.5, new DateTime(2024, 01, 02, 12, 0, 0)],
        [new DateTime(2024, 01, 01), 0, 0, 0.25, new DateTime(2024, 01, 01, 6, 0, 0)],

        // Boundary conditions
        [DateTime.MinValue, 0, 0, 0, DateTime.MinValue],
        [DateTime.MaxValue, 0, 0, 0, DateTime.MaxValue],

        // Smallest valid time addition: 1 millisecond
        [new DateTime(2024, 01, 01, 0, 0, 0), 0, 0, 1.0 / 86400000.0, new DateTime(2024, 01, 01, 0, 0, 0, 1)],
        [new DateTime(2024, 01, 01, 0, 0, 0), 0, 0, 1.0 / 86400000, new DateTime(2024, 01, 01, 0, 0, 0, 1)],
    ];

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Add" />, when OutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetAddExceptionCases))]
    public void Add_WhenOutOfRange_ShouldThrowArgumentOutOfRangeException(DateTime input, int years, int months, double days)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            input.Add(years, months, days);
        });
    }

    public static IEnumerable<object[]> GetAddExceptionCases()
    {
        yield return new object[] { DateTime.MaxValue.AddDays(-1), 0, 0, 2 };
        yield return new object[] { DateTime.MaxValue, 1, 0, 0 };
        yield return new object[] { DateTime.MaxValue, 0, 1, 0 };
        yield return new object[] { DateTime.MinValue, -1, 0, 0 };
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Add" />, when AllParametersZero, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Add_WhenAllParametersZero_ShouldReturnSameDate()
    {
        DateTime input = new(2024, 1, 1, 12, 0, 0);
        Assert.AreEqual(input, input.Add(0, 0, 0));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Add" />, when AddingNegativeFractionalDay, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Add_WhenAddingNegativeFractionalDay_ShouldSubtractAccurately()
    {
        DateTime input = new(2024, 1, 2, 12, 0, 0);
        DateTime expected = new(2024, 1, 2, 6, 0, 0);
        Assert.AreEqual(expected, input.Add(0, 0, -0.25));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Add" />, when DaysIsLessThanEpsilon, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Add_WhenDaysIsLessThanEpsilon_ShouldBeIgnored()
    {
        DateTime input = new(2024, 1, 1, 0, 0, 0);
        DateTime actual = input.Add(0, 0, 1e-12); // Below epsilon
        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Add" />, when AddingToFeb28LeapYear, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Add_WhenAddingToFeb28LeapYear_ShouldIncludeFeb29()
    {
        DateTime input = new(2024, 2, 28);
        DateTime actual = input.Add(0, 0, 1);
        Assert.AreEqual(new DateTime(2024, 2, 29), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.Add" />, when InDstTransitionZone, returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow("2024-03-10T01:30:00", 0, 0, 1.0 / 24, "2024-03-10T03:30:00", "Pacific Standard Time")]
    [DataRow("2024-11-03T01:30:00", 0, 0, 1.0, "2024-11-04T01:30:00", "Pacific Standard Time")]
    public void Add_WhenInDstTransitionZone_ShouldRespectTimezone(string inputDate, int years, int months, double days, string expectedDate, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var input = DateTime.Parse(inputDate, null, DateTimeStyles.AssumeLocal);
        var unspecified = DateTime.SpecifyKind(input, DateTimeKind.Unspecified);
        var expected = DateTime.Parse(expectedDate);

        DateTime utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
        DateTime resultUtc = utc.AddYears(years).AddMonths(months).AddDays(days);
        DateTime resultLocal = TimeZoneInfo.ConvertTimeFromUtc(resultUtc, tz);

        Assert.AreEqual(expected, resultLocal);
    }
}
