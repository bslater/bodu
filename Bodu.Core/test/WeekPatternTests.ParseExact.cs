// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.ParseExact.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that all recognised binary format specifiers (<c>'0'</c>, <c>'1'</c>, <c>'B'</c>,
    /// <c>"01"</c>) produce identical results when applied to the same input string.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenAllBinarySpecifiersAppliedToSameInput_ShouldProduceIdenticalResults()
    {
        const string input = "1010101";

        var r0 = WeekPattern.ParseExact(input, "0");
        var r1 = WeekPattern.ParseExact(input, "1");
        var rB = WeekPattern.ParseExact(input, "B");
        var r01 = WeekPattern.ParseExact(input, "01");

        Assert.AreEqual(r0, r1, "Formats '0' and '1' should yield the same result.");
        Assert.AreEqual(r0, rB, "Formats '0' and 'B' should yield the same result.");
        Assert.AreEqual(r0, r01, "Formats '0' and \"01\" should yield the same result.");
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>'0'</c>. This is a regression test for a defect where this
    /// documented specifier was not recognised and caused a <see cref="FormatException" /> to be thrown.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIs0_ShouldParseBinaryString()
    {
        var result = WeekPattern.ParseExact("0111110", "0");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>"01"</c>.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIs01_ShouldParseBinaryString()
    {
        var result = WeekPattern.ParseExact("0111110", "01");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>'1'</c>.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIs1_ShouldParseBinaryString()
    {
        var result = WeekPattern.ParseExact("0111110", "1");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>'B'</c>.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIsB_ShouldParseBinaryString()
    {
        var result = WeekPattern.ParseExact("0111110", "B");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when a binary input contains an invalid character.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryInputContainsInvalidCharacter_ShouldThrowException() => Assert.ThrowsExactly<FormatException>(() => { _ = WeekPattern.ParseExact("0111X10", "0"); });

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when the format string is empty.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsEmpty_ShouldThrowException() => Assert.ThrowsExactly<FormatException>(() => { _ = WeekPattern.ParseExact("_M_W_F_", string.Empty); });

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses a
    /// Monday-first string using the <c>'M'</c> format specifier.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsMondayFirst_ShouldSetCorrectDays()
    {
        // "MTWTF__" = Monday-first: Monday through Friday selected.
        var result = WeekPattern.ParseExact("MTWTF__", "M");

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> with the two-character dash
    /// format specifier (<c>"MD"</c>) correctly parses a Monday-first string.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsMondayFirstWithDash_ShouldSetCorrectDays()
    {
        var result = WeekPattern.ParseExact("M-W-F--", "MD");

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsFalse(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when the format is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsNull_ShouldThrowException() => Assert.ThrowsExactly<ArgumentNullException>(() => { _ = WeekPattern.ParseExact("_M_W_F_", null); });

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses a
    /// Sunday-first string using the <c>'S'</c> format specifier.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsSundayFirst_ShouldSetCorrectDays()
    {
        var result = WeekPattern.ParseExact("_M_W_F_", "S");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsFalse(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when the format string is unrecognised.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsUnrecognised_ShouldThrowException() => Assert.ThrowsExactly<FormatException>(() => { _ = WeekPattern.ParseExact("_M_W_F_", "Z"); });

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenInputIsNull_ShouldThrowException() => Assert.ThrowsExactly<ArgumentNullException>(() => { _ = WeekPattern.ParseExact(null!, "S"); });

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when the format string is invalid or unsupported.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekPatternTests.GetInvalidFormatSpecifierTestData), typeof(WeekPatternTests))]
    public void ParseExact_WhenInvalidFormatSpecifier_ShouldThrowFormatException(string input, string format) => Assert.ThrowsExactly<FormatException>(() => { _ = WeekPattern.ParseExact(input, format); });

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses
    /// binary-formatted strings (e.g., <c>"1010101"</c>) across all 128 valid permutations when the
    /// <c>'B'</c> format specifier is provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekPatternTests.GetAllBitmaskPermutationTestData), typeof(WeekPatternTests))]
    public void ParseExact_WhenValidBinaryInput_ShouldReturnExpected(byte expected, string _, string input) => Assert.AreEqual(expected, WeekPattern.ParseExact(input, "B"));

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses Monday-first
    /// symbol strings (e.g., <c>"MTWTFSS"</c>) across all 128 valid permutations when the <c>'M'</c>
    /// format specifier is provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekPatternTests.GetAllBitmaskPermutationWithMondaySymbolsTestData), typeof(WeekPatternTests))]
    public void ParseExact_WhenValidMondaySymbolInput_ShouldReturnExpected(byte expected, string input, string _) => Assert.AreEqual(expected, WeekPattern.ParseExact(input, "M"));
    /// <summary>
    /// Verifies that <see cref="WeekPattern.ParseExact(string, string)" /> correctly parses Sunday-first
    /// symbol strings (e.g., <c>"SMTWTFS"</c>) across all 128 valid permutations when the <c>'S'</c>
    /// format specifier is provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(WeekPatternTests.GetAllBitmaskPermutationTestData), typeof(WeekPatternTests))]
    public void ParseExact_WhenValidSundaySymbolInput_ShouldReturnExpected(byte expected, string input, string _) => Assert.AreEqual(expected, WeekPattern.ParseExact(input, "S"));

}
