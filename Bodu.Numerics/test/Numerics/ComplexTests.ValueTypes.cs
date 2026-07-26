// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.ValueTypes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that <see cref="Complex{T}" /> over <see cref="float" /> satisfies the conjugate-product identity, since
    /// no framework oracle exists for non-<see cref="double" /> component types.
    /// </summary>
    [TestMethod]
    public void Float_ConjugateProduct_ShouldEqualSquaredMagnitude()
    {
        var value = new Complex<float>(3.0f, 4.0f);

        Complex<float> product = value * Complex<float>.Conjugate(value);

        Assert.AreEqual(25.0f, product.Real, 1e-3f);
        Assert.AreEqual(0.0f, product.Imaginary, 1e-3f);
    }

    /// <summary>
    /// Verifies that the exponential and logarithm are inverses for <see cref="Complex{T}" /> over <see cref="float" />.
    /// </summary>
    [TestMethod]
    public void Float_ExpAndLog_ShouldBeInverses()
    {
        var value = new Complex<float>(1.0f, 0.5f);

        Complex<float> result = Complex<float>.Exp(Complex<float>.Log(value));

        Assert.AreEqual(value.Real, result.Real, 1e-4f);
        Assert.AreEqual(value.Imaginary, result.Imaginary, 1e-4f);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}" /> over <see cref="float" /> reports the expected magnitude.
    /// </summary>
    [TestMethod]
    public void Float_Magnitude_ShouldReturnExpectedValue()
    {
        Assert.AreEqual(5.0f, new Complex<float>(3.0f, 4.0f).Magnitude, 1e-4f);
    }

    /// <summary>
    /// Verifies that <see cref="Complex{T}" /> over <see cref="System.Half" /> supports arithmetic and reports the
    /// expected magnitude at the type's reduced precision.
    /// </summary>
    [TestMethod]
    public void Half_ArithmeticAndMagnitude_ShouldBehaveConsistently()
    {
        var a = new Complex<Half>((Half)3.0f, (Half)4.0f);
        var b = new Complex<Half>((Half)1.0f, (Half)2.0f);

        Complex<Half> sum = a + b;

        Assert.AreEqual((Half)4.0f, sum.Real);
        Assert.AreEqual((Half)6.0f, sum.Imaginary);
        Assert.AreEqual(5.0f, (float)a.Magnitude, 1e-2f);
    }

    /// <summary>
    /// Verifies that a default-initialized value equals zero for a non-<see cref="double" /> component type.
    /// </summary>
    [TestMethod]
    public void Float_Default_ShouldEqualZero()
    {
        Assert.AreEqual(Complex<float>.Zero, default(Complex<float>));
    }
}
