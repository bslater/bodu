// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class DayOfWeekSetTests
{

    /// <summary>
    /// Verifies that all recognised binary format specifiers (<c>'0'</c>, <c>'1'</c>, <c>'B'</c>,
    /// <c>"01"</c>) return identical strings for the same instance.
    /// </summary>
    [TestMethod]
    public void ToString_WhenAllBinarySpecifiersUsedOnSameInstance_ShouldProduceIdenticalOutput()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

        string s0 = days.ToString("0");
        string s1 = days.ToString("1");
        string sB = days.ToString("B");
        string s01 = days.ToString("01");

        Assert.AreEqual(s0, s1, "Formats '0' and '1' should produce identical output.");
        Assert.AreEqual(s0, sB, "Formats '0' and 'B' should produce identical output.");
        Assert.AreEqual(s0, s01, "Formats '0' and \"01\" should produce identical output.");
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the binary format <c>'b'</c>
    /// returns the expected binary string for every valid bitmask permutation.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetAllBitmaskPermutationTestData))]
    public void ToString_WhenBinaryFormat_ShouldReturnExpectedBinaryString(byte value, string _, string expected) => Assert.AreEqual(expected, SundayFirst(value).ToString("b"));

    /// <summary>
    /// Verifies that the default <see cref="DayOfWeekSet.ToString()" /> overload returns the expected
    /// symbol string for every valid bitmask permutation.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetAllBitmaskPermutationTestData))]
    public void ToString_WhenCalled_ShouldReturnExpectedSymbolString(byte value, string expected, string _) => Assert.AreEqual(expected, SundayFirst(value).ToString());

    /// <summary>
    /// Verifies that calling <see cref="IFormattable.ToString(string, IFormatProvider)" /> directly
    /// produces the same result as the concrete <see cref="DayOfWeekSet.ToString(string, IFormatProvider)" />
    /// overload.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCalledViaIFormattableInterface_ShouldMatchConcreteOverload()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        IFormattable formattable = days;

        Assert.AreEqual(days.ToString("M", null), formattable.ToString("M", null));
    }

    /// <summary>
    /// Verifies that the default <see cref="DayOfWeekSet.ToString()" /> overload returns a Sunday-first
    /// string with underscore for unselected days.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCalledWithNoArguments_ShouldReturnSundayFirstWithUnderscores()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("_M_W_F_", days.ToString());
    }
    /// <summary>
    /// Verifies that the default <see cref="DayOfWeekSet.ToString()" /> overload returns a Sunday-first
    /// string with underscore for unselected days.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCalledWithoutParameters_ShouldUseDefaultFormat()
    {
        var days = new DayOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday);
        Assert.AreEqual("SM_____", days.ToString());
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet" /> explicitly implements <see cref="IFormattable" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCastToIFormattable_ShouldSucceed()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday);
        var formattable = days as IFormattable;

        Assert.IsNotNull(formattable,
            "DayOfWeekSet must implement IFormattable for composite formatting to work.");
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string, IFormatProvider)" /> produces the correct
    /// output when both format and provider are specified.
    /// </summary>
    [TestMethod]
    [DataRow("S", "SM____S")]
    [DataRow("M", "M____SS")]
    [DataRow("B", "1100001")]
    public void ToString_WhenFormatAndProviderProvided_ShouldFormatCorrectly(string format, string expected)
    {
        var days = new DayOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
        Assert.AreEqual(expected, days.ToString(format, null));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the <c>'0'</c> binary format
    /// specifier returns the correct binary string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIs0_ShouldReturnBinaryString()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("0101010", days.ToString("0"));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the <c>"01"</c> binary format
    /// specifier returns the correct binary string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIs01_ShouldReturnBinaryString()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("0101010", days.ToString("01"));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the <c>'0'</c> format and the
    /// <see cref="DayOfWeekSet.Weekdays" /> instance returns the correct binary string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIs0AndAllWeekdaysSelected_ShouldReturnCorrectBinaryString() => Assert.AreEqual("0111110", DayOfWeekSet.Weekdays.ToString("0"));

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the <c>'1'</c> binary format
    /// specifier returns the correct binary string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIs1_ShouldReturnBinaryString()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("0101010", days.ToString("1"));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the <c>'B'</c> binary format
    /// specifier returns the correct binary string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsB_ShouldReturnBinaryString()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("0101010", days.ToString("B"));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the <c>'M'</c> format returns a
    /// correct Monday-first string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsMondayFirst_ShouldReturnCorrectMondayFirstString()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("M_W_F__", days.ToString("M"));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the two-character dash format
    /// (<c>"MD"</c>) returns a correct Monday-first string with dashes for unselected days.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsMondayFirstWithDash_ShouldReturnCorrectString()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("M-W-F--", days.ToString("MD"));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string, System.IFormatProvider)" /> treats a
    /// <see langword="null" /> format string as the default Sunday-first specifier.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsNull_ShouldDefaultToSundayFirstFormat()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday);
        Assert.AreEqual(days.ToString("S"), days.ToString((string?)null, null));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> with the <c>'S'</c> format returns a
    /// correct Sunday-first string.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsSundayFirst_ShouldReturnCorrectSundayFirstString()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        Assert.AreEqual("_M_W_F_", days.ToString("S"));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> throws <see cref="FormatException" />
    /// when the format string is unrecognised.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsUnrecognised_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = new DayOfWeekSet(DayOfWeek.Monday).ToString("Z");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string, System.IFormatProvider)" /> with a non-null format
    /// provider produces the same output as the format-only overload - the provider is currently ignored, but the
    /// call site must still execute.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatProviderProvided_ShouldIgnoreProvider()
    {
        var days = new DayOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
        Assert.AreEqual("M____SS", days.ToString("M", System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet" /> implements <see cref="IFormattable" /> and that composite
    /// format strings correctly invoke <see cref="DayOfWeekSet.ToString(string, IFormatProvider)" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormattedViaStringFormat_ShouldApplyFormatSpecifier()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

        string result = string.Format("{0:M}", days);

        Assert.AreEqual("M_W_F__", result,
            "string.Format should apply the format specifier via IFormattable.");
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> throws <see cref="FormatException" />, naming the
    /// format, when an unrecognised format specifier is provided.
    /// </summary>
    [TestMethod]
    public void ToString_WhenInvalidFormat_ShouldThrowExactly()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday);

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = days.ToString("X");
        });

        StringAssert.Contains(ex.Message, "'X'");
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> treats an empty format string as the default
    /// Sunday-first format, as the framework's formattable types do.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsEmpty_ShouldUseDefaultFormat()
    {
        var days = new DayOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday);
        Assert.AreEqual("SM_____", days.ToString(string.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> throws <see cref="FormatException" /> when the
    /// two-character format specifier has an invalid first character (i.e. not <c>'S'</c> or <c>'M'</c>).
    /// </summary>
    [TestMethod]
    public void ToString_WhenTwoCharFormatHasInvalidStartDay_ShouldThrowExactly()
    {
        var days = new DayOfWeekSet(DayOfWeek.Monday);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = days.ToString("XU");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.ToString(string)" /> produces the correct output for each
    /// recognised format specifier.
    /// </summary>
    [TestMethod]
    [DataRow("S", "SM____S")] // Sunday-first
    [DataRow("s", "SM____S")]
    [DataRow("G", "SM____S")] // General, the Sunday-first default
    [DataRow("g", "SM____S")]
    [DataRow("M", "M____SS")] // Monday-first
    [DataRow("m", "M____SS")]
    [DataRow("B", "1100001")] // Binary (Sunday + Monday + Saturday selected)
    [DataRow("b", "1100001")]
    public void ToString_WhenValidFormat_ShouldFormatCorrectly(string format, string expected)
    {
        var days = new DayOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
        Assert.AreEqual(expected, days.ToString(format));
    }

}
