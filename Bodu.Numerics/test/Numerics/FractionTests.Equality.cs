// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Equality.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that fractions reduced from different components compare equal and hash equally.
    /// </summary>
    [TestMethod]
    public void Equals_WhenValuesReduceToSameCanonicalForm_ShouldBeEqualAndShareHashCode()
    {
        var a = new Fraction<int>(1, 2);
        var b = new Fraction<int>(50, 100);

        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that the object overload of <c>Equals</c> recognizes a boxed fraction of the same value.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedWithBoxedFraction_ShouldReturnTrue()
    {
        object boxed = new Fraction<int>(3, 4);

        Assert.IsTrue(new Fraction<int>(3, 4).Equals(boxed));
    }

    /// <summary>
    /// Verifies that the object overload of <c>Equals</c> rejects <see langword="null" /> and foreign types.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparedWithNullOrForeignType_ShouldReturnFalse()
    {
        var value = new Fraction<int>(3, 4);

        Assert.IsFalse(value.Equals(null));
        Assert.IsFalse(value.Equals("3/4"));
    }

    /// <summary>
    /// Verifies that a default-initialized fraction is equal to zero.
    /// </summary>
    [TestMethod]
    public void Equals_WhenComparingDefaultWithZero_ShouldReturnTrue() => Assert.IsTrue(default(Fraction<int>).Equals(Fraction<int>.Zero));

    /// <summary>
    /// Verifies that unequal fractions are reported as unequal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenValuesDiffer_ShouldReturnFalse() => Assert.IsFalse(new Fraction<int>(1, 2).Equals(new Fraction<int>(1, 3)));
}
