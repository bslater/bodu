// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Parse(string)" /> accepts the supported textual forms.
    /// </summary>
    [TestMethod]
    [DataRow("3/4", 3, 4)]
    [DataRow("3", 3, 1)]
    [DataRow("-3/4", -3, 4)]
    [DataRow("  3 / 4  ", 3, 4)]
    [DataRow("1 3/4", 7, 4)]
    [DataRow("-1 3/4", -7, 4)]
    [DataRow("¾", 3, 4)]
    [DataRow("1½", 3, 2)]
    [DataRow("-1½", -3, 2)]
    [DataRow("75%", 3, 4)]
    public void Parse_WhenGivenSupportedForm_ShouldReturnExpectedValue(string text, int expectedNumerator, int expectedDenominator)
    {
        Fraction<int> value = Fraction<int>.Parse(text);

        Assert.AreEqual(new Fraction<int>(expectedNumerator, expectedDenominator), value);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Parse(string)" /> rejects malformed input.
    /// </summary>
    [TestMethod]
    [DataRow("abc")]
    [DataRow("")]
    [DataRow("1/0")]
    [DataRow("/4")]
    [DataRow("3/")]
    public void Parse_WhenGivenInvalidInput_ShouldThrowFormatException(string text)
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Fraction<int>.Parse(text);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.Parse(string)" /> rejects a <see langword="null" /> string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Fraction<int>.Parse(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryParse(string?, out Fraction{T})" /> succeeds for valid input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenGivenValidInput_ShouldReturnTrueAndValue()
    {
        bool parsed = Fraction<int>.TryParse("5/8", out Fraction<int> value);

        Assert.IsTrue(parsed);
        Assert.AreEqual(new Fraction<int>(5, 8), value);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryParse(string?, out Fraction{T})" /> fails gracefully for
    /// invalid or <see langword="null" /> input.
    /// </summary>
    [TestMethod]
    [DataRow("not a fraction")]
    [DataRow(null)]
    public void TryParse_WhenGivenInvalidInput_ShouldReturnFalse(string? text)
    {
        bool parsed = Fraction<int>.TryParse(text, out Fraction<int> value);

        Assert.IsFalse(parsed);
        Assert.AreEqual(Fraction<int>.Zero, value);
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryParse(string?, out Fraction{T})" /> reports overflow as failure.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenValueExceedsFixedWidthRange_ShouldReturnFalse()
    {
        bool parsed = Fraction<int>.TryParse("9999999999/1", out _);

        Assert.IsFalse(parsed);
    }

    /// <summary>
    /// Verifies that parsing accepts a wide table of valid textual forms.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow("0", 0, 1)]
    [DataRow("+3/4", 3, 4)]
    [DataRow("+3", 3, 1)]
    [DataRow("10/20", 1, 2)]
    [DataRow("2 1/2", 5, 2)]
    [DataRow("-0 3/4", -3, 4)]
    [DataRow("2⅓", 7, 3)]
    [DataRow("-2⅓", -7, 3)]
    [DataRow("150%", 3, 2)]
    [DataRow("0%", 0, 1)]
    [DataRow("  -3 / 4  ", -3, 4)]
    [DataRow("100/300", 1, 3)]
    public void Parse_WhenGivenKnownForm_ShouldReturnExpectedValue(string text, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), Fraction<int>.Parse(text));
    }

    /// <summary>
    /// Verifies that every supported Unicode vulgar-fraction glyph parses to its rational value.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow("½", 1, 2)]
    [DataRow("⅓", 1, 3)]
    [DataRow("⅔", 2, 3)]
    [DataRow("¼", 1, 4)]
    [DataRow("¾", 3, 4)]
    [DataRow("⅕", 1, 5)]
    [DataRow("⅖", 2, 5)]
    [DataRow("⅗", 3, 5)]
    [DataRow("⅘", 4, 5)]
    [DataRow("⅙", 1, 6)]
    [DataRow("⅚", 5, 6)]
    [DataRow("⅐", 1, 7)]
    [DataRow("⅛", 1, 8)]
    [DataRow("⅜", 3, 8)]
    [DataRow("⅝", 5, 8)]
    [DataRow("⅞", 7, 8)]
    [DataRow("⅑", 1, 9)]
    [DataRow("⅒", 1, 10)]
    public void Parse_WhenGivenVulgarGlyph_ShouldReturnExpectedValue(string text, int en, int ed)
    {
        Assert.AreEqual(new Fraction<int>(en, ed), Fraction<int>.Parse(text));
    }

    /// <summary>
    /// Verifies that parsing rejects a wide table of malformed input.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow("abc")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("1/0")]
    [DataRow("/4")]
    [DataRow("3/")]
    [DataRow("3/4/5")]
    [DataRow("--3")]
    [DataRow("3.5")]
    [DataRow("%")]
    [DataRow("1 2 3/4")]
    [DataRow("0/0")]
    public void Parse_WhenGivenMalformedInput_ShouldThrowFormatExceptionForEachCase(string text)
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Fraction<int>.Parse(text);
        });
    }

    /// <summary>
    /// Verifies that formatting and parsing round-trip exactly across a table of values and format specifiers.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(3, 4)]
    [DataRow(-7, 4)]
    [DataRow(5, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 3)]
    [DataRow(-11, 8)]
    public void Parse_WhenRoundTrippingEveryFormat_ShouldReproduceValue(int numerator, int denominator)
    {
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        foreach (string format in new[] { "G", "M", "U", "P" })
        {
            string text = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(value, Fraction<int>.Parse(text, System.Globalization.CultureInfo.InvariantCulture), format);
        }
    }
}
