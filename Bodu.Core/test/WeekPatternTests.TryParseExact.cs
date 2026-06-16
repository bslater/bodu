// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.TryParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

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
        bool success = WeekPattern.TryParseExact("0111110", "0", out WeekPattern result);

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
        bool success = WeekPattern.TryParseExact("0111110", "01", out WeekPattern result);

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
        bool success = WeekPattern.TryParseExact("0111110", "1", out WeekPattern result);

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
        bool success = WeekPattern.TryParseExact("0111X10", "0", out WeekPattern result);

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
        bool success = WeekPattern.TryParseExact("_M_W_F_", null, out WeekPattern result);

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
        bool success = WeekPattern.TryParseExact("_M_W_F_", "S", out WeekPattern result);

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
        bool success = WeekPattern.TryParseExact("_M_W_F_", "Z", out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }
    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact(string, string, out WeekPattern)" /> returns
    /// <see langword="true" /> and yields the expected bitmask for every <see cref="WeekPatternParseKat" />
    /// row produced by <see cref="GetTryParseExactValidKats" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying the valid input, explicit format, and expected bitmask.</param>
    [TestMethod]
    [DynamicData(
        nameof(GetTryParseExactValidKats),
        typeof(WeekPatternTests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParseExact_WhenInputIsValid_ShouldReturnTrueAndSetExpectedValue(WeekPatternParseKat kat)
    {
        bool success = WeekPattern.TryParseExact(kat.Input, kat.Format, out WeekPattern actual);

        Assert.IsTrue(success);
        Assert.AreEqual(kat.Expected, (byte)actual);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact(string, string, out WeekPattern)" /> returns
    /// <see langword="false" /> and sets the result to <see cref="WeekPattern.Empty" /> for every
    /// <see cref="InvalidWeekPatternParseKat" /> row produced by <see cref="GetTryParseExactInvalidKats" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying an input/format combination expected to fail parsing.</param>
    [TestMethod]
    [DynamicData(
        nameof(GetTryParseExactInvalidKats),
        typeof(WeekPatternTests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParseExact_WhenInputIsInvalid_ShouldReturnFalseAndSetEmpty(InvalidWeekPatternParseKat kat)
    {
        bool success = WeekPattern.TryParseExact(kat.Input, kat.Format, out WeekPattern actual);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.TryParseExact" /> returns <see langword="false" /> and sets
    /// the result to <see cref="WeekPattern.Empty" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenInputIsNull_ShouldReturnFalseAndSetEmpty()
    {
        bool success = WeekPattern.TryParseExact(null, "S", out WeekPattern result);

        Assert.IsFalse(success);
        Assert.AreEqual(WeekPattern.Empty, result);
    }

}
