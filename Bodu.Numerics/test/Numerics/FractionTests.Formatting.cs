// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that the default format renders a ratio, or a bare integer when the value is whole.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4, "3/4")]
    [DataRow(-3, 4, "-3/4")]
    [DataRow(6, 2, "3")]
    [DataRow(0, 5, "0")]
    public void ToString_WhenUsingDefaultFormat_ShouldRenderRatioOrInteger(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString());
    }

    /// <summary>
    /// Verifies that the mixed-number format separates the whole and fractional parts.
    /// </summary>
    [TestMethod]
    [DataRow(7, 4, "1 3/4")]
    [DataRow(-7, 4, "-1 3/4")]
    [DataRow(3, 4, "3/4")]
    public void ToString_WhenUsingMixedFormat_ShouldSeparateWholeAndFraction(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString("M"));
    }

    /// <summary>
    /// Verifies that the Unicode format renders a vulgar-fraction glyph when one exists.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4, "¾")]
    [DataRow(1, 2, "½")]
    [DataRow(7, 4, "1¾")]
    [DataRow(-3, 4, "-¾")]
    public void ToString_WhenUsingUnicodeFormat_ShouldRenderVulgarGlyph(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString("U"));
    }

    /// <summary>
    /// Verifies that the Unicode format falls back to the mixed-number form when no glyph exists.
    /// </summary>
    [TestMethod]
    public void ToString_WhenUsingUnicodeFormatWithoutGlyph_ShouldFallBackToMixed()
    {
        Assert.AreEqual("5/9", new Fraction<int>(5, 9).ToString("U"));
    }

    /// <summary>
    /// Verifies that the percent format scales the value by one hundred and appends a percent sign.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4, "75%")]
    [DataRow(1, 3, "100/3%")]
    public void ToString_WhenUsingPercentFormat_ShouldScaleByOneHundred(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString("P"));
    }

    /// <summary>
    /// Verifies that an unsupported format specifier throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsUnsupported_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = new Fraction<int>(3, 4).ToString("Z");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
    /// writes the formatted value into a character span.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationIsLargeEnough_ShouldWriteFormattedValue()
    {
        Span<char> buffer = stackalloc char[16];

        bool formatted = new Fraction<int>(3, 4).TryFormat(buffer, out int written, default, null);

        Assert.IsTrue(formatted);
        Assert.AreEqual("3/4", new string(buffer[..written]));
    }

    /// <summary>
    /// Verifies that every format specifier round-trips through <see cref="Fraction{T}.Parse(string)" />.
    /// </summary>
    [TestMethod]
    [DataRow("G")]
    [DataRow("M")]
    [DataRow("U")]
    [DataRow("P")]
    public void ToString_WhenRoundTrippedThroughParse_ShouldReproduceValue(string format)
    {
        Fraction<int> value = new Fraction<int>(7, 4);

        string text = value.ToString(format, CultureInfo.InvariantCulture);

        Assert.AreEqual(value, Fraction<int>.Parse(text, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that the default format renders known answers for a wide table of values.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(0, 1, "0")]
    [DataRow(1, 1, "1")]
    [DataRow(-1, 1, "-1")]
    [DataRow(3, 4, "3/4")]
    [DataRow(-3, 4, "-3/4")]
    [DataRow(100, 7, "100/7")]
    [DataRow(-100, 7, "-100/7")]
    [DataRow(6, 3, "2")]
    public void ToString_WhenUsingDefaultFormat_ShouldRenderKnownAnswers(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString());
    }

    /// <summary>
    /// Verifies that the mixed-number format renders known answers for a wide table of values.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(7, 4, "1 3/4")]
    [DataRow(-7, 4, "-1 3/4")]
    [DataRow(11, 4, "2 3/4")]
    [DataRow(-11, 4, "-2 3/4")]
    [DataRow(3, 4, "3/4")]
    [DataRow(5, 3, "1 2/3")]
    [DataRow(-5, 3, "-1 2/3")]
    [DataRow(4, 1, "4")]
    [DataRow(0, 1, "0")]
    public void ToString_WhenUsingMixedFormat_ShouldRenderKnownAnswers(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString("M"));
    }

    /// <summary>
    /// Verifies that the Unicode format renders known answers for a wide table of values.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(1, 2, "½")]
    [DataRow(1, 3, "⅓")]
    [DataRow(2, 3, "⅔")]
    [DataRow(1, 4, "¼")]
    [DataRow(3, 4, "¾")]
    [DataRow(1, 8, "⅛")]
    [DataRow(5, 8, "⅝")]
    [DataRow(7, 4, "1¾")]
    [DataRow(-1, 2, "-½")]
    [DataRow(5, 2, "2½")]
    [DataRow(3, 1, "3")]
    [DataRow(5, 9, "5/9")]
    public void ToString_WhenUsingUnicodeFormat_ShouldRenderKnownAnswers(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString("U"));
    }

    /// <summary>
    /// Verifies that the percent format renders known answers for a wide table of values.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(1, 1, "100%")]
    [DataRow(1, 2, "50%")]
    [DataRow(1, 4, "25%")]
    [DataRow(3, 4, "75%")]
    [DataRow(2, 1, "200%")]
    [DataRow(-3, 4, "-75%")]
    [DataRow(0, 1, "0%")]
    [DataRow(1, 8, "25/2%")]
    [DataRow(1, 3, "100/3%")]
    [DataRow(1, 6, "50/3%")]
    public void ToString_WhenUsingPercentFormat_ShouldRenderKnownAnswers(int numerator, int denominator, string expected)
    {
        Assert.AreEqual(expected, new Fraction<int>(numerator, denominator).ToString("P"));
    }

    /// <summary>
    /// Verifies that the named formatting methods match their format-specifier equivalents.
    /// </summary>
    [TestMethod]
    public void NamedFormatMethods_WhenInvoked_ShouldMatchFormatSpecifiers()
    {
        Fraction<int> value = new Fraction<int>(7, 4);
        CultureInfo culture = CultureInfo.InvariantCulture;

        Assert.AreEqual(value.ToString("M", culture), value.ToMixedString(culture));
        Assert.AreEqual(value.ToMixedString(culture), value.ToMixedNumberString(culture));
        Assert.AreEqual(value.ToString("U", culture), value.ToUnicodeString(culture));
        Assert.AreEqual(value.ToString("P", culture), value.ToPercentString(culture));
    }

    /// <summary>
    /// Verifies that the named formatting methods produce the expected text for a known value.
    /// </summary>
    [TestMethod]
    public void NamedFormatMethods_WhenFormattingKnownValue_ShouldReturnExpectedText()
    {
        Fraction<int> value = new Fraction<int>(7, 4);

        Assert.AreEqual("1 3/4", value.ToMixedString(CultureInfo.InvariantCulture));
        Assert.AreEqual("1¾", value.ToUnicodeString(CultureInfo.InvariantCulture));
        Assert.AreEqual("175%", value.ToPercentString(CultureInfo.InvariantCulture));
    }
}
