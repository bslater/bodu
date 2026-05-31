// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.UnsignedBackingType.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that a fraction backed by an unsigned integer type reduces non-negative components.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenBackedByUnsignedType_ShouldNormalizeNonNegativeComponents()
    {
        Fraction<uint> value = new Fraction<uint>(6, 8);

        Assert.AreEqual(3u, value.Numerator);
        Assert.AreEqual(4u, value.Denominator);
    }

    /// <summary>
    /// Verifies that the zero and one constants are available for an unsigned backing type.
    /// </summary>
    [TestMethod]
    public void ZeroAndOne_WhenBackedByUnsignedType_ShouldBeAvailable()
    {
        Assert.IsTrue(Fraction<byte>.Zero.IsZero);
        Assert.IsTrue(Fraction<byte>.One.IsInteger);
        Assert.AreEqual(1u, Fraction<uint>.One.Numerator);
    }

    /// <summary>
    /// Verifies that requesting negative one for an unsigned backing type throws
    /// <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void MinusOne_WhenBackedByUnsignedType_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = Fraction<uint>.MinusOne;
        });
    }

    /// <summary>
    /// Verifies that negating a non-zero unsigned-backed fraction throws <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Negate_WhenBackedByUnsignedTypeAndNonZero_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<uint>(3, 4).Negate();
        });
    }

    /// <summary>
    /// Verifies that negating zero is permitted even for an unsigned backing type.
    /// </summary>
    [TestMethod]
    public void Negate_WhenBackedByUnsignedTypeAndZero_ShouldReturnZero()
    {
        Assert.AreEqual(Fraction<uint>.Zero, Fraction<uint>.Zero.Negate());
    }

    /// <summary>
    /// Verifies that subtraction producing a negative result throws <see cref="OverflowException" /> for an
    /// unsigned backing type.
    /// </summary>
    [TestMethod]
    public void Subtraction_WhenBackedByUnsignedTypeAndResultIsNegative_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<uint>(1, 4) - new Fraction<uint>(1, 2);
        });
    }

    /// <summary>
    /// Verifies that an unsigned-backed fraction is never reported as negative.
    /// </summary>
    [TestMethod]
    public void IsNegative_WhenBackedByUnsignedType_ShouldAlwaysBeFalse()
    {
        Assert.IsFalse(new Fraction<uint>(3, 4).IsNegative);
        Assert.IsFalse(Fraction<uint>.Zero.IsNegative);
    }

    /// <summary>
    /// Verifies that arithmetic overflowing an unsigned fixed-width component throws
    /// <see cref="OverflowException" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenBackedByByteAndResultExceedsRange_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = new Fraction<byte>(200) + new Fraction<byte>(100);
        });
    }
}
