// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

/// <summary>
/// Contains the unit tests for the <see cref="Fraction{T}" /> rational number type.
/// </summary>
[TestClass]
public partial class FractionTests
{
    /// <summary>
    /// Verifies that adding two fractions produces the expected canonical sum.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Smoke)]
    public void Add_WhenAddingTwoFractions_ShouldReturnCanonicalSum()
    {
        Fraction<int> result = new Fraction<int>(1, 2) + new Fraction<int>(1, 3);

        Assert.AreEqual(new Fraction<int>(5, 6), result);
    }
}
