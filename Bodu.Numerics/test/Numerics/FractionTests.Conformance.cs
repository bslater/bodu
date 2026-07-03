// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Conformance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that construction reduces to canonical form exactly as Python's <c>fractions.Fraction</c> and Apache
    /// Commons Numbers <c>BigFraction</c> do — the sign is carried on the numerator and the components are divided by
    /// their greatest common divisor. Rows are known-good values copied from those references.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow(16, -10, -8, 5, DisplayName = "Fraction(16, -10) -> -8/5")]
    [DataRow(6, 4, 3, 2, DisplayName = "Fraction(6, 4) -> 3/2")]
    [DataRow(123, 456, 41, 152, DisplayName = "Fraction(123, 456) -> 41/152")]
    [DataRow(-4, -8, 1, 2, DisplayName = "Fraction(-4, -8) -> 1/2")]
    [DataRow(0, 5, 0, 1, DisplayName = "Fraction(0, 5) -> 0/1")]
    public void Construction_ForReferenceLibraryVector_ShouldReduceToCanonicalForm(int numerator, int denominator, int expectedNumerator, int expectedDenominator)
    {
        var value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(expectedNumerator, value.Numerator);
        Assert.AreEqual(expectedDenominator, value.Denominator);
    }

    /// <summary>
    /// Verifies parsing of the <c>"numerator/denominator"</c> ratio form against reference values, matching
    /// <c>fractions.Fraction('3/7')</c>.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DataRow("3/7", 3, 7)]
    [DataRow("10/4", 5, 2)]
    [DataRow("-9/12", -3, 4)]
    public void Parse_ForReferenceLibraryVector_ShouldMatchExpectedValue(string text, int expectedNumerator, int expectedDenominator)
    {
        var value = Fraction<int>.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(new Fraction<int>(expectedNumerator, expectedDenominator), value);
    }
}
