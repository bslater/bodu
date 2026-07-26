// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Operators.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the binary arithmetic operators produce the expected results.
    /// </summary>
    [TestMethod]
    public void ArithmeticOperators_WhenCombiningValues_ShouldProduceExpectedResults()
    {
        var a = new Complex<double>(1.0, 2.0);
        var b = new Complex<double>(3.0, 4.0);

        Assert.AreEqual(new Complex<double>(4.0, 6.0), a + b);
        Assert.AreEqual(new Complex<double>(-2.0, -2.0), a - b);
        Assert.AreEqual(new Complex<double>(-5.0, 10.0), a * b);
        Assert.AreEqual(new Complex<double>(0.44, 0.08), a / b);
    }

    /// <summary>
    /// Verifies that the unary negation operator negates both components.
    /// </summary>
    [TestMethod]
    public void UnaryNegationOperator_WhenApplied_ShouldNegateBothComponents()
    {
        Assert.AreEqual(new Complex<double>(-1.0, -2.0), -new Complex<double>(1.0, 2.0));
    }

    /// <summary>
    /// Verifies that the increment and decrement operators change the real component by one.
    /// </summary>
    [TestMethod]
    public void IncrementAndDecrementOperators_WhenApplied_ShouldChangeRealComponentByOne()
    {
        var value = new Complex<double>(1.0, 5.0);

        value++;
        Assert.AreEqual(new Complex<double>(2.0, 5.0), value);

        value--;
        Assert.AreEqual(new Complex<double>(1.0, 5.0), value);
    }

    /// <summary>
    /// Verifies that the unary plus operator returns the value unchanged.
    /// </summary>
    [TestMethod]
    public void UnaryPlusOperator_WhenApplied_ShouldReturnValueUnchanged()
    {
        var value = new Complex<double>(3.0, -7.0);

        Assert.AreEqual(value, +value);
    }
}
