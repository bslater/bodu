// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the bracketed form parses into the expected value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBracketedForm_ShouldReturnExpectedValue()
    {
        Complex<double> value = Complex<double>.Parse("<3; -4>", CultureInfo.InvariantCulture);

        Assert.AreEqual(new Complex<double>(3.0, -4.0), value);
    }

    /// <summary>
    /// Verifies that the single-argument <see cref="Complex{T}.Parse(string)" /> overload delegates to the
    /// provider-based parser and terminates with the expected value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenStringOnly_ShouldDelegateToProviderOverload()
    {
        Complex<double> value = Complex<double>.Parse("<3; -4>");

        Assert.AreEqual(new Complex<double>(3.0, -4.0), value);
    }

    /// <summary>
    /// Verifies that a bare real value parses onto the real axis.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBareReal_ShouldPlaceValueOnRealAxis()
    {
        Complex<double> value = Complex<double>.Parse("5", CultureInfo.InvariantCulture);

        Assert.AreEqual(new Complex<double>(5.0, 0.0), value);
    }

    /// <summary>
    /// Verifies that the default string representation round-trips through the parser.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenToStringOutput_ShouldRoundTrip()
    {
        var original = new Complex<double>(-2.5, 7.25);

        Complex<double> parsed = Complex<double>.Parse(original.ToString(null, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        Assert.AreEqual(original, parsed);
    }

    /// <summary>
    /// Verifies that the bracketed form round-trips through a comma-decimal culture.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCommaDecimalCulture_ShouldRoundTrip()
    {
        var german = CultureInfo.GetCultureInfo("de-DE");
        var original = new Complex<double>(1.5, -2.5);

        Complex<double> parsed = Complex<double>.Parse(original.ToString(null, german), german);

        Assert.AreEqual(original, parsed);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.TryParse(string?, out Complex{T})" /> returns <see langword="false" /> for
    /// malformed input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputMalformed_ShouldReturnFalse()
    {
        Assert.IsFalse(Complex<double>.TryParse("<3; >", out _));
        Assert.IsFalse(Complex<double>.TryParse("not a complex", out _));
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Parse(string, IFormatProvider?)" /> throws
    /// <see cref="ArgumentNullException" /> for a null input.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsNull_ShouldThrowArgumentNull()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Complex<double>.Parse((string)null!, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" /> throws
    /// <see cref="FormatException" /> for malformed input.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputMalformed_ShouldThrowFormatException()
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Complex<double>.Parse("<1; 2", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that a value with non-finite components round-trips through <see cref="Complex{T}.ToString()" /> and the
    /// parser, since each component formats to and parses from its named literal.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Parse_WhenComponentsAreNonFinite_ShouldRoundTrip()
    {
        foreach (var original in new[]
        {
            new Complex<double>(double.NaN, double.PositiveInfinity),
            new Complex<double>(double.PositiveInfinity, double.NegativeInfinity),
        })
        {
            Complex<double> parsed = Complex<double>.Parse(original.ToString(null, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

            Assert.AreEqual(
                BitConverter.DoubleToInt64Bits(original.Real),
                BitConverter.DoubleToInt64Bits(parsed.Real),
                $"real of {original}");
            Assert.AreEqual(
                BitConverter.DoubleToInt64Bits(original.Imaginary),
                BitConverter.DoubleToInt64Bits(parsed.Imaginary),
                $"imaginary of {original}");
        }
    }

    /// <summary>
    /// Verifies that a value requiring full round-trip precision reparses to the identical value, confirming the default
    /// format is shortest-round-trippable.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Parse_WhenValueNeedsFullPrecision_ShouldRoundTripExactly()
    {
        var original = new Complex<double>(Math.PI, 0.1 + 0.2);

        Complex<double> parsed = Complex<double>.Parse(original.ToString(null, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        Assert.AreEqual(original, parsed);
    }
}
