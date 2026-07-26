// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that <see cref="Complex{T}.Magnitude" /> returns the Euclidean distance from the origin.
    /// </summary>
    [TestMethod]
    public void Magnitude_WhenValueIsThreeFourI_ShouldReturnFive()
    {
        Assert.AreEqual(5.0, new Complex<double>(3.0, 4.0).Magnitude, 1e-12);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Phase" /> returns the argument measured from the positive real axis.
    /// </summary>
    [TestMethod]
    public void Phase_WhenValueIsImaginaryUnit_ShouldReturnHalfPi()
    {
        Assert.AreEqual(Math.PI / 2.0, Complex<double>.ImaginaryOne.Phase, 1e-12);
    }

    /// <summary>
    /// Verifies that the well-known static values expose the expected components.
    /// </summary>
    [TestMethod]
    public void WellKnownValues_ShouldExposeExpectedComponents()
    {
        Assert.AreEqual(new Complex<double>(0.0, 0.0), Complex<double>.Zero);
        Assert.AreEqual(new Complex<double>(1.0, 0.0), Complex<double>.One);
        Assert.AreEqual(new Complex<double>(0.0, 1.0), Complex<double>.ImaginaryOne);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Infinity" /> carries infinite components and <see cref="Complex{T}.NaN" />
    /// carries NaN components.
    /// </summary>
    [TestMethod]
    public void NonFiniteStatics_ShouldCarryNonFiniteComponents()
    {
        Assert.IsTrue(double.IsPositiveInfinity(Complex<double>.Infinity.Real));
        Assert.IsTrue(double.IsPositiveInfinity(Complex<double>.Infinity.Imaginary));
        Assert.IsTrue(double.IsNaN(Complex<double>.NaN.Real));
        Assert.IsTrue(double.IsNaN(Complex<double>.NaN.Imaginary));
    }

    /// <summary>
    /// Verifies that magnitude and phase round-trip through <see cref="Complex{T}.FromPolarCoordinates" />.
    /// </summary>
    [TestMethod]
    public void MagnitudeAndPhase_ShouldRoundTripThroughPolarCoordinates()
    {
        var original = new Complex<double>(-3.0, 2.0);

        Complex<double> rebuilt = Complex<double>.FromPolarCoordinates(original.Magnitude, original.Phase);

        Assert.AreEqual(original.Real, rebuilt.Real, 1e-12);
        Assert.AreEqual(original.Imaginary, rebuilt.Imaginary, 1e-12);
    }

    /// <summary>
    /// Verifies that the phase on the negative real axis honors the sign of the imaginary zero — <c>+π</c> for
    /// <c>+0</c> and <c>-π</c> for <c>-0</c> — and that the phase of a signed zero carries the imaginary zero's sign,
    /// matching <see cref="System.Numerics.Complex.Phase" />.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void Phase_WhenOnNegativeRealAxisOrSignedZero_ShouldMatchBclSignedZero()
    {
        foreach ((double re, double im) in new[] { (-1.0, 0.0), (-1.0, -0.0), (0.0, 0.0), (0.0, -0.0) })
        {
            double mine = new Complex<double>(re, im).Phase;
            double bcl = new System.Numerics.Complex(re, im).Phase;

            Assert.AreEqual(
                BitConverter.DoubleToInt64Bits(bcl),
                BitConverter.DoubleToInt64Bits(mine),
                $"phase({re},{im}): mine={mine} bcl={bcl}");
        }
    }
}
