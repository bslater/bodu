// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.Add.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{
    public static IEnumerable<object[]> GetAddTestCases()
    {
        yield return new object[] { new DateOnly(2024, 01, 01), 1, 0, 0, new DateOnly(2025, 01, 01) };
        yield return new object[] { new DateOnly(2024, 01, 01), 0, 1, 0, new DateOnly(2024, 02, 01) };
        yield return new object[] { new DateOnly(2024, 01, 01), 0, 0, 1, new DateOnly(2024, 01, 02) };
        yield return new object[] { new DateOnly(2024, 01, 01), -1, 0, 0, new DateOnly(2023, 01, 01) };
        yield return new object[] { new DateOnly(2024, 01, 01), 0, -1, 0, new DateOnly(2023, 12, 01) };
        yield return new object[] { new DateOnly(2024, 01, 01), 0, 0, -1, new DateOnly(2023, 12, 31) };
        yield return new object[] { new DateOnly(2024, 01, 31), 0, 1, 0, new DateOnly(2024, 02, 29) };
        yield return new object[] { new DateOnly(2023, 01, 31), 0, 1, 0, new DateOnly(2023, 02, 28) };
        yield return new object[] { new DateOnly(2024, 02, 29), 1, 0, 0, new DateOnly(2025, 02, 28) };
        yield return new object[] { new DateOnly(2025, 05, 01), -1, -2, -1, new DateOnly(2024, 02, 29) };
        yield return new object[] { new DateOnly(0001, 01, 01), 0, 0, 0, new DateOnly(0001, 01, 01) };
        yield return new object[] { new DateOnly(9999, 12, 31), 0, 0, 0, new DateOnly(9999, 12, 31) };
        yield return new object[] { new DateOnly(2000, 01, 01), 1000, 0, 0, new DateOnly(3000, 01, 01) };
        yield return new object[] { new DateOnly(2000, 01, 01), -999, 0, 0, new DateOnly(1001, 01, 01) };
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Add" />, when ValidInputsProvided, returns the expected value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DateTimeExtensionsTests.GetAddTestCases), typeof(DateTimeExtensionsTests))]
    public void Add_WhenValidInputsProvided_ShouldReturnExpectedResult(DateTime inputDateTime, int years, int months, double days, DateTime expectedDateTime)
    {
        var input = DateOnly.FromDateTime(inputDateTime);
        var expected = DateOnly.FromDateTime(expectedDateTime);
        DateOnly actual = input.Add(years, months, (int)days);
        Assert.AreEqual(expected, actual);
    }

    public static IEnumerable<object[]> GetAddExceptionCases()
    {
        yield return new object[] { DateOnly.MaxValue.ToString("yyyy-MM-dd"), 1, 0, 0 };
        yield return new object[] { DateOnly.MaxValue.ToString("yyyy-MM-dd"), 0, 1, 0 };
        yield return new object[] { DateOnly.MaxValue.ToString("yyyy-MM-dd"), 0, 0, 1 };
        yield return new object[] { DateOnly.MinValue.ToString("yyyy-MM-dd"), -1, 0, 0 };
        yield return new object[] { DateOnly.MinValue.ToString("yyyy-MM-dd"), 0, -1, 0 };
        yield return new object[] { DateOnly.MinValue.ToString("yyyy-MM-dd"), 0, 0, -1 };
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Add" />, when OutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetAddExceptionCases))]
    public void Add_WhenOutOfRange_ShouldThrowExactly(string inputDate, int years, int months, int days)
    {
        var input = DateOnly.Parse(inputDate);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            input.Add(years, months, days);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Add" />, when AllParametersZero, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Add_WhenAllParametersZero_ShouldReturnSameDate()
    {
        var input = new DateOnly(2024, 1, 1);
        DateOnly actual = input.Add(0, 0, 0);
        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.Add" />, when AddingToFeb28InLeapYear, returns the expected value.
    /// </summary>
    [TestMethod]
    public void Add_WhenAddingToFeb28InLeapYear_ShouldReturnFeb29()
    {
        var input = new DateOnly(2024, 2, 28);
        DateOnly actual = input.Add(0, 0, 1);
        Assert.AreEqual(new DateOnly(2024, 2, 29), actual);
    }
}
