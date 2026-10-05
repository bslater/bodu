// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.TryParse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

public partial class DayOfWeekSetTests
{

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse(string, out DayOfWeekSet)" /> returns
    /// <see langword="true" /> and yields the expected bitmask for every
    /// <see cref="DayOfWeekSetParseKat" /> row produced by <see cref="GetTryParseValidKats" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying the valid input, optional format, and expected bitmask.</param>
    [TestMethod]
    [DynamicData(
        nameof(GetTryParseValidKats),
        typeof(DayOfWeekSetTests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenInputIsValid_ShouldReturnTrueAndSetExpectedValue(DayOfWeekSetParseKat kat)
    {
        bool success = DayOfWeekSet.TryParse(kat.Input, out DayOfWeekSet actual);

        Assert.IsTrue(success);
        Assert.AreEqual(SundayFirst(kat.Expected), actual);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse(string, out DayOfWeekSet)" /> returns
    /// <see langword="false" /> and sets the result to <see cref="DayOfWeekSet.Empty" /> for every
    /// <see cref="InvalidDayOfWeekSetParseKat" /> row produced by <see cref="GetTryParseInvalidKats" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a malformed input expected to fail parsing.</param>
    [TestMethod]
    [DynamicData(
        nameof(GetTryParseInvalidKats),
        typeof(DayOfWeekSetTests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryParse_WhenInputIsInvalid_ShouldReturnFalseAndSetEmpty(InvalidDayOfWeekSetParseKat kat)
    {
        bool success = DayOfWeekSet.TryParse(kat.Input, out DayOfWeekSet actual);

        Assert.IsFalse(success);
        Assert.AreEqual(DayOfWeekSet.Empty, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse" /> returns <see langword="false" /> and sets the
    /// result to <see cref="DayOfWeekSet.Empty" /> when the input contains an unrecognised character.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputContainsInvalidCharacter_ShouldReturnFalseAndSetEmpty()
    {
        bool success = DayOfWeekSet.TryParse("SMTWTFX", out DayOfWeekSet result);

        Assert.IsFalse(success);
        Assert.AreEqual(DayOfWeekSet.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse" /> returns <see langword="false" /> and sets the
    /// result to <see cref="DayOfWeekSet.Empty" /> when the input has an invalid length.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputHasInvalidLength_ShouldReturnFalseAndSetEmpty()
    {
        bool success = DayOfWeekSet.TryParse("SMTWTF", out DayOfWeekSet result);

        Assert.IsFalse(success);
        Assert.AreEqual(DayOfWeekSet.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse" /> correctly auto-detects and parses a binary
    /// input string.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsBinary_ShouldReturnTrueAndSetCorrectDays()
    {
        bool success = DayOfWeekSet.TryParse("0111110", out DayOfWeekSet result);

        Assert.IsTrue(success);
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse" /> returns <see langword="false" /> and sets the
    /// result to <see cref="DayOfWeekSet.Empty" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsNull_ShouldReturnFalseAndSetEmpty()
    {
        bool success = DayOfWeekSet.TryParse(null, out DayOfWeekSet result);

        Assert.IsFalse(success);
        Assert.AreEqual(DayOfWeekSet.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse" /> returns <see langword="true" /> and sets the
    /// correct result for a valid Sunday-first input string.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValidSundayFirst_ShouldReturnTrueAndSetCorrectDays()
    {
        bool success = DayOfWeekSet.TryParse("_M_W_F_", out DayOfWeekSet result);

        Assert.IsTrue(success);
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.TryParse" /> returns <see langword="true" /> and sets an
    /// empty result for an all-unselected string.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputRepresentsNoDaysSelected_ShouldReturnTrueAndSetEmpty()
    {
        bool success = DayOfWeekSet.TryParse("_______", out DayOfWeekSet result);

        Assert.IsTrue(success);
        Assert.AreEqual(0, result.Count);
    }

}
