// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.Digits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ReverseDigits</c> reverses the decimal digits of a <see cref="ulong"/>.
    /// </summary>
    [DataTestMethod]
    [DataRow(0UL, 0UL)]
    [DataRow(1UL, 1UL)]
    [DataRow(10UL, 1UL)]
    [DataRow(12345UL, 54321UL)]
    [DataRow(120UL, 21UL)]
    public void ReverseDigits_ULong_ShouldReturnExpected(ulong input, ulong expected) =>
        Assert.AreEqual(expected, input.ReverseDigits());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> with default count rotates one position to the left.
    /// </summary>
    [TestMethod]
    public void RotateDigitsLeft_ULong_DefaultCount_ShouldRotateOneLeft() =>
        Assert.AreEqual(23451UL, 12345UL.RotateDigitsLeft());

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> with default count rotates one position to the right.
    /// </summary>
    [TestMethod]
    public void RotateDigitsRight_ULong_DefaultCount_ShouldRotateOneRight() =>
        Assert.AreEqual(51234UL, 12345UL.RotateDigitsRight());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> with an explicit count rotates by that amount.
    /// </summary>
    [DataTestMethod]
    [DataRow(12345UL, 0, 12345UL)]
    [DataRow(12345UL, 1, 23451UL)]
    [DataRow(12345UL, 2, 34512UL)]
    [DataRow(12345UL, 5, 12345UL)]
    [DataRow(12345UL, 6, 23451UL)]
    public void RotateDigitsLeft_ULong_WithCount_ShouldReturnExpected(ulong input, int count, ulong expected) =>
        Assert.AreEqual(expected, input.RotateDigitsLeft(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> with an explicit count rotates by that amount.
    /// </summary>
    [DataTestMethod]
    [DataRow(12345UL, 0, 12345UL)]
    [DataRow(12345UL, 1, 51234UL)]
    [DataRow(12345UL, 2, 45123UL)]
    [DataRow(12345UL, 5, 12345UL)]
    public void RotateDigitsRight_ULong_WithCount_ShouldReturnExpected(ulong input, int count, ulong expected) =>
        Assert.AreEqual(expected, input.RotateDigitsRight(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> throws when the count is negative.
    /// </summary>
    [TestMethod]
    public void RotateDigitsLeft_ULong_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 12345UL.RotateDigitsLeft(-1);
        });
    }

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> throws when the count is negative.
    /// </summary>
    [TestMethod]
    public void RotateDigitsRight_ULong_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 12345UL.RotateDigitsRight(-1);
        });
    }

    /// <summary>
    /// Verifies that <c>RotateDigits*</c> on zero or single-digit values returns the value unchanged.
    /// </summary>
    [TestMethod]
    public void RotateDigits_ULong_WhenValueHasFewerThanTwoDigits_ShouldReturnUnchanged()
    {
        Assert.AreEqual(0UL, 0UL.RotateDigitsLeft(3));
        Assert.AreEqual(0UL, 0UL.RotateDigitsRight(3));
        Assert.AreEqual(7UL, 7UL.RotateDigitsLeft(3));
        Assert.AreEqual(7UL, 7UL.RotateDigitsRight(3));
    }

    /// <summary>
    /// Verifies that <c>ToDigitArray</c> returns the digits of a value in most-significant-first order.
    /// </summary>
    [TestMethod]
    public void ToDigitArray_ULong_ShouldReturnDigitsMostSignificantFirst()
    {
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, 12345UL.ToDigitArray());
        CollectionAssert.AreEqual(new byte[] { 0 }, 0UL.ToDigitArray());
        CollectionAssert.AreEqual(new byte[] { 7 }, 7UL.ToDigitArray());
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0 }, 100UL.ToDigitArray());
    }

    /// <summary>
    /// Verifies that <c>ToDigitArray</c> on a <see cref="uint"/> produces the same result as on <see cref="ulong"/>.
    /// </summary>
    [TestMethod]
    public void ToDigitArray_UInt_ShouldReturnDigitsMostSignificantFirst() =>
        CollectionAssert.AreEqual(new byte[] { 4, 2, 9, 4, 9, 6, 7, 2, 9, 5 }, uint.MaxValue.ToDigitArray());
}
