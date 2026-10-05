// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu;

public partial class DayOfWeekSetTests
{

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> throws <see cref="FormatException" />
    /// when the input contains an unrecognised character.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputContainsInvalidCharacter_ShouldThrowExactly() => Assert.ThrowsExactly<FormatException>(() => { _ = DayOfWeekSet.Parse("SMTWTFX"); });

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly auto-detects and parses a
    /// binary string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsBinary_ShouldSetCorrectDays()
    {
        var result = DayOfWeekSet.Parse("0111110");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly parses a Monday-first string,
    /// inferring the ordering from the leading selected-day character.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsMondayFirstAndFirstCharIsSelected_ShouldSetCorrectDays()
    {
        var result = DayOfWeekSet.Parse("MTWTF__");

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
    }
    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> throws <see cref="ArgumentNullException" />
    /// when the input is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsNull_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = DayOfWeekSet.Parse(null!);
        });

        Assert.AreEqual("s", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly identifies a Sunday-first
    /// string where all seven days are selected.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsSundayFirstAllSelected_ShouldSelectAllDays() => Assert.AreEqual(7, DayOfWeekSet.Parse("SMTWTFS").Count);

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly parses a Sunday-first string
    /// with specific days selected.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsSundayFirstPartialSelection_ShouldSetCorrectDays()
    {
        var result = DayOfWeekSet.Parse("_M_W_F_");

        Assert.IsFalse(result.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsFalse(result.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(result.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.IsFalse(result.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> throws <see cref="FormatException" />
    /// when the input is not exactly seven characters long.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputLengthIsNot7_ShouldThrowExactly() => Assert.ThrowsExactly<FormatException>(() => { _ = DayOfWeekSet.Parse("SMTWTF"); });

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> throws <see cref="FormatException" /> for
    /// every <see cref="InvalidDayOfWeekSetParseKat" /> row produced by
    /// <see cref="DayOfWeekSetTests.GetTryParseInvalidKats" />. Identical row coverage to the legacy
    /// <c>GetInvalidParseInputNoFormatTestData</c> source but with named display rows so failures surface
    /// the offending input rather than an opaque row index.
    /// </summary>
    /// <param name="kat">The KAT row supplying a malformed no-format input expected to throw.</param>
    [TestMethod]
    [DynamicData(
        nameof(DayOfWeekSetTests.GetTryParseInvalidKats),
        typeof(DayOfWeekSetTests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalidInput_ShouldThrowExactly(InvalidDayOfWeekSetParseKat kat) =>
        Assert.ThrowsExactly<FormatException>(() => { _ = DayOfWeekSet.Parse(kat.Input); });

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly assigns day indices when the
    /// first position of a Monday-first string is an unselected placeholder.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMondayFirstAndFirstPositionIsUnselected_ShouldAssignCorrectDays()
    {
        var result = DayOfWeekSet.Parse("_TWTFSS");

        Assert.IsFalse(result.Contains(DayOfWeek.Monday), "Monday should be unselected.");
        Assert.IsTrue(result.Contains(DayOfWeek.Tuesday), "Tuesday should be selected.");
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday), "Wednesday should be selected.");
        Assert.IsTrue(result.Contains(DayOfWeek.Thursday), "Thursday should be selected.");
        Assert.IsTrue(result.Contains(DayOfWeek.Friday), "Friday should be selected.");
        Assert.IsTrue(result.Contains(DayOfWeek.Saturday), "Saturday should be selected.");
        Assert.IsTrue(result.Contains(DayOfWeek.Sunday), "Sunday should be selected.");
    }

    /// <summary>
    /// Verifies that the result of <see cref="DayOfWeekSet.Parse(string)" /> round-trips correctly
    /// through <see cref="DayOfWeekSet.ToString()" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRoundTripped_ShouldProduceOriginalString()
    {
        const string input = "S_____S";
        Assert.AreEqual(input, DayOfWeekSet.Parse(input).ToString());
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly parses binary-formatted strings
    /// across all 128 valid permutations.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DayOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DayOfWeekSetTests))]
    public void Parse_WhenValidBinaryInput_ShouldReturnExpected(byte expected, string _, string input) => Assert.AreEqual(SundayFirst(expected), DayOfWeekSet.Parse(input));

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly parses Monday-first symbol
    /// strings across all 128 valid permutations.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DayOfWeekSetTests.GetAllBitmaskPermutationWithMondaySymbolsTestData), typeof(DayOfWeekSetTests))]
    public void Parse_WhenValidMondaySymbolInput_ShouldReturnExpected(byte expected, string input, string _)
    {
        if (input == "______S")
        {
            // The one genuinely ambiguous symbol form: auto-detect documents the Sunday-first (Saturday-only)
            // resolution, so the Monday-first expectation does not apply here. Pinned explicitly by
            // Parse_WhenInputIsAmbiguousTrailingS_ShouldResolveAsSundayFirst below.
            TestContext.WriteLine("Skipping '______S': auto-detect documents the Sunday-first resolution.");
            return;
        }

        Assert.AreEqual(SundayFirst(expected), DayOfWeekSet.Parse(input));
    }

    /// <summary>
    /// Verifies that the single ambiguous symbol form <c>"______S"</c> resolves under auto-detection to the documented
    /// Sunday-first reading (Saturday-only), and that <see cref="DayOfWeekSet.ParseExact(string, string)" /> with the
    /// <c>"M"</c> format recovers the Monday-first (Sunday-only) reading.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsAmbiguousTrailingS_ShouldResolveAsSundayFirst()
    {
        DayOfWeekSet autoDetected = DayOfWeekSet.Parse("______S");
        DayOfWeekSet mondayFirst = DayOfWeekSet.ParseExact("______S", "M");

        Assert.AreEqual(new DayOfWeekSet(DayOfWeek.Saturday), autoDetected);
        Assert.AreEqual(new DayOfWeekSet(DayOfWeek.Sunday), mondayFirst);
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> correctly parses Sunday-first symbol
    /// strings across all 128 valid permutations.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DayOfWeekSetTests.GetAllBitmaskPermutationTestData), typeof(DayOfWeekSetTests))]
    public void Parse_WhenValidSundaySymbolInput_ShouldReturnExpected(byte expected, string input, string _) => Assert.AreEqual(SundayFirst(expected), DayOfWeekSet.Parse(input));

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Parse(string)" /> reads back every set written in every Sunday-first and
    /// binary format, whose order and placeholder it detects.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenToStringOfAnySetInSundayFirstOrBinaryFormat_ShouldReturnSet()
    {
        string[] formats = ["S", "E", "U", "D", "A", "SE", "SU", "SD", "SA", "B", "0", "1", "01"];

        for (int bits = 0; bits <= 0b111_1111; bits++)
        {
            DayOfWeekSet set = DayOfWeekSet.FromUInt64((ulong)bits);

            foreach (string format in formats)
            {
                string text = set.ToString(format);

                Assert.AreEqual(set, DayOfWeekSet.Parse(text), $"bits {bits}, format '{format}', text '{text}'");
            }
        }
    }

    /// <summary>
    /// Verifies that the <see cref="FormatException" /> for a character that fits neither the day nor the placeholder
    /// names the character and its one-based position.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputContainsInvalidCharacter_ShouldNameCharacterAndPosition()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = DayOfWeekSet.Parse("_MTXTF_");
        });

        StringAssert.Contains(ex.Message, "'X'");
        StringAssert.Contains(ex.Message, "4");
    }

}
