// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.TryParse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParse(string, out WeekPattern)" /> returns the expected
    /// success flag and parsed value for valid and invalid inputs using auto-detected format.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetTryParseTestData), typeof(WeekPatternTests))]
    public void TryParse_WhenGivenInput_ShouldReturnExpectedResultAndParsedValue(string input, byte expected, bool isValid)
    {
        var success = WeekPattern.TryParse(input, out WeekPattern actual);

        Assert.AreEqual(isValid, success);

        if (success)
            Assert.AreEqual(expected, (byte)actual);
        else
            Assert.AreEqual(WeekPattern.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParse" /> returns <see langword="false" /> and sets the
    /// result to <see cref="WeekPattern.Empty" /> when the input contains an unrecognised character.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputContainsInvalidCharacter_ShouldReturnFalseAndSetEmpty()
    {
        var success = WeekPattern.TryParse("SMTWTFX", out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParse" /> returns <see langword="false" /> and sets the
    /// result to <see cref="WeekPattern.Empty" /> when the input has an invalid length.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputHasInvalidLength_ShouldReturnFalseAndSetEmpty()
    {
        var success = WeekPattern.TryParse("SMTWTF", out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParse" /> correctly auto-detects and parses a binary
    /// input string.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsBinary_ShouldReturnTrueAndSetCorrectDays()
    {
        var success = WeekPattern.TryParse("0111110", out WeekPattern result);

        Assert.IsTrue(success);
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParse" /> returns <see langword="false" /> and sets the
    /// result to <see cref="WeekPattern.Empty" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsNull_ShouldReturnFalseAndSetEmpty()
    {
        var success = WeekPattern.TryParse(null, out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParse" /> returns <see langword="true" /> and sets the
    /// correct result for a valid Sunday-first input string.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValidSundayFirst_ShouldReturnTrueAndSetCorrectDays()
    {
        var success = WeekPattern.TryParse("_M_W_F_", out WeekPattern result);

        Assert.IsTrue(success);
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParse" /> returns <see langword="true" /> and sets an
    /// empty result for an all-unselected string.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputRepresentsNoDaysSelected_ShouldReturnTrueAndSetEmpty()
    {
        var success = WeekPattern.TryParse("_______", out WeekPattern result);

        Assert.IsTrue(success);
        Assert.AreEqual(0, result.Count);
    }

}
