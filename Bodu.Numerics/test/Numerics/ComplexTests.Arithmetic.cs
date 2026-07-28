// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Arithmetic.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the named arithmetic methods agree with the corresponding operators.
    /// </summary>
    [TestMethod]
    public void NamedArithmeticMethods_ShouldAgreeWithOperators()
    {
        var a = new Complex<double>(2.0, -3.0);
        var b = new Complex<double>(-1.0, 4.0);

        Assert.AreEqual(a + b, Complex<double>.Add(a, b));
        Assert.AreEqual(a - b, Complex<double>.Subtract(a, b));
        Assert.AreEqual(a * b, Complex<double>.Multiply(a, b));
        Assert.AreEqual(a / b, Complex<double>.Divide(a, b));
        Assert.AreEqual(-a, Complex<double>.Negate(a));
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Conjugate" /> reverses the sign of the imaginary component.
    /// </summary>
    [TestMethod]
    public void Conjugate_WhenApplied_ShouldReverseImaginarySign()
    {
        Assert.AreEqual(new Complex<double>(3.0, -4.0), Complex<double>.Conjugate(new Complex<double>(3.0, 4.0)));
    }

    /// <summary>
    /// Verifies that the product of a value and its conjugate equals the squared magnitude on the real axis.
    /// </summary>
    [TestMethod]
    public void Conjugate_WhenMultipliedByValue_ShouldEqualSquaredMagnitude()
    {
        var value = new Complex<double>(3.0, 4.0);

        Complex<double> product = value * Complex<double>.Conjugate(value);

        Assert.AreEqual(25.0, product.Real, 1e-12);
        Assert.AreEqual(0.0, product.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Reciprocal" /> of a non-zero value is its multiplicative inverse.
    /// </summary>
    [TestMethod]
    public void Reciprocal_WhenValueIsNonZero_ShouldReturnMultiplicativeInverse()
    {
        var value = new Complex<double>(1.0, 2.0);

        Complex<double> product = value * Complex<double>.Reciprocal(value);

        Assert.AreEqual(1.0, product.Real, 1e-12);
        Assert.AreEqual(0.0, product.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Reciprocal" /> of zero returns zero rather than an infinity.
    /// </summary>
    [TestMethod]
    public void Reciprocal_WhenValueIsZero_ShouldReturnZero()
    {
        Assert.AreEqual(Complex<double>.Zero, Complex<double>.Reciprocal(Complex<double>.Zero));
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Abs" /> returns the magnitude and does not overflow for large components.
    /// </summary>
    [TestMethod]
    public void Abs_WhenComponentsAreLarge_ShouldNotOverflow()
    {
        double result = Complex<double>.Abs(new Complex<double>(3e200, 4e200));

        Assert.AreEqual(5e200, result, 1e188);
    }

    /// <summary>
    /// Verifies that dividing by a divisor with very large components stays finite, because division uses Smith's
    /// algorithm rather than forming the divisor's sum of squares (which would overflow to infinity, yielding NaN).
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Division_WhenDivisorComponentsAreLarge_ShouldStayFinite()
    {
        // c² + d² = 2e400 overflows, but Smith's evaluates c + d·(c/d) = 2e200, which is finite.
        Complex<double> result = new Complex<double>(1.0, 1.0) / new Complex<double>(1e200, 1e200);

        Assert.IsTrue(double.IsFinite(result.Real) && double.IsFinite(result.Imaginary), $"result was {result}");
        Assert.AreEqual(1e-200, result.Real, 1e-212);
        Assert.AreEqual(0.0, result.Imaginary, 1e-212);
    }

    /// <summary>
    /// Verifies that the reciprocal of an infinite value matches <see cref="System.Numerics.Complex.Reciprocal" />
    /// (which is <c>NaN</c>, since <c>1 / ∞</c> under Smith's algorithm forms <c>∞/∞</c>) — pinning the actual .NET
    /// behaviour rather than the intuitive zero.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Reciprocal_WhenValueIsInfinite_ShouldMatchBcl()
    {
        AssertBitExact(
            Complex<double>.Reciprocal(Complex<double>.Infinity),
            System.Numerics.Complex.Reciprocal(new System.Numerics.Complex(double.PositiveInfinity, double.PositiveInfinity)),
            "reciprocal(inf,inf)");
    }

    /// <summary>
    /// Verifies that multiplication uses the direct four-multiply form without C99 Annex-G infinity recovery, matching
    /// <see cref="System.Numerics.Complex" /> exactly rather than the recovered infinities that numpy / C99 produce.
    /// This pins the deliberate specification choice so a "correcting" change is caught.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Multiply_WhenOperandsAreInfinite_ShouldMatchBclNaiveResult()
    {
        var left = new Complex<double>(1.0, 1.0);
        var right = new Complex<double>(double.PositiveInfinity, double.PositiveInfinity);

        AssertBitExact(
            left * right,
            new System.Numerics.Complex(1.0, 1.0) * new System.Numerics.Complex(double.PositiveInfinity, double.PositiveInfinity),
            "(1+i)*(inf+inf i)");
    }

    /// <summary>
    /// Verifies that the magnitude of a subnormal component is preserved rather than flushed to zero.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Abs_WhenComponentIsSubnormal_ShouldNotFlushToZero()
    {
        Assert.AreEqual(double.Epsilon, Complex<double>.Abs(new Complex<double>(double.Epsilon, 0.0)));
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Abs" /> of an infinite component paired with a <c>NaN</c> component returns
    /// positive infinity — the IEEE 754 <c>hypot</c> value (and the value of <see cref="double.Hypot(double, double)" /> on every
    /// runtime). The result is asserted as a fixed constant rather than against <see cref="System.Numerics.Complex.Abs" />
    /// because that oracle is runtime-dependent for this input: .NET 8 returned <c>NaN</c> from a since-corrected guard,
    /// while .NET 9 and later return positive infinity. Pinning the stable IEEE value keeps the magnitude identical
    /// across every framework the library targets.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Abs_WhenOneComponentIsInfiniteAndOtherIsNaN_ShouldReturnPositiveInfinity()
    {
        double infThenNaN = Complex<double>.Abs(new Complex<double>(double.NegativeInfinity, double.NaN));
        double nanThenInf = Complex<double>.Abs(new Complex<double>(double.NaN, double.PositiveInfinity));

        Assert.IsTrue(double.IsPositiveInfinity(infThenNaN), $"abs(-inf, NaN) was {infThenNaN}");
        Assert.IsTrue(double.IsPositiveInfinity(nanThenInf), $"abs(NaN, +inf) was {nanThenInf}");
    }

    /// <summary>
    /// Verifies that dividing by zero yields NaN components rather than an infinity, matching
    /// <see cref="System.Numerics.Complex" /> — the <c>0/0</c> that Smith's algorithm forms for a zero divisor.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Division_WhenDivisorIsZero_ShouldMatchBclNaN()
    {
        Complex<double> result = new Complex<double>(1.0, 2.0) / Complex<double>.Zero;
        System.Numerics.Complex bcl = new System.Numerics.Complex(1.0, 2.0) / System.Numerics.Complex.Zero;

        Assert.IsTrue(double.IsNaN(result.Real) && double.IsNaN(result.Imaginary), $"result was {result}");
        AssertBitExact(result, bcl, "(1+2i)/0");
    }
}
