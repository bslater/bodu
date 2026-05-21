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
}
