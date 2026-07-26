// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.DefaultValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that a default-initialized <see cref="Complex{T}" /> equals <see cref="Complex{T}.Zero" />.
    /// </summary>
    [TestMethod]
    public void Default_ShouldEqualZero()
    {
        Complex<double> value = default;

        Assert.AreEqual(Complex<double>.Zero, value);
        Assert.AreEqual(0.0, value.Real);
        Assert.AreEqual(0.0, value.Imaginary);
    }

    /// <summary>
    /// Verifies that a default-initialized value behaves as the additive identity.
    /// </summary>
    [TestMethod]
    public void Default_WhenAddedToValue_ShouldLeaveValueUnchanged()
    {
        var value = new Complex<double>(3.0, -2.0);

        Assert.AreEqual(value, value + default(Complex<double>));
    }
}
