// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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
        var value = Fraction<int>.Parse(text);

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
    public void Parse_WhenGivenInvalidInput_ShouldThrowExactly(string text)
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
    public void Parse_WhenGivenNull_ShouldThrowExactly()
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
    public void Parse_WhenGivenKnownForm_ShouldReturnExpectedValue(string text, int en, int ed) => Assert.AreEqual(new Fraction<int>(en, ed), Fraction<int>.Parse(text));

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
    public void Parse_WhenGivenVulgarGlyph_ShouldReturnExpectedValue(string text, int en, int ed) => Assert.AreEqual(new Fraction<int>(en, ed), Fraction<int>.Parse(text));

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
    public void Parse_WhenGivenMalformedInput_ShouldThrowExactly(string text)
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
        var value = new Fraction<int>(numerator, denominator);

        foreach (string? format in new[] { "G", "M", "U", "P" })
        {
            string text = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(value, Fraction<int>.Parse(text, System.Globalization.CultureInfo.InvariantCulture), format);
        }
    }

    /// <summary>
    /// Verifies that a mixed number whose magnitude component carries its own sign is rejected, since the only
    /// permitted sign is the leading sign of the whole value.
    /// </summary>
    [TestMethod]
    [DataRow("1 -1/2")]
    [DataRow("1 1/-2")]
    [DataRow("-1 -1/2")]
    public void TryParse_WhenMagnitudeComponentIsSigned_ShouldReturnFalse(string text)
    {
        bool parsed = Fraction<int>.TryParse(text, out Fraction<int> result);

        Assert.IsFalse(parsed);
        Assert.AreEqual(Fraction<int>.Zero, result);
    }

    /// <summary>
    /// Verifies that parsing a string consisting only of a sign character fails because no magnitude remains after the
    /// sign is consumed.
    /// </summary>
    [TestMethod]
    [DataRow("-")]
    [DataRow("+")]
    public void TryParse_WhenInputIsSignOnly_ShouldReturnFalse(string text)
    {
        Assert.IsFalse(Fraction<int>.TryParse(text, out Fraction<int> result));
        Assert.AreEqual(Fraction<int>.Zero, result);
    }

    /// <summary>
    /// Verifies that parsing a Unicode vulgar fraction whose leading whole-number part is non-numeric fails.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenVulgarGlyphWholePartIsNonNumeric_ShouldReturnFalse()
    {
        Assert.IsFalse(Fraction<int>.TryParse("x½", out Fraction<int> result));
        Assert.AreEqual(Fraction<int>.Zero, result);
    }

    /// <summary>
    /// Verifies that a negative value formatted in every format under a culture whose negative sign is not the ASCII
    /// hyphen-minus parses back to the same value under that culture.
    /// </summary>
    /// <param name="cultureName">The name of the culture under test.</param>
    [TestMethod]
    [DataRow("sv-SE")]
    [DataRow("fa-IR")]
    [DataRow("ar-SA")]
    [DataRow("he-IL")]
    public void Parse_WhenNegativeTextUsesCultureNegativeSign_ShouldReproduceValue(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var value = new Fraction<int>(-7, 4);

        foreach (string format in new[] { "G", "M", "U", "P" })
        {
            string text = value.ToString(format, culture);
            Assert.AreEqual(value, Fraction<int>.Parse(text, culture), $"{format}: '{text}'");
        }
    }

    /// <summary>
    /// Verifies that a negative value formatted into a character span under a culture whose negative sign is not the
    /// ASCII hyphen-minus parses back from that span to the same value under that culture.
    /// </summary>
    /// <param name="cultureName">The name of the culture under test.</param>
    [TestMethod]
    [DataRow("sv-SE")]
    [DataRow("fa-IR")]
    [DataRow("ar-SA")]
    [DataRow("he-IL")]
    public void TryParse_WhenNegativeSpanUsesCultureNegativeSign_ShouldReturnTrueAndValue(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var value = new Fraction<int>(-3, 4);
        Span<char> buffer = stackalloc char[32];
        Assert.IsTrue(value.TryFormat(buffer, out int written, default, culture));

        bool parsed = Fraction<int>.TryParse(buffer[..written], culture, out Fraction<int> result);

        Assert.IsTrue(parsed, buffer[..written].ToString());
        Assert.AreEqual(value, result);
    }

    /// <summary>
    /// Verifies that a value carrying the positive sign of a culture whose positive sign is not the ASCII plus sign
    /// parses under that culture.
    /// </summary>
    /// <param name="cultureName">The name of the culture under test.</param>
    [TestMethod]
    [DataRow("fa-IR")]
    [DataRow("ar-SA")]
    [DataRow("he-IL")]
    public void TryParse_WhenTextUsesCulturePositiveSign_ShouldReturnTrueAndValue(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        string text = culture.NumberFormat.PositiveSign + "3/4";

        bool parsed = Fraction<int>.TryParse(text, culture, out Fraction<int> result);

        Assert.IsTrue(parsed, text);
        Assert.AreEqual(new Fraction<int>(3, 4), result);
    }

    /// <summary>
    /// Verifies that negative, positive, whole and zero values formatted in every format under every culture the
    /// runtime reports parse back to the same value under that culture.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRoundTrippingUnderEveryCulture_ShouldReproduceValue()
    {
        Fraction<int>[] values = [new(-7, 4), new(-3, 4), new(-5, 1), Fraction<int>.Zero, new(7, 4), new(3, 4)];
        string[] formats = ["G", "M", "U", "P"];
        var failures = new List<string>();

        foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            foreach (Fraction<int> value in values)
            {
                foreach (string format in formats)
                {
                    string text = value.ToString(format, culture);
                    if (!Fraction<int>.TryParse(text, culture, out Fraction<int> parsed) || parsed != value)
                        failures.Add($"{culture.Name}/{format}/{value}: '{text}'");
                }
            }
        }

        Assert.AreEqual(0, failures.Count, string.Join("; ", failures.Take(10)));
    }
}
