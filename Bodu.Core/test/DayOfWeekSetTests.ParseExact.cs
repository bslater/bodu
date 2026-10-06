// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.ParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class DayOfWeekSetTests
{

    /// <summary>
    /// Verifies that all recognised binary format specifiers (<c>'0'</c>, <c>'1'</c>, <c>'B'</c>,
    /// <c>"01"</c>) produce identical results when applied to the same input string.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenAllBinarySpecifiersAppliedToSameInput_ShouldProduceIdenticalResults()
    {
        const string input = "1010101";

        var r0 = DayOfWeekSet.ParseExact(input, "0");
        var r1 = DayOfWeekSet.ParseExact(input, "1");
        var rB = DayOfWeekSet.ParseExact(input, "B");
        var r01 = DayOfWeekSet.ParseExact(input, "01");

        Assert.AreEqual(r0, r1, "Formats '0' and '1' should yield the same result.");
        Assert.AreEqual(r0, rB, "Formats '0' and 'B' should yield the same result.");
        Assert.AreEqual(r0, r01, "Formats '0' and \"01\" should yield the same result.");
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>'0'</c>. This is a regression test for a defect where this
    /// documented specifier was not recognised and caused a <see cref="FormatException" /> to be thrown.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIs0_ShouldParseBinaryString()
    {
        var result = DayOfWeekSet.ParseExact("0111110", "0");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>"01"</c>.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIs01_ShouldParseBinaryString()
    {
        var result = DayOfWeekSet.ParseExact("0111110", "01");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>'1'</c>.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIs1_ShouldParseBinaryString()
    {
        var result = DayOfWeekSet.ParseExact("0111110", "1");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses a binary
    /// string when the format specifier is <c>'B'</c>.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryFormatSpecifierIsB_ShouldParseBinaryString()
    {
        var result = DayOfWeekSet.ParseExact("0111110", "B");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when a binary input contains an invalid character.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryInputContainsInvalidCharacter_ShouldThrowExactly() => Assert.ThrowsExactly<FormatException>(() => { _ = DayOfWeekSet.ParseExact("0111X10", "0"); });

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when the format string is empty.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsEmpty_ShouldThrowExactly() => Assert.ThrowsExactly<FormatException>(() => { _ = DayOfWeekSet.ParseExact("_M_W_F_", string.Empty); });

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses a
    /// Monday-first string using the <c>'M'</c> format specifier.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsMondayFirst_ShouldSetCorrectDays()
    {
        // "MTWTF__" = Monday-first: Monday through Friday selected.
        var result = DayOfWeekSet.ParseExact("MTWTF__", "M");

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> with the two-character dash
    /// format specifier (<c>"MD"</c>) correctly parses a Monday-first string.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsMondayFirstWithDash_ShouldSetCorrectDays()
    {
        var result = DayOfWeekSet.ParseExact("M-W-F--", "MD");

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsFalse(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when the format is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DayOfWeekSet.ParseExact("_M_W_F_", null!);
        });

        Assert.AreEqual("format", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses a
    /// Sunday-first string using the <c>'S'</c> format specifier.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsSundayFirst_ShouldSetCorrectDays()
    {
        var result = DayOfWeekSet.ParseExact("_M_W_F_", "S");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsFalse(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when the format string is unrecognised.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsUnrecognised_ShouldThrowExactly() => Assert.ThrowsExactly<FormatException>(() => { _ = DayOfWeekSet.ParseExact("_M_W_F_", "Z"); });

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> throws
    /// <see cref="ArgumentNullException" /> when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenInputIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DayOfWeekSet.ParseExact(null!, "S");
        });

        Assert.AreEqual("s", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> throws
    /// <see cref="FormatException" /> when the format string is invalid or unsupported.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DayOfWeekSetTests.GetInvalidFormatSpecifierTestData), typeof(DayOfWeekSetTests))]
    public void ParseExact_WhenInvalidFormatSpecifier_ShouldThrowExactly(string input, string format) => Assert.ThrowsExactly<FormatException>(() => { _ = DayOfWeekSet.ParseExact(input, format); });

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses
    /// binary-formatted strings (e.g., <c>"1010101"</c>) across all 128 valid permutations when the
    /// <c>'B'</c> format specifier is provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DayOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DayOfWeekSetTests))]
    public void ParseExact_WhenValidBinaryInput_ShouldReturnExpected(byte expected, string _, string input) => Assert.AreEqual(SundayFirst(expected), DayOfWeekSet.ParseExact(input, "B"));

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses Monday-first
    /// symbol strings (e.g., <c>"MTWTFSS"</c>) across all 128 valid permutations when the <c>'M'</c>
    /// format specifier is provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DayOfWeekSetTests.GetAllBitmaskPermutationWithMondaySymbolsTestData), typeof(DayOfWeekSetTests))]
    public void ParseExact_WhenValidMondaySymbolInput_ShouldReturnExpected(byte expected, string input, string _) => Assert.AreEqual(SundayFirst(expected), DayOfWeekSet.ParseExact(input, "M"));
    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> correctly parses Sunday-first
    /// symbol strings (e.g., <c>"SMTWTFS"</c>) across all 128 valid permutations when the <c>'S'</c>
    /// format specifier is provided.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DayOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DayOfWeekSetTests))]
    public void ParseExact_WhenValidSundaySymbolInput_ShouldReturnExpected(byte expected, string input, string _) => Assert.AreEqual(SundayFirst(expected), DayOfWeekSet.ParseExact(input, "S"));

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ParseExact(string, string)" /> reads back every set written in every
    /// format, given that format in either case.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenGivenToStringOfAnySetInSameFormat_ShouldReturnSet()
    {
        string[] formats =
        [
            "S", "M", "E", "U", "D", "A", "SE", "SU", "SD", "SA", "ME", "MU", "MD", "MA", "B", "0", "1", "01",
            "s", "m", "e", "u", "d", "a", "se", "su", "sd", "sa", "me", "mu", "md", "ma", "b", "mE", "Su",
        ];

        for (int bits = 0; bits <= 0b111_1111; bits++)
        {
            DayOfWeekSet set = DayOfWeekSet.FromUInt64((ulong)bits);

            foreach (string format in formats)
            {
                string text = set.ToString(format);

                Assert.AreEqual(set, DayOfWeekSet.ParseExact(text, format), $"bits {bits}, format '{format}', text '{text}'");
            }
        }
    }

    /// <summary>
    /// Verifies that the <see cref="FormatException" /> for an unsupported format names the format.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsUnrecognised_ShouldNameFormat()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = DayOfWeekSet.ParseExact("_M_W_F_", "SZ");
        });

        StringAssert.Contains(ex.Message, "'SZ'");
    }

}
