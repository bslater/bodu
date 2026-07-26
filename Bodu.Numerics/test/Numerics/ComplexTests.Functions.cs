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

    /// <summary>
    /// Verifies that the exponential of a value with a non-finite component matches <see cref="System.Numerics.Complex" />,
    /// pinning the non-finite Exp special cases (e.g. <c>Exp(∞, 0)</c> yields an infinite real part and a NaN imaginary
    /// part from <c>∞·sin(0)</c>) that the edge oracle otherwise covers only in bulk.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Exp_WhenInputIsNonFinite_ShouldMatchBcl()
    {
        foreach ((double re, double im) in new[]
        {
            (double.PositiveInfinity, 0.0),
            (0.0, double.PositiveInfinity),
            (double.NegativeInfinity, 1.0),
        })
        {
            AssertClose(
                Complex<double>.Exp(new Complex<double>(re, im)),
                System.Numerics.Complex.Exp(new System.Numerics.Complex(re, im)),
                $"exp({re},{im})",
                1e-12);
        }
    }

    /// <summary>
    /// Verifies that the tangent of a value with a large imaginary component saturates to the imaginary unit instead of
    /// overflowing to NaN — the numpy #3010 / #5518 defect that a naive <c>sin/cos</c> quotient exhibits.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Tan_WhenImaginaryComponentIsLarge_ShouldSaturateWithoutOverflow()
    {
        Complex<double> result = Complex<double>.Tan(new Complex<double>(0.0, 1000.0));

        Assert.IsTrue(double.IsFinite(result.Real) && double.IsFinite(result.Imaginary), $"result was {result}");
        Assert.AreEqual(0.0, result.Real, 1e-12);
        Assert.AreEqual(1.0, result.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that the hyperbolic tangent of a value with a large real component saturates to one instead of
    /// overflowing to NaN.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Tanh_WhenRealComponentIsLarge_ShouldSaturateWithoutOverflow()
    {
        Complex<double> result = Complex<double>.Tanh(new Complex<double>(1000.0, 0.0));

        Assert.IsTrue(double.IsFinite(result.Real) && double.IsFinite(result.Imaginary), $"result was {result}");
        Assert.AreEqual(1.0, result.Real, 1e-12);
        Assert.AreEqual(0.0, result.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that the tangent is continuous and finite on both sides of the internal large-imaginary threshold, so
    /// the two code paths agree at the boundary.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Tan_WhenStraddlingLargeArgumentThreshold_ShouldBeContinuous()
    {
        Complex<double> below = Complex<double>.Tan(new Complex<double>(1.0, 3.9));
        Complex<double> above = Complex<double>.Tan(new Complex<double>(1.0, 4.1));

        Assert.IsTrue(double.IsFinite(below.Real) && double.IsFinite(below.Imaginary), $"below was {below}");
        Assert.IsTrue(double.IsFinite(above.Real) && double.IsFinite(above.Imaginary), $"above was {above}");
        Assert.AreEqual(below.Real, above.Real, 1e-3);
        Assert.AreEqual(below.Imaginary, above.Imaginary, 1e-3);
    }

    /// <summary>
    /// Verifies that the square root of a value whose components are near the overflow ceiling stays finite, because the
    /// magnitude is evaluated with overflow-avoiding rescaling rather than forming the sum of squares directly.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Sqrt_WhenComponentsAreNearOverflow_ShouldStayFinite()
    {
        Complex<double> result = Complex<double>.Sqrt(new Complex<double>(1e308, 1e308));

        Assert.IsTrue(double.IsFinite(result.Real) && double.IsFinite(result.Imaginary), $"result was {result}");
        AssertClose(result, System.Numerics.Complex.Sqrt(new System.Numerics.Complex(1e308, 1e308)), "sqrt(1e308,1e308)", 1e-9);
    }

    /// <summary>
    /// Verifies that the square root of values with an infinite component matches <see cref="System.Numerics.Complex" />.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Sqrt_WhenComponentIsInfinite_ShouldMatchBcl()
    {
        foreach ((double re, double im) in new[] { (double.PositiveInfinity, 1.0), (double.NegativeInfinity, 1.0), (1.0, double.PositiveInfinity) })
        {
            AssertBitExact(
                Complex<double>.Sqrt(new Complex<double>(re, im)),
                System.Numerics.Complex.Sqrt(new System.Numerics.Complex(re, im)),
                $"sqrt({re},{im})");
        }
    }

    /// <summary>
    /// Verifies that the square root of a negative real value with a signed-zero imaginary component matches
    /// <see cref="System.Numerics.Complex" /> — pinning the branch-cut convention .NET actually uses (the positive
    /// imaginary root for both zero signs) rather than the C99 sign-carrying variant.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Sqrt_WhenNegativeRealWithSignedZeroImaginary_ShouldMatchBcl()
    {
        AssertBitExact(
            Complex<double>.Sqrt(new Complex<double>(-4.0, 0.0)),
            System.Numerics.Complex.Sqrt(new System.Numerics.Complex(-4.0, 0.0)),
            "sqrt(-4,+0)");
        AssertBitExact(
            Complex<double>.Sqrt(new Complex<double>(-4.0, -0.0)),
            System.Numerics.Complex.Sqrt(new System.Numerics.Complex(-4.0, -0.0)),
            "sqrt(-4,-0)");
    }

    /// <summary>
    /// Verifies that raising any base to the zero power returns one, including the zero, NaN, and infinite bases, since
    /// the exponent is tested before the base.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Pow_WhenExponentIsZero_ShouldReturnOneForEveryBase()
    {
        Assert.AreEqual(Complex<double>.One, Complex<double>.Pow(new Complex<double>(3.0, 4.0), Complex<double>.Zero));
        Assert.AreEqual(Complex<double>.One, Complex<double>.Pow(Complex<double>.Zero, Complex<double>.Zero));
        Assert.AreEqual(Complex<double>.One, Complex<double>.Pow(Complex<double>.NaN, Complex<double>.Zero));
    }

    /// <summary>
    /// Verifies that raising zero to a non-zero power returns zero — including a negative exponent, where .NET returns
    /// zero rather than the infinity that real <see cref="Math.Pow(double, double)" /> produces.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Pow_WhenBaseIsZeroAndExponentNonZero_ShouldReturnZero()
    {
        Assert.AreEqual(Complex<double>.Zero, Complex<double>.Pow(Complex<double>.Zero, new Complex<double>(2.0, 0.0)));
        Assert.AreEqual(Complex<double>.Zero, Complex<double>.Pow(Complex<double>.Zero, new Complex<double>(-1.0, 0.0)));
    }

    /// <summary>
    /// Verifies that the natural logarithm places its branch cut on the negative real axis with the imaginary zero's
    /// sign selecting the side, matching <see cref="System.Numerics.Complex" />, and that the logarithm of zero is
    /// <c>(-∞, 0)</c>.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Log_WhenOnBranchCutOrZero_ShouldMatchBcl()
    {
        foreach ((double re, double im) in new[] { (-1.0, 0.0), (-1.0, -0.0), (0.0, 0.0) })
        {
            AssertBitExact(
                Complex<double>.Log(new Complex<double>(re, im)),
                System.Numerics.Complex.Log(new System.Numerics.Complex(re, im)),
                $"log({re},{im})");
        }
    }
}
