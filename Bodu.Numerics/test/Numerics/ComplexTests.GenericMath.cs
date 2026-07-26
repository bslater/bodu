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
}
