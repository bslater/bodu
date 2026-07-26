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
}
