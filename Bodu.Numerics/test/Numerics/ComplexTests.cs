// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Contains the unit tests for the <see cref="Complex{T}" /> complex number type.
/// </summary>
[TestClass]
public partial class ComplexTests
{
    /// <summary>
    /// Verifies that multiplying two complex values produces the expected product.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Smoke)]
    public void Multiply_WhenMultiplyingTwoValues_ShouldReturnExpectedProduct()
    {
        Complex<double> result = new Complex<double>(1.0, 2.0) * new Complex<double>(3.0, 4.0);

        Assert.AreEqual(new Complex<double>(-5.0, 10.0), result);
    }
}
