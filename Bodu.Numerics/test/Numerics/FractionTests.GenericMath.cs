// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.GenericMath.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that <see cref="Fraction{T}" /> can substitute for a type parameter constrained to
    /// <see cref="INumber{TSelf}" />.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenUsedAsINumber_ShouldComposeWithConstrainedCode()
    {
        Fraction<int> total = SumValues(
            new Fraction<int>(1, 2),
            new Fraction<int>(1, 3),
            new Fraction<int>(1, 6));

        Assert.AreEqual(Fraction<int>.One, total);
    }

    /// <summary>
    /// Verifies that the generic-math absolute value and sign operations behave correctly.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenComputingAbsAndSign_ShouldReturnExpectedValues()
    {
        Assert.AreEqual(new Fraction<int>(3, 4), AbsoluteValue(new Fraction<int>(-3, 4)));
        Assert.AreEqual(-1, SignOf(new Fraction<int>(-3, 4)));
        Assert.AreEqual(1, SignOf(new Fraction<int>(3, 4)));
    }

    /// <summary>
    /// Verifies that the generic-math clamp operation constrains a value to an inclusive range.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenClampingValue_ShouldConstrainToRange()
    {
        Fraction<int> low = Fraction<int>.Zero;
        Fraction<int> high = Fraction<int>.One;

        Assert.AreEqual(high, Clamp(new Fraction<int>(2, 1), low, high));
        Assert.AreEqual(low, Clamp(new Fraction<int>(-1, 1), low, high));
        Assert.AreEqual(new Fraction<int>(1, 2), Clamp(new Fraction<int>(1, 2), low, high));
    }

    /// <summary>
    /// Verifies that the generic-math creation routine converts an integer into a <see cref="Fraction{T}" />.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenCreatingFromInteger_ShouldProduceWholeNumber()
    {
        Assert.AreEqual(new Fraction<int>(5, 1), CreateFromInt32<Fraction<int>>(5));
    }

    /// <summary>
    /// Sums a sequence of numbers using only the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="values">The values to sum.</param>
    /// <returns>The total of <paramref name="values" />.</returns>
    private static TNumber SumValues<TNumber>(params TNumber[] values)
        where TNumber : INumber<TNumber>
    {
        TNumber total = TNumber.Zero;
        foreach (TNumber value in values)
        {
            total += value;
        }

        return total;
    }

    /// <summary>
    /// Returns the absolute value of a number using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value whose magnitude is required.</param>
    /// <returns>The magnitude of <paramref name="value" />.</returns>
    private static TNumber AbsoluteValue<TNumber>(TNumber value)
        where TNumber : INumber<TNumber> =>
        TNumber.Abs(value);

    /// <summary>
    /// Returns the sign of a number using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to inspect.</param>
    /// <returns>The sign of <paramref name="value" />.</returns>
    private static int SignOf<TNumber>(TNumber value)
        where TNumber : INumber<TNumber> =>
        TNumber.Sign(value);

    /// <summary>
    /// Clamps a number to an inclusive range using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>The clamped value.</returns>
    private static TNumber Clamp<TNumber>(TNumber value, TNumber min, TNumber max)
        where TNumber : INumber<TNumber> =>
        TNumber.Clamp(value, min, max);

    /// <summary>
    /// Creates a number from an <see cref="int" /> using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The integer to convert.</param>
    /// <returns>The created value.</returns>
    private static TNumber CreateFromInt32<TNumber>(int value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.CreateChecked(value);

    /// <summary>
    /// Verifies that the generic-math classification predicates report known answers.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    [DataRow(4, 1, true, true, false, false)]
    [DataRow(3, 1, true, false, true, false)]
    [DataRow(3, 4, false, false, false, false)]
    [DataRow(0, 1, true, true, false, false)]
    [DataRow(-2, 1, true, true, false, true)]
    [DataRow(-3, 1, true, false, true, true)]
    [DataRow(-3, 4, false, false, false, true)]
    public void GenericMath_WhenClassifyingValue_ShouldReportKnownAnswers(
        int numerator,
        int denominator,
        bool isInteger,
        bool isEvenInteger,
        bool isOddInteger,
        bool isNegative)
    {
        Fraction<int> value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(isInteger, IsIntegerOf(value), nameof(isInteger));
        Assert.AreEqual(isEvenInteger, IsEvenIntegerOf(value), nameof(isEvenInteger));
        Assert.AreEqual(isOddInteger, IsOddIntegerOf(value), nameof(isOddInteger));
        Assert.AreEqual(isNegative, IsNegativeOf(value), nameof(isNegative));
    }

    /// <summary>
    /// Verifies that the generic-math minimum, maximum, and magnitude selectors return expected values.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenSelectingExtremes_ShouldReturnExpectedValues()
    {
        Assert.AreEqual(new Fraction<int>(1, 2), MaxOf(new Fraction<int>(1, 3), new Fraction<int>(1, 2)));
        Assert.AreEqual(new Fraction<int>(1, 3), MinOf(new Fraction<int>(1, 3), new Fraction<int>(1, 2)));
        Assert.AreEqual(new Fraction<int>(-1, 3), MaxOf(new Fraction<int>(-1, 2), new Fraction<int>(-1, 3)));
        Assert.AreEqual(new Fraction<int>(-3, 4), MaxMagnitudeOf(new Fraction<int>(-3, 4), new Fraction<int>(1, 2)));
        Assert.AreEqual(new Fraction<int>(1, 2), MinMagnitudeOf(new Fraction<int>(-3, 4), new Fraction<int>(1, 2)));
    }

    /// <summary>
    /// Verifies that <see cref="Fraction{T}" /> reports a radix of two through the generic-math contract.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenQueryingRadix_ShouldReturnTwo()
    {
        Assert.AreEqual(2, RadixOf<Fraction<int>>());
    }

    /// <summary>
    /// Determines whether a number is an integer using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is an integer; otherwise, <see langword="false" />.</returns>
    private static bool IsIntegerOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsInteger(value);

    /// <summary>
    /// Determines whether a number is an even integer using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is an even integer; otherwise, <see langword="false" />.</returns>
    private static bool IsEvenIntegerOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsEvenInteger(value);

    /// <summary>
    /// Determines whether a number is an odd integer using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is an odd integer; otherwise, <see langword="false" />.</returns>
    private static bool IsOddIntegerOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsOddInteger(value);

    /// <summary>
    /// Determines whether a number is negative using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is negative; otherwise, <see langword="false" />.</returns>
    private static bool IsNegativeOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsNegative(value);

    /// <summary>
    /// Returns the larger of two numbers using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The larger of the two values.</returns>
    private static TNumber MaxOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumber<TNumber> =>
        TNumber.Max(x, y);

    /// <summary>
    /// Returns the smaller of two numbers using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The smaller of the two values.</returns>
    private static TNumber MinOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumber<TNumber> =>
        TNumber.Min(x, y);

    /// <summary>
    /// Returns the number with the greater magnitude using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The value with the greater magnitude.</returns>
    private static TNumber MaxMagnitudeOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumberBase<TNumber> =>
        TNumber.MaxMagnitude(x, y);

    /// <summary>
    /// Returns the number with the smaller magnitude using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The value with the smaller magnitude.</returns>
    private static TNumber MinMagnitudeOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumberBase<TNumber> =>
        TNumber.MinMagnitude(x, y);

    /// <summary>
    /// Returns the radix of a number type using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <returns>The radix reported by <typeparamref name="TNumber" />.</returns>
    private static int RadixOf<TNumber>()
        where TNumber : INumberBase<TNumber> =>
        TNumber.Radix;
}
