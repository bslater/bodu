// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the component constructor stores the real and imaginary parts unchanged.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGivenComponents_ShouldStoreThem()
    {
        var value = new Complex<double>(3.0, -4.0);

        Assert.AreEqual(3.0, value.Real);
        Assert.AreEqual(-4.0, value.Imaginary);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Create" /> produces the same value as the constructor.
    /// </summary>
    [TestMethod]
    public void Create_WhenGivenComponents_ShouldEqualConstructedValue()
    {
        Assert.AreEqual(new Complex<double>(1.5, 2.5), Complex<double>.Create(1.5, 2.5));
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.Deconstruct" /> yields the original components.
    /// </summary>
    [TestMethod]
    public void Deconstruct_WhenCalled_ShouldYieldComponents()
    {
        (double real, double imaginary) = new Complex<double>(7.0, 8.0);

        Assert.AreEqual(7.0, real);
        Assert.AreEqual(8.0, imaginary);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}.FromPolarCoordinates" /> reconstructs the expected Cartesian value.
    /// </summary>
    [TestMethod]
    public void FromPolarCoordinates_WhenGivenMagnitudeAndPhase_ShouldReturnCartesianValue()
    {
        Complex<double> value = Complex<double>.FromPolarCoordinates(2.0, Math.PI / 2.0);

        Assert.AreEqual(0.0, value.Real, 1e-12);
        Assert.AreEqual(2.0, value.Imaginary, 1e-12);
    }
}
