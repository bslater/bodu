// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.TryParseExact.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="true" /> and
    /// correctly parses a binary string when the format specifier is <c>'0'</c>. This is a regression
    /// test for a defect where this documented specifier was not recognised, causing the method to return
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenBinaryFormatSpecifierIs0_ShouldReturnTrueAndSetCorrectDays()
    {
        var success = WeekPattern.TryParseExact("0111110", "0", out WeekPattern result);

        Assert.IsTrue(success, "Format specifier '0' should be recognised as binary.");
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="true" /> and
    /// correctly parses a binary string when the format specifier is <c>"01"</c>.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenBinaryFormatSpecifierIs01_ShouldReturnTrueAndSetCorrectDays()
    {
        var success = WeekPattern.TryParseExact("0111110", "01", out WeekPattern result);

        Assert.IsTrue(success, "Format specifier \"01\" should be recognised as binary.");
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="true" /> and
    /// correctly parses a binary string when the format specifier is <c>'1'</c>.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenBinaryFormatSpecifierIs1_ShouldReturnTrueAndSetCorrectDays()
    {
        var success = WeekPattern.TryParseExact("0111110", "1", out WeekPattern result);

        Assert.IsTrue(success, "Format specifier '1' should be recognised as binary.");
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="false" /> when the
    /// binary input contains an invalid character.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenBinaryInputContainsInvalidCharacter_ShouldReturnFalseAndSetEmpty()
    {
        var success = WeekPattern.TryParseExact("0111X10", "0", out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="false" /> and sets
    /// the result to <see cref="WeekPattern.Empty" /> when the format is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenFormatIsNull_ShouldReturnFalseAndSetEmpty()
    {
        var success = WeekPattern.TryParseExact("_M_W_F_", null, out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="true" /> and sets
    /// the correct result for a valid Sunday-first input with the <c>'S'</c> format specifier.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenFormatIsSundayFirstAndInputIsValid_ShouldReturnTrueAndSetCorrectDays()
    {
        var success = WeekPattern.TryParseExact("_M_W_F_", "S", out WeekPattern result);

        Assert.IsTrue(success);
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="false" /> and sets
    /// the result to <see cref="WeekPattern.Empty" /> when the format is unrecognised.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenFormatIsUnrecognised_ShouldReturnFalseAndSetEmpty()
    {
        var success = WeekPattern.TryParseExact("_M_W_F_", "Z", out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }
    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact(string, string, out WeekPattern)" /> returns
    /// the expected success flag and value for various valid and invalid combinations of input and format.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetTryParseExactTestData), typeof(WeekPatternTests))]
    public void TryParseExact_WhenGivenInputAndFormat_ShouldReturnExpectedResultAndParsedValue(
        string input, string format, byte expected, bool isValid)
    {
        var success = WeekPattern.TryParseExact(input, format, out WeekPattern actual);

        Assert.AreEqual(isValid, success);

        if (success)
            Assert.AreEqual(expected, actual);
        else
            Assert.AreEqual(WeekPattern.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="false" /> and sets
    /// the result to <see cref="WeekPattern.Empty" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenInputIsNull_ShouldReturnFalseAndSetEmpty()
    {
        var success = WeekPattern.TryParseExact(null, "S", out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }

}
