// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.GenericMath.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the generic-math identity values expose the expected components.
    /// </summary>
    [TestMethod]
    public void GenericMathIdentities_ShouldExposeExpectedValues()
    {
        Assert.AreEqual(Complex<double>.Zero, Identity<Complex<double>>(additive: true));
        Assert.AreEqual(Complex<double>.One, Identity<Complex<double>>(additive: false));
        Assert.AreEqual(new Complex<double>(-1.0, 0.0), NegativeOne<Complex<double>>());
        Assert.AreEqual(2, Radix<Complex<double>>());
    }

    /// <summary>
    /// Verifies that <see cref="System.Numerics.INumberBase{TSelf}.IsComplexNumber" /> is true only when both
    /// components are non-zero.
    /// </summary>
    [TestMethod]
    public void IsComplexNumber_ShouldRequireBothComponentsNonZero()
    {
        Assert.IsTrue(IsComplexNumber(new Complex<double>(1.0, 1.0)));
        Assert.IsFalse(IsComplexNumber(new Complex<double>(1.0, 0.0)));
        Assert.IsFalse(IsComplexNumber(new Complex<double>(0.0, 1.0)));
    }

    /// <summary>
    /// Verifies that the real/imaginary/zero classifications match the definitions for representative values.
    /// </summary>
    [TestMethod]
    public void RealImaginaryZeroPredicates_ShouldClassifyRepresentativeValues()
    {
        Assert.IsTrue(IsRealNumber(new Complex<double>(3.0, 0.0)));
        Assert.IsFalse(IsRealNumber(new Complex<double>(3.0, 1.0)));

        Assert.IsTrue(IsImaginaryNumber(new Complex<double>(0.0, 3.0)));
        Assert.IsFalse(IsImaginaryNumber(new Complex<double>(1.0, 3.0)));

        Assert.IsTrue(IsZeroValue(Complex<double>.Zero));
        Assert.IsFalse(IsZeroValue(Complex<double>.One));
    }

    /// <summary>
    /// Verifies that the sign predicates report true only for a real value on the real axis with the matching sign,
    /// matching <see cref="System.Numerics.Complex" />.
    /// </summary>
    [TestMethod]
    public void SignPredicates_ShouldConsiderOnlyRealAxisValues()
    {
        Assert.IsTrue(IsNegative(new Complex<double>(-3.0, 0.0)));
        Assert.IsFalse(IsNegative(new Complex<double>(-3.0, 1.0)));

        Assert.IsTrue(IsPositive(new Complex<double>(3.0, 0.0)));
        Assert.IsFalse(IsPositive(new Complex<double>(3.0, 1.0)));
    }

    /// <summary>
    /// Verifies that the magnitude selectors resolve an exact-magnitude tie by the real component's magnitude — the
    /// documented deterministic rule for a non-ordered complex type.
    /// </summary>
    [TestMethod]
    public void MagnitudeSelectors_WhenMagnitudesTie_ShouldResolveByRealMagnitude()
    {
        var wideReal = new Complex<double>(4.0, 3.0);   // magnitude 5, |real| 4
        var narrowReal = new Complex<double>(3.0, 4.0); // magnitude 5, |real| 3

        Assert.AreEqual(wideReal, MaxMagnitude(wideReal, narrowReal));
        Assert.AreEqual(wideReal, MaxMagnitude(narrowReal, wideReal));
        Assert.AreEqual(narrowReal, MinMagnitude(wideReal, narrowReal));
        Assert.AreEqual(narrowReal, MinMagnitude(narrowReal, wideReal));
    }

    /// <summary>
    /// Verifies that the checked, saturating, and truncating creation contracts embed a real source value on the real
    /// axis and read it back out through the reverse conversion.
    /// </summary>
    [TestMethod]
    public void CreationContracts_WhenConvertingRealValue_ShouldRoundTrip()
    {
        Assert.AreEqual(new Complex<double>(7.0, 0.0), ConvertChecked<int, Complex<double>>(7));
        Assert.AreEqual(new Complex<double>(7.0, 0.0), ConvertSaturating<int, Complex<double>>(7));
        Assert.AreEqual(new Complex<double>(7.0, 0.0), ConvertTruncating<int, Complex<double>>(7));

        Assert.AreEqual(7.0, ConvertChecked<Complex<double>, double>(new Complex<double>(7.0, 0.0)));
        Assert.AreEqual(7.0, ConvertSaturating<Complex<double>, double>(new Complex<double>(7.0, 0.0)));
        Assert.AreEqual(7.0, ConvertTruncating<Complex<double>, double>(new Complex<double>(7.0, 0.0)));
    }

    /// <summary>
    /// Verifies that converting <em>from</em> an unsupported numeric type throws <see cref="NotSupportedException" />
    /// through all three creation contracts, exercising the defensive handling in the from-conversion members.
    /// </summary>
    [TestMethod]
    public void CreationContracts_WhenConvertingFromUnsupportedType_ShouldThrowNotSupported()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertChecked<UnsupportedNumber, Complex<double>>(default); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertSaturating<UnsupportedNumber, Complex<double>>(default); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertTruncating<UnsupportedNumber, Complex<double>>(default); });
    }

    /// <summary>
    /// Verifies that converting a real-valued complex <em>to</em> an unsupported numeric type throws
    /// <see cref="NotSupportedException" /> through all three creation contracts, exercising the defensive handling in
    /// the to-conversion members.
    /// </summary>
    [TestMethod]
    public void CreationContracts_WhenConvertingToUnsupportedType_ShouldThrowNotSupported()
    {
        var real = new Complex<double>(3.0, 0.0);

        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertChecked<Complex<double>, UnsupportedNumber>(real); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertSaturating<Complex<double>, UnsupportedNumber>(real); });
        _ = Assert.ThrowsExactly<NotSupportedException>(() => { _ = ConvertTruncating<Complex<double>, UnsupportedNumber>(real); });
    }

    /// <summary>
    /// Verifies that the static <see cref="System.Numerics.INumberBase{TSelf}" /> parse entry points round-trip the
    /// bracketed form on both the string and span overloads.
    /// </summary>
    [TestMethod]
    public void ParsingThroughNumberBase_ShouldRoundTripBracketedForm()
    {
        var expected = new Complex<double>(3.0, -4.0);

        Assert.AreEqual(expected, ParseNumberBase<Complex<double>>("<3; -4>"));
        Assert.IsTrue(TryParseNumberBase<Complex<double>>("<3; -4>", out Complex<double> parsed));
        Assert.AreEqual(expected, parsed);
        Assert.IsFalse(TryParseNumberBase<Complex<double>>("not a complex", out _));
    }

    /// <summary>Invokes the generic-math checked conversion between two number types.</summary>
    /// <typeparam name="TSource">The source number type.</typeparam>
    /// <typeparam name="TTarget">The destination number type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    private static TTarget ConvertChecked<TSource, TTarget>(TSource value)
        where TSource : INumberBase<TSource>
        where TTarget : INumberBase<TTarget> =>
        TTarget.CreateChecked(value);

    /// <summary>Invokes the generic-math saturating conversion between two number types.</summary>
    /// <typeparam name="TSource">The source number type.</typeparam>
    /// <typeparam name="TTarget">The destination number type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    private static TTarget ConvertSaturating<TSource, TTarget>(TSource value)
        where TSource : INumberBase<TSource>
        where TTarget : INumberBase<TTarget> =>
        TTarget.CreateSaturating(value);

    /// <summary>Invokes the generic-math truncating conversion between two number types.</summary>
    /// <typeparam name="TSource">The source number type.</typeparam>
    /// <typeparam name="TTarget">The destination number type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    private static TTarget ConvertTruncating<TSource, TTarget>(TSource value)
        where TSource : INumberBase<TSource>
        where TTarget : INumberBase<TTarget> =>
        TTarget.CreateTruncating(value);

    /// <summary>Invokes the static <see cref="System.Numerics.INumberBase{TSelf}" /> string parse.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <returns>The parsed value.</returns>
    private static TC ParseNumberBase<TC>(string text)
        where TC : INumberBase<TC> =>
        TC.Parse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Invokes the static <see cref="System.Numerics.INumberBase{TSelf}" /> span try-parse.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="value">Receives the parsed value on success.</param>
    /// <returns><see langword="true" /> when parsing succeeded.</returns>
    private static bool TryParseNumberBase<TC>(string text, out TC value)
        where TC : INumberBase<TC> =>
        TC.TryParse(text.AsSpan(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);

    /// <summary>Returns the additive or multiplicative identity through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="additive">Whether to return the additive identity rather than the multiplicative identity.</param>
    /// <returns>The requested identity value.</returns>
    private static TC Identity<TC>(bool additive)
        where TC : IAdditiveIdentity<TC, TC>, IMultiplicativeIdentity<TC, TC> =>
        additive ? TC.AdditiveIdentity : TC.MultiplicativeIdentity;

    /// <summary>Returns the signed-number negative one through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <returns>The negative-one value.</returns>
    private static TC NegativeOne<TC>()
        where TC : ISignedNumber<TC> =>
        TC.NegativeOne;

    /// <summary>Returns the radix through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <returns>The radix.</returns>
    private static int Radix<TC>()
        where TC : INumberBase<TC> =>
        TC.Radix;

    /// <summary>Evaluates the complex-number predicate through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns>The predicate result.</returns>
    private static bool IsComplexNumber<TC>(TC value)
        where TC : INumberBase<TC> =>
        TC.IsComplexNumber(value);

    /// <summary>Evaluates the real-number predicate through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns>The predicate result.</returns>
    private static bool IsRealNumber<TC>(TC value)
        where TC : INumberBase<TC> =>
        TC.IsRealNumber(value);

    /// <summary>Evaluates the imaginary-number predicate through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns>The predicate result.</returns>
    private static bool IsImaginaryNumber<TC>(TC value)
        where TC : INumberBase<TC> =>
        TC.IsImaginaryNumber(value);

    /// <summary>Evaluates the zero predicate through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns>The predicate result.</returns>
    private static bool IsZeroValue<TC>(TC value)
        where TC : INumberBase<TC> =>
        TC.IsZero(value);

    /// <summary>Evaluates the negative predicate through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns>The predicate result.</returns>
    private static bool IsNegative<TC>(TC value)
        where TC : INumberBase<TC> =>
        TC.IsNegative(value);

    /// <summary>Evaluates the positive predicate through the generic-math interface.</summary>
    /// <typeparam name="TC">The number type.</typeparam>
    /// <param name="value">The value to classify.</param>
    /// <returns>The predicate result.</returns>
    private static bool IsPositive<TC>(TC value)
        where TC : INumberBase<TC> =>
        TC.IsPositive(value);

    /// <summary>
    /// Verifies that the public static classification methods <see cref="Complex{T}.IsNaN" />,
    /// <see cref="Complex{T}.IsInfinity" />, and <see cref="Complex{T}.IsFinite" /> agree with the
    /// <see cref="System.Numerics.Complex" /> oracle across finite, infinite, NaN, and mixed component shapes —
    /// including the infinite-plus-NaN case, where an infinite component dominates NaN.
    /// </summary>
    [TestMethod]
    public void IsNaNIsInfinityIsFinite_WhenClassifyingComponentShapes_ShouldMatchBclOracle()
    {
        (double Real, double Imaginary)[] shapes =
        [
            (1.0, 2.0),
            (0.0, 0.0),
            (double.PositiveInfinity, 1.0),
            (1.0, double.NegativeInfinity),
            (double.NaN, 1.0),
            (1.0, double.NaN),
            (double.NaN, double.NaN),
            (double.PositiveInfinity, double.NaN),
            (double.NaN, double.NegativeInfinity),
            (double.PositiveInfinity, double.NegativeInfinity),
        ];

        foreach ((double real, double imaginary) in shapes)
        {
            var value = new Complex<double>(real, imaginary);
            var oracle = new System.Numerics.Complex(real, imaginary);

            Assert.AreEqual(System.Numerics.Complex.IsNaN(oracle), Complex<double>.IsNaN(value), $"IsNaN({real}, {imaginary})");
            Assert.AreEqual(System.Numerics.Complex.IsInfinity(oracle), Complex<double>.IsInfinity(value), $"IsInfinity({real}, {imaginary})");
            Assert.AreEqual(System.Numerics.Complex.IsFinite(oracle), Complex<double>.IsFinite(value), $"IsFinite({real}, {imaginary})");
        }
    }
}
