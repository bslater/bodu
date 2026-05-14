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
    /// Verifies that <c>ReverseDigits</c> on <see cref="ushort"/> reverses the decimal digits.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0, (ushort)0)]
    [DataRow((ushort)7, (ushort)7)]
    [DataRow((ushort)10, (ushort)1)]
    [DataRow((ushort)12345, (ushort)54321)]
    public void ReverseDigits_UShort_ShouldReturnExpected(ushort input, ushort expected) =>
        Assert.AreEqual(expected, input.ReverseDigits());

    /// <summary>
    /// Verifies that <c>ReverseDigits</c> on <see cref="uint"/> reverses the decimal digits.
    /// </summary>
    [TestMethod]
    [DataRow(0u, 0u)]
    [DataRow(7u, 7u)]
    [DataRow(10u, 1u)]
    [DataRow(12345u, 54321u)]
    [DataRow(1_234_567_890u, 987_654_321u)]
    public void ReverseDigits_UInt_ShouldReturnExpected(uint input, uint expected) =>
        Assert.AreEqual(expected, input.ReverseDigits());

    /// <summary>
    /// Verifies that <c>ReverseDigits</c> reverses the decimal digits of a <see cref="ulong"/>.
    /// </summary>
    [TestMethod]
    [DataRow(0UL, 0UL)]
    [DataRow(1UL, 1UL)]
    [DataRow(10UL, 1UL)]
    [DataRow(12345UL, 54321UL)]
    [DataRow(120UL, 21UL)]
    public void ReverseDigits_ULong_ShouldReturnExpected(ulong input, ulong expected) =>
        Assert.AreEqual(expected, input.ReverseDigits());

    /// <summary>
    /// Verifies that applying <c>ReverseDigits</c> twice to a value without trailing zeros returns the original.
    /// </summary>
    [TestMethod]
    [DataRow(7UL)]
    [DataRow(123UL)]
    [DataRow(98765UL)]
    [DataRow(1_234_567_891UL)]
    public void ReverseDigits_ULong_WhenAppliedTwiceAndNoTrailingZeros_ShouldReturnOriginal(ulong value) =>
        Assert.AreEqual(value, value.ReverseDigits().ReverseDigits());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> on <see cref="ushort"/> with default count rotates one position to the left.
    /// </summary>
    [TestMethod]
    public void RotateDigitsLeft_UShort_DefaultCount_ShouldRotateOneLeft() =>
        Assert.AreEqual((ushort)23451, ((ushort)12345).RotateDigitsLeft());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> on <see cref="ushort"/> rotates the decimal digits.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)12345, 0, (ushort)12345)]
    [DataRow((ushort)12345, 1, (ushort)23451)]
    [DataRow((ushort)12345, 5, (ushort)12345)]
    public void RotateDigitsLeft_UShort_ShouldReturnExpected(ushort input, int count, ushort expected) =>
        Assert.AreEqual(expected, input.RotateDigitsLeft(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> on <see cref="uint"/> with default count rotates one position to the left.
    /// </summary>
    [TestMethod]
    public void RotateDigitsLeft_UInt_DefaultCount_ShouldRotateOneLeft() =>
        Assert.AreEqual(23451u, 12345u.RotateDigitsLeft());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> on <see cref="uint"/> rotates the decimal digits.
    /// </summary>
    [TestMethod]
    [DataRow(12345u, 0, 12345u)]
    [DataRow(12345u, 1, 23451u)]
    [DataRow(12345u, 2, 34512u)]
    [DataRow(12345u, 5, 12345u)]
    public void RotateDigitsLeft_UInt_ShouldReturnExpected(uint input, int count, uint expected) =>
        Assert.AreEqual(expected, input.RotateDigitsLeft(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> with default count rotates one position to the left.
    /// </summary>
    [TestMethod]
    public void RotateDigitsLeft_ULong_DefaultCount_ShouldRotateOneLeft() =>
        Assert.AreEqual(23451UL, 12345UL.RotateDigitsLeft());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> with an explicit count rotates by that amount.
    /// </summary>
    [TestMethod]
    [DataRow(12345UL, 0, 12345UL)]
    [DataRow(12345UL, 1, 23451UL)]
    [DataRow(12345UL, 2, 34512UL)]
    [DataRow(12345UL, 5, 12345UL)]
    [DataRow(12345UL, 6, 23451UL)]
    public void RotateDigitsLeft_ULong_WithCount_ShouldReturnExpected(ulong input, int count, ulong expected) =>
        Assert.AreEqual(expected, input.RotateDigitsLeft(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> throws when the count is negative.
    /// </summary>
    [TestMethod]
    public void RotateDigitsLeft_ULong_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 12345UL.RotateDigitsLeft(-1);
        });

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> on <see cref="ushort"/> with default count rotates one position to the right.
    /// </summary>
    [TestMethod]
    public void RotateDigitsRight_UShort_DefaultCount_ShouldRotateOneRight() =>
        Assert.AreEqual((ushort)51234, ((ushort)12345).RotateDigitsRight());

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> on <see cref="ushort"/> rotates the decimal digits.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)12345, 0, (ushort)12345)]
    [DataRow((ushort)12345, 1, (ushort)51234)]
    [DataRow((ushort)12345, 5, (ushort)12345)]
    public void RotateDigitsRight_UShort_ShouldReturnExpected(ushort input, int count, ushort expected) =>
        Assert.AreEqual(expected, input.RotateDigitsRight(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> on <see cref="uint"/> with default count rotates one position to the right.
    /// </summary>
    [TestMethod]
    public void RotateDigitsRight_UInt_DefaultCount_ShouldRotateOneRight() =>
        Assert.AreEqual(51234u, 12345u.RotateDigitsRight());

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> on <see cref="uint"/> rotates the decimal digits.
    /// </summary>
    [TestMethod]
    [DataRow(12345u, 0, 12345u)]
    [DataRow(12345u, 1, 51234u)]
    [DataRow(12345u, 2, 45123u)]
    [DataRow(12345u, 5, 12345u)]
    public void RotateDigitsRight_UInt_ShouldReturnExpected(uint input, int count, uint expected) =>
        Assert.AreEqual(expected, input.RotateDigitsRight(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> with default count rotates one position to the right.
    /// </summary>
    [TestMethod]
    public void RotateDigitsRight_ULong_DefaultCount_ShouldRotateOneRight() =>
        Assert.AreEqual(51234UL, 12345UL.RotateDigitsRight());

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> with an explicit count rotates by that amount.
    /// </summary>
    [TestMethod]
    [DataRow(12345UL, 0, 12345UL)]
    [DataRow(12345UL, 1, 51234UL)]
    [DataRow(12345UL, 2, 45123UL)]
    [DataRow(12345UL, 5, 12345UL)]
    public void RotateDigitsRight_ULong_WithCount_ShouldReturnExpected(ulong input, int count, ulong expected) =>
        Assert.AreEqual(expected, input.RotateDigitsRight(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> throws when the count is negative.
    /// </summary>
    [TestMethod]
    public void RotateDigitsRight_ULong_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 12345UL.RotateDigitsRight(-1);
        });

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
    /// Verifies that <c>RotateDigitsLeft</c> and <c>RotateDigitsRight</c> are inverse operations.
    /// </summary>
    [TestMethod]
    [DataRow(12345UL, 0)]
    [DataRow(12345UL, 1)]
    [DataRow(12345UL, 3)]
    [DataRow(987_654_321UL, 4)]
    public void RotateDigits_ULong_LeftThenRight_ShouldReturnOriginal(ulong value, int count)
    {
        Assert.AreEqual(value, value.RotateDigitsLeft(count).RotateDigitsRight(count));
        Assert.AreEqual(value, value.RotateDigitsRight(count).RotateDigitsLeft(count));
    }

    /// <summary>
    /// Verifies that rotating a value by its digit length returns the value unchanged.
    /// </summary>
    [TestMethod]
    [DataRow(12345UL, 5)]
    [DataRow(7UL, 1)]
    [DataRow(987_654_321UL, 9)]
    public void RotateDigits_ULong_WhenCountEqualsLength_ShouldReturnOriginal(ulong value, int length)
    {
        Assert.AreEqual(value, value.RotateDigitsLeft(length));
        Assert.AreEqual(value, value.RotateDigitsRight(length));
    }

    /// <summary>
    /// Verifies that <c>ToDigitArray</c> on <see cref="ushort"/> returns the digits in most-significant-first order.
    /// </summary>
    [TestMethod]
    public void ToDigitArray_UShort_ShouldReturnDigitsMostSignificantFirst()
    {
        CollectionAssert.AreEqual(new byte[] { 0 }, ((ushort)0).ToDigitArray());
        CollectionAssert.AreEqual(new byte[] { 7 }, ((ushort)7).ToDigitArray());
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, ((ushort)12345).ToDigitArray());
        CollectionAssert.AreEqual(new byte[] { 6, 5, 5, 3, 5 }, ushort.MaxValue.ToDigitArray());
    }

    /// <summary>
    /// Verifies that <c>ToDigitArray</c> on a <see cref="uint"/> produces the same result as on <see cref="ulong"/>.
    /// </summary>
    [TestMethod]
    public void ToDigitArray_UInt_ShouldReturnDigitsMostSignificantFirst() =>
        CollectionAssert.AreEqual(new byte[] { 4, 2, 9, 4, 9, 6, 7, 2, 9, 5 }, uint.MaxValue.ToDigitArray());

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
}
