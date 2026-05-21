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
}
