// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Functions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the square root of negative one is the imaginary unit.
    /// </summary>
    [TestMethod]
    public void Sqrt_WhenValueIsNegativeOne_ShouldReturnImaginaryUnit()
    {
        Complex<double> result = Complex<double>.Sqrt(new Complex<double>(-1.0, 0.0));

        Assert.AreEqual(0.0, result.Real, 1e-12);
        Assert.AreEqual(1.0, result.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that the square root of a purely imaginary value has equal real and imaginary parts.
    /// </summary>
    [TestMethod]
    public void Sqrt_WhenValueIsPurelyImaginary_ShouldHaveEqualParts()
    {
        Complex<double> result = Complex<double>.Sqrt(new Complex<double>(0.0, 2.0));

        Assert.AreEqual(result.Real, result.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that squaring the square root reproduces the original value.
    /// </summary>
    [TestMethod]
    public void Sqrt_WhenSquared_ShouldReproduceOriginal()
    {
        var value = new Complex<double>(3.0, 4.0);

        Complex<double> root = Complex<double>.Sqrt(value);
        Complex<double> squared = root * root;

        Assert.AreEqual(value.Real, squared.Real, 1e-9);
        Assert.AreEqual(value.Imaginary, squared.Imaginary, 1e-9);
    }

    /// <summary>
    /// Verifies that the exponential of zero is one.
    /// </summary>
    [TestMethod]
    public void Exp_WhenValueIsZero_ShouldReturnOne()
    {
        Complex<double> result = Complex<double>.Exp(Complex<double>.Zero);

        Assert.AreEqual(1.0, result.Real, 1e-12);
        Assert.AreEqual(0.0, result.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that the natural logarithm and exponential are inverses.
    /// </summary>
    [TestMethod]
    public void ExpAndLog_ShouldBeInverses()
    {
        var value = new Complex<double>(1.0, 0.5);

        Complex<double> result = Complex<double>.Exp(Complex<double>.Log(value));

        Assert.AreEqual(value.Real, result.Real, 1e-9);
        Assert.AreEqual(value.Imaginary, result.Imaginary, 1e-9);
    }

    /// <summary>
    /// Verifies that raising the imaginary unit to the second power yields negative one.
    /// </summary>
    [TestMethod]
    public void Pow_WhenSquaringImaginaryUnit_ShouldReturnNegativeOne()
    {
        Complex<double> result = Complex<double>.Pow(Complex<double>.ImaginaryOne, new Complex<double>(2.0, 0.0));

        Assert.AreEqual(-1.0, result.Real, 1e-12);
        Assert.AreEqual(0.0, result.Imaginary, 1e-12);
    }
}
