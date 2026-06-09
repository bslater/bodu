// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.GenericMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
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
    public void GenericMath_WhenCreatingFromInteger_ShouldProduceWholeNumber() => Assert.AreEqual(new Fraction<int>(5, 1), CreateFromInt32<Fraction<int>>(5));

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
        var value = new Fraction<int>(numerator, denominator);

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
    public void GenericMath_WhenQueryingRadix_ShouldReturnTwo() => Assert.AreEqual(2, RadixOf<Fraction<int>>());

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

    /// <summary>
    /// Verifies that converting a non-integer fraction with the checked contract succeeds without throwing.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConvertingNonIntegerToChecked_ShouldSucceedWithoutThrowing() => Assert.AreEqual(0, ConvertChecked<Fraction<int>, int>(new Fraction<int>(1, 2)));

    /// <summary>
    /// Verifies that the checked conversion contract throws for a value outside the destination range.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenCheckedConversionOverflowsDestination_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = ConvertChecked<Fraction<int>, byte>(new Fraction<int>(1000, 1));
        });
    }

    /// <summary>
    /// Verifies that converting from a decimal preserves the exact rational value rather than a double.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConvertingFromDecimal_ShouldPreserveExactValue() => Assert.AreEqual(new Fraction<int>(1, 10), ConvertChecked<decimal, Fraction<int>>(0.1m));

    /// <summary>
    /// Verifies that a saturating conversion clamps an out-of-range value instead of throwing.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenSaturatingConversionOverflows_ShouldClampToBound() => Assert.AreEqual(Fraction<int>.MaxValue, ConvertSaturating<double, Fraction<int>>(1e30));

    /// <summary>
    /// Converts a value to a destination type using the checked creation contract.
    /// </summary>
    /// <typeparam name="TSource">The source number type.</typeparam>
    /// <typeparam name="TTarget">The destination number type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value converted to <typeparamref name="TTarget" />.</returns>
    private static TTarget ConvertChecked<TSource, TTarget>(TSource value)
        where TSource : INumberBase<TSource>
        where TTarget : INumberBase<TTarget> =>
        TTarget.CreateChecked(value);

    /// <summary>
    /// Converts a value to a destination type using the saturating creation contract.
    /// </summary>
    /// <typeparam name="TSource">The source number type.</typeparam>
    /// <typeparam name="TTarget">The destination number type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value converted to <typeparamref name="TTarget" />.</returns>
    private static TTarget ConvertSaturating<TSource, TTarget>(TSource value)
        where TSource : INumberBase<TSource>
        where TTarget : INumberBase<TTarget> =>
        TTarget.CreateSaturating(value);

    /// <summary>
    /// Converts a value to a destination type using the truncating creation contract.
    /// </summary>
    /// <typeparam name="TSource">The source number type.</typeparam>
    /// <typeparam name="TTarget">The destination number type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value converted to <typeparamref name="TTarget" />.</returns>
    private static TTarget ConvertTruncating<TSource, TTarget>(TSource value)
        where TSource : INumberBase<TSource>
        where TTarget : INumberBase<TTarget> =>
        TTarget.CreateTruncating(value);

    /// <summary>
    /// Verifies that the additive, multiplicative, and signed identities exposed through the generic-math contracts
    /// resolve to the corresponding rational constants.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenQueryingIdentities_ShouldMatchRationalConstants()
    {
        Assert.AreEqual(Fraction<int>.Zero, AdditiveIdentityOf<Fraction<int>>());
        Assert.AreEqual(Fraction<int>.One, MultiplicativeIdentityOf<Fraction<int>>());
        Assert.AreEqual(Fraction<int>.MinusOne, NegativeOneOf<Fraction<int>>());
    }

    /// <summary>
    /// Verifies that the classification predicates that are constant for every rational value report the values
    /// expected of an exact, finite, real number.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenQueryingConstantClassifiers_ShouldReportRationalSemantics()
    {
        var value = new Fraction<int>(3, 4);

        Assert.IsTrue(IsCanonicalOf(value), nameof(INumberBase<Fraction<int>>.IsCanonical));
        Assert.IsFalse(IsComplexNumberOf(value), nameof(INumberBase<Fraction<int>>.IsComplexNumber));
        Assert.IsTrue(IsFiniteOf(value), nameof(INumberBase<Fraction<int>>.IsFinite));
        Assert.IsFalse(IsImaginaryNumberOf(value), nameof(INumberBase<Fraction<int>>.IsImaginaryNumber));
        Assert.IsFalse(IsInfinityOf(value), nameof(INumberBase<Fraction<int>>.IsInfinity));
        Assert.IsFalse(IsNaNOf(value), nameof(INumberBase<Fraction<int>>.IsNaN));
        Assert.IsFalse(IsNegativeInfinityOf(value), nameof(INumberBase<Fraction<int>>.IsNegativeInfinity));
        Assert.IsFalse(IsPositiveInfinityOf(value), nameof(INumberBase<Fraction<int>>.IsPositiveInfinity));
        Assert.IsTrue(IsRealNumberOf(value), nameof(INumberBase<Fraction<int>>.IsRealNumber));
        Assert.IsFalse(IsSubnormalOf(value), nameof(INumberBase<Fraction<int>>.IsSubnormal));
    }

    /// <summary>
    /// Verifies that the value-dependent classification predicates report known answers across zero, positive, and
    /// negative rationals.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, false, false, true)]
    [DataRow(3, 4, true, true, false)]
    [DataRow(-3, 4, true, false, false)]
    public void GenericMath_WhenQueryingValueDependentClassifiers_ShouldReportKnownAnswers(
        int numerator,
        int denominator,
        bool isNormal,
        bool isPositive,
        bool isZero)
    {
        var value = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(isNormal, IsNormalOf(value), nameof(isNormal));
        Assert.AreEqual(isPositive, IsPositiveOf(value), nameof(isPositive));
        Assert.AreEqual(isZero, IsZeroOf(value), nameof(isZero));
    }

    /// <summary>
    /// Verifies that the IEEE-style magnitude-number selectors agree with the plain magnitude selectors, preferring
    /// the positive value on a magnitude tie for the maximum and the negative value for the minimum.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenSelectingMagnitudeNumber_ShouldMatchMagnitudeSelectors()
    {
        Assert.AreEqual(new Fraction<int>(-3, 4), MaxMagnitudeNumberOf(new Fraction<int>(-3, 4), new Fraction<int>(1, 2)));
        Assert.AreEqual(new Fraction<int>(1, 2), MinMagnitudeNumberOf(new Fraction<int>(-3, 4), new Fraction<int>(1, 2)));
        Assert.AreEqual(new Fraction<int>(3, 4), MaxMagnitudeNumberOf(new Fraction<int>(3, 4), new Fraction<int>(-3, 4)));
        Assert.AreEqual(new Fraction<int>(-3, 4), MinMagnitudeNumberOf(new Fraction<int>(3, 4), new Fraction<int>(-3, 4)));
    }

    /// <summary>
    /// Verifies that the culture-and-style parse and try-parse overloads required by <see cref="INumberBase{TSelf}" />
    /// round-trip the canonical ratio form.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenParsingThroughNumberBase_ShouldRoundTripRatioForm()
    {
        var expected = new Fraction<int>(3, 4);

        Assert.AreEqual(expected, ParseString<Fraction<int>>("3/4"));
        Assert.AreEqual(expected, ParseSpan<Fraction<int>>("3/4"));

        Assert.IsTrue(TryParseString<Fraction<int>>("3/4", out Fraction<int> fromString));
        Assert.AreEqual(expected, fromString);

        Assert.IsTrue(TryParseSpan<Fraction<int>>("3/4", out Fraction<int> fromSpan));
        Assert.AreEqual(expected, fromSpan);
    }

    /// <summary>
    /// Verifies that the saturating conversion contract narrows integral and fractional rationals toward zero and
    /// reads the exact integral magnitude through the backing integer rather than a lossy double.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, 5)]
    [DataRow(1, 2, 0)]
    [DataRow(-7, 2, -3)]
    public void GenericMath_WhenConvertingToSaturating_ShouldReturnExpected(int numerator, int denominator, int expected) =>
        Assert.AreEqual(expected, ConvertSaturating<Fraction<int>, int>(new Fraction<int>(numerator, denominator)));

    /// <summary>
    /// Verifies that a saturating conversion of an integral rational whose magnitude exceeds the destination range
    /// clamps to the nearer bound instead of throwing.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConvertingToSaturatingOverflows_ShouldClampToBound()
    {
        Assert.AreEqual(byte.MaxValue, ConvertSaturating<Fraction<int>, byte>(new Fraction<int>(1000, 1)));
        Assert.AreEqual(byte.MinValue, ConvertSaturating<Fraction<int>, byte>(new Fraction<int>(-1000, 1)));
    }

    /// <summary>
    /// Verifies that the truncating conversion contract narrows integral and fractional rationals toward zero.
    /// </summary>
    [TestMethod]
    [DataRow(5, 1, 5)]
    [DataRow(1, 2, 0)]
    [DataRow(-7, 2, -3)]
    public void GenericMath_WhenConvertingToTruncating_ShouldReturnExpected(int numerator, int denominator, int expected) =>
        Assert.AreEqual(expected, ConvertTruncating<Fraction<int>, int>(new Fraction<int>(numerator, denominator)));

    /// <summary>
    /// Verifies that converting from a finite, non-integer floating-point value approximates the nearest rational
    /// through every creation contract.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConvertingFromFiniteNonIntegerDouble_ShouldApproximate()
    {
        var expected = new Fraction<int>(1, 2);

        Assert.AreEqual(expected, ConvertChecked<double, Fraction<int>>(0.5));
        Assert.AreEqual(expected, ConvertSaturating<double, Fraction<int>>(0.5));
        Assert.AreEqual(expected, ConvertTruncating<double, Fraction<int>>(0.5));
    }

    /// <summary>
    /// Verifies that converting from a non-finite floating-point value is rejected, since no rational can represent
    /// infinity.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConvertingFromNonFiniteDouble_ShouldThrowNotSupportedException()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = ConvertChecked<double, Fraction<int>>(double.PositiveInfinity);
        });
    }

    /// <summary>
    /// Verifies that converting from a finite source whose double approximation is non-finite is rejected, exercising
    /// the guard that re-checks the approximation after the floating-point conversion.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenFiniteSourceApproximatesToInfinity_ShouldThrowNotSupportedException()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = ConvertChecked<UnsupportedNumber, Fraction<int>>(UnsupportedNumber.WithInfiniteDoubleApproximation());
        });
    }

    /// <summary>
    /// Verifies that a <see cref="NotSupportedException" /> raised while converting <em>from</em> an unsupported
    /// numeric type is surfaced by every from-creation contract rather than being swallowed into a wrong answer.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConvertingFromUnsupportedType_ShouldThrowNotSupportedException()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertChecked<UnsupportedNumber, Fraction<int>>(default); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertSaturating<UnsupportedNumber, Fraction<int>>(default); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertTruncating<UnsupportedNumber, Fraction<int>>(default); });
    }

    /// <summary>
    /// Verifies that a <see cref="NotSupportedException" /> raised while converting <em>to</em> an unsupported numeric
    /// type is surfaced by every to-creation contract rather than being swallowed into a wrong answer.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenConvertingToUnsupportedType_ShouldThrowNotSupportedException()
    {
        var integer = new Fraction<int>(5, 1);

        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertChecked<Fraction<int>, UnsupportedNumber>(integer); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertSaturating<Fraction<int>, UnsupportedNumber>(integer); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertTruncating<Fraction<int>, UnsupportedNumber>(integer); });
    }

    /// <summary>
    /// Returns the additive identity of a number type using the <see cref="IAdditiveIdentity{TSelf, TResult}" />
    /// contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <returns>The additive identity reported by <typeparamref name="TNumber" />.</returns>
    private static TNumber AdditiveIdentityOf<TNumber>()
        where TNumber : IAdditiveIdentity<TNumber, TNumber> =>
        TNumber.AdditiveIdentity;

    /// <summary>
    /// Returns the multiplicative identity of a number type using the
    /// <see cref="IMultiplicativeIdentity{TSelf, TResult}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <returns>The multiplicative identity reported by <typeparamref name="TNumber" />.</returns>
    private static TNumber MultiplicativeIdentityOf<TNumber>()
        where TNumber : IMultiplicativeIdentity<TNumber, TNumber> =>
        TNumber.MultiplicativeIdentity;

    /// <summary>
    /// Returns the negative-one constant of a number type using the <see cref="ISignedNumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <returns>The negative-one value reported by <typeparamref name="TNumber" />.</returns>
    private static TNumber NegativeOneOf<TNumber>()
        where TNumber : ISignedNumber<TNumber> =>
        TNumber.NegativeOne;

    /// <summary>
    /// Determines whether a value is in canonical form using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is canonical; otherwise, <see langword="false" />.</returns>
    private static bool IsCanonicalOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsCanonical(value);

    /// <summary>
    /// Determines whether a value is a complex number using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is complex; otherwise, <see langword="false" />.</returns>
    private static bool IsComplexNumberOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsComplexNumber(value);

    /// <summary>
    /// Determines whether a value is finite using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is finite; otherwise, <see langword="false" />.</returns>
    private static bool IsFiniteOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsFinite(value);

    /// <summary>
    /// Determines whether a value is an imaginary number using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is imaginary; otherwise, <see langword="false" />.</returns>
    private static bool IsImaginaryNumberOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsImaginaryNumber(value);

    /// <summary>
    /// Determines whether a value is infinite using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is infinite; otherwise, <see langword="false" />.</returns>
    private static bool IsInfinityOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsInfinity(value);

    /// <summary>
    /// Determines whether a value is NaN using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is NaN; otherwise, <see langword="false" />.</returns>
    private static bool IsNaNOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsNaN(value);

    /// <summary>
    /// Determines whether a value is negative infinity using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is negative infinity; otherwise, <see langword="false" />.</returns>
    private static bool IsNegativeInfinityOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsNegativeInfinity(value);

    /// <summary>
    /// Determines whether a value is normal using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is normal; otherwise, <see langword="false" />.</returns>
    private static bool IsNormalOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsNormal(value);

    /// <summary>
    /// Determines whether a value is positive using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is positive; otherwise, <see langword="false" />.</returns>
    private static bool IsPositiveOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsPositive(value);

    /// <summary>
    /// Determines whether a value is positive infinity using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is positive infinity; otherwise, <see langword="false" />.</returns>
    private static bool IsPositiveInfinityOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsPositiveInfinity(value);

    /// <summary>
    /// Determines whether a value is a real number using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is real; otherwise, <see langword="false" />.</returns>
    private static bool IsRealNumberOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsRealNumber(value);

    /// <summary>
    /// Determines whether a value is subnormal using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is subnormal; otherwise, <see langword="false" />.</returns>
    private static bool IsSubnormalOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsSubnormal(value);

    /// <summary>
    /// Determines whether a value is zero using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns><see langword="true" /> if the value is zero; otherwise, <see langword="false" />.</returns>
    private static bool IsZeroOf<TNumber>(TNumber value)
        where TNumber : INumberBase<TNumber> =>
        TNumber.IsZero(value);

    /// <summary>
    /// Returns the number with the greater IEEE-style magnitude using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The value with the greater magnitude.</returns>
    private static TNumber MaxMagnitudeNumberOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumberBase<TNumber> =>
        TNumber.MaxMagnitudeNumber(x, y);

    /// <summary>
    /// Returns the number with the smaller IEEE-style magnitude using the <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The value with the smaller magnitude.</returns>
    private static TNumber MinMagnitudeNumberOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumberBase<TNumber> =>
        TNumber.MinMagnitudeNumber(x, y);

    /// <summary>
    /// Parses a number from a string using the culture-and-style <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <returns>The parsed value.</returns>
    private static TNumber ParseString<TNumber>(string text)
        where TNumber : INumberBase<TNumber> =>
        TNumber.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a number from a character span using the culture-and-style <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <returns>The parsed value.</returns>
    private static TNumber ParseSpan<TNumber>(ReadOnlySpan<char> text)
        where TNumber : INumberBase<TNumber> =>
        TNumber.Parse(text, NumberStyles.Number, CultureInfo.InvariantCulture);

    /// <summary>
    /// Attempts to parse a number from a string using the culture-and-style <see cref="INumberBase{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    private static bool TryParseString<TNumber>(string? text, out TNumber result)
        where TNumber : INumberBase<TNumber> =>
        TNumber.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result!);

    /// <summary>
    /// Attempts to parse a number from a character span using the culture-and-style <see cref="INumberBase{TSelf}" />
    /// contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="result">When this method returns, contains the parsed value.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    private static bool TryParseSpan<TNumber>(ReadOnlySpan<char> text, out TNumber result)
        where TNumber : INumberBase<TNumber> =>
        TNumber.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out result!);

    /// <summary>
    /// Verifies that the sign-copying and ordered-extreme selectors defined by <see cref="INumber{TSelf}" /> apply the
    /// sign of one operand to the magnitude of another and order rationals by value.
    /// </summary>
    [TestMethod]
    public void GenericMath_WhenApplyingNumberSelectors_ShouldMatchSignAndOrdering()
    {
        Assert.AreEqual(new Fraction<int>(-3, 4), CopySignOf(new Fraction<int>(3, 4), Fraction<int>.MinusOne));
        Assert.AreEqual(new Fraction<int>(3, 4), CopySignOf(new Fraction<int>(-3, 4), Fraction<int>.One));
        Assert.AreEqual(new Fraction<int>(1, 2), MaxNumberOf(new Fraction<int>(1, 3), new Fraction<int>(1, 2)));
        Assert.AreEqual(new Fraction<int>(1, 3), MinNumberOf(new Fraction<int>(1, 3), new Fraction<int>(1, 2)));
    }

    /// <summary>
    /// Applies the sign of one value to the magnitude of another using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="value">The value supplying the magnitude.</param>
    /// <param name="sign">The value supplying the sign.</param>
    /// <returns>The magnitude of <paramref name="value" /> carrying the sign of <paramref name="sign" />.</returns>
    private static TNumber CopySignOf<TNumber>(TNumber value, TNumber sign)
        where TNumber : INumber<TNumber> =>
        TNumber.CopySign(value, sign);

    /// <summary>
    /// Returns the larger of two values, ignoring NaN propagation, using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The larger of the two values.</returns>
    private static TNumber MaxNumberOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumber<TNumber> =>
        TNumber.MaxNumber(x, y);

    /// <summary>
    /// Returns the smaller of two values, ignoring NaN propagation, using the <see cref="INumber{TSelf}" /> contract.
    /// </summary>
    /// <typeparam name="TNumber">The number type.</typeparam>
    /// <param name="x">The first value.</param>
    /// <param name="y">The second value.</param>
    /// <returns>The smaller of the two values.</returns>
    private static TNumber MinNumberOf<TNumber>(TNumber x, TNumber y)
        where TNumber : INumber<TNumber> =>
        TNumber.MinNumber(x, y);
}
