// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.IsPrime.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>IsPrime</c> returns <see langword="true"/> for known small primes and <see langword="false"/> otherwise.
    /// </summary>
    [DataTestMethod]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(5, true)]
    [DataRow(15, false)]
    [DataRow(17, true)]
    [DataRow(97, true)]
    [DataRow(100, false)]
    [DataRow(7919, true)]
    [DataRow(7921, false)]
    public void IsPrime_Int_WhenInput_ShouldReturnExpected(int value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> returns <see langword="false"/> for negative signed values.
    /// </summary>
    [TestMethod]
    public void IsPrime_Int_WhenNegative_ShouldReturnFalse()
    {
        Assert.IsFalse((-7).IsPrime());
        Assert.IsFalse(int.MinValue.IsPrime());
    }

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="ulong"/> returns <see langword="true"/> for a large known prime.
    /// </summary>
    [TestMethod]
    public void IsPrime_ULong_WhenLargeKnownPrime_ShouldReturnTrue() =>
        Assert.IsTrue(1_000_000_007UL.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="ulong"/> returns <see langword="false"/> for a large composite.
    /// </summary>
    [TestMethod]
    public void IsPrime_ULong_WhenLargeComposite_ShouldReturnFalse() =>
        Assert.IsFalse((1_000_000_007UL * 3UL).IsPrime());
}
