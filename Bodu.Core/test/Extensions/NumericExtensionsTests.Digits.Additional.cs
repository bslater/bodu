// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.Digits.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ReverseDigits</c> on <see cref="ushort"/> reverses the decimal digits.
    /// </summary>
    [DataTestMethod]
    [DataRow((ushort)0, (ushort)0)]
    [DataRow((ushort)7, (ushort)7)]
    [DataRow((ushort)10, (ushort)1)]
    [DataRow((ushort)12345, (ushort)54321)]
    public void ReverseDigits_UShort_ShouldReturnExpected(ushort input, ushort expected) =>
        Assert.AreEqual(expected, input.ReverseDigits());

    /// <summary>
    /// Verifies that <c>ReverseDigits</c> on <see cref="uint"/> reverses the decimal digits.
    /// </summary>
    [DataTestMethod]
    [DataRow(0u, 0u)]
    [DataRow(7u, 7u)]
    [DataRow(10u, 1u)]
    [DataRow(12345u, 54321u)]
    [DataRow(1_234_567_890u, 987_654_321u)]
    public void ReverseDigits_UInt_ShouldReturnExpected(uint input, uint expected) =>
        Assert.AreEqual(expected, input.ReverseDigits());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> on <see cref="ushort"/> rotates the decimal digits.
    /// </summary>
    [DataTestMethod]
    [DataRow((ushort)12345, 0, (ushort)12345)]
    [DataRow((ushort)12345, 1, (ushort)23451)]
    [DataRow((ushort)12345, 5, (ushort)12345)]
    public void RotateDigitsLeft_UShort_ShouldReturnExpected(ushort input, int count, ushort expected) =>
        Assert.AreEqual(expected, input.RotateDigitsLeft(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> on <see cref="ushort"/> rotates the decimal digits.
    /// </summary>
    [DataTestMethod]
    [DataRow((ushort)12345, 0, (ushort)12345)]
    [DataRow((ushort)12345, 1, (ushort)51234)]
    [DataRow((ushort)12345, 5, (ushort)12345)]
    public void RotateDigitsRight_UShort_ShouldReturnExpected(ushort input, int count, ushort expected) =>
        Assert.AreEqual(expected, input.RotateDigitsRight(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> on <see cref="uint"/> rotates the decimal digits.
    /// </summary>
    [DataTestMethod]
    [DataRow(12345u, 0, 12345u)]
    [DataRow(12345u, 1, 23451u)]
    [DataRow(12345u, 2, 34512u)]
    [DataRow(12345u, 5, 12345u)]
    public void RotateDigitsLeft_UInt_ShouldReturnExpected(uint input, int count, uint expected) =>
        Assert.AreEqual(expected, input.RotateDigitsLeft(count));

    /// <summary>
    /// Verifies that <c>RotateDigitsRight</c> on <see cref="uint"/> rotates the decimal digits.
    /// </summary>
    [DataTestMethod]
    [DataRow(12345u, 0, 12345u)]
    [DataRow(12345u, 1, 51234u)]
    [DataRow(12345u, 2, 45123u)]
    [DataRow(12345u, 5, 12345u)]
    public void RotateDigitsRight_UInt_ShouldReturnExpected(uint input, int count, uint expected) =>
        Assert.AreEqual(expected, input.RotateDigitsRight(count));

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
    /// Verifies that applying <c>ReverseDigits</c> twice to a value without trailing zeros returns the original.
    /// </summary>
    [DataTestMethod]
    [DataRow(7UL)]
    [DataRow(123UL)]
    [DataRow(98765UL)]
    [DataRow(1_234_567_891UL)]
    public void ReverseDigits_ULong_WhenAppliedTwiceAndNoTrailingZeros_ShouldReturnOriginal(ulong value) =>
        Assert.AreEqual(value, value.ReverseDigits().ReverseDigits());

    /// <summary>
    /// Verifies that <c>RotateDigitsLeft</c> and <c>RotateDigitsRight</c> are inverse operations.
    /// </summary>
    [DataTestMethod]
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
    [DataTestMethod]
    [DataRow(12345UL, 5)]
    [DataRow(7UL, 1)]
    [DataRow(987_654_321UL, 9)]
    public void RotateDigits_ULong_WhenCountEqualsLength_ShouldReturnOriginal(ulong value, int length)
    {
        Assert.AreEqual(value, value.RotateDigitsLeft(length));
        Assert.AreEqual(value, value.RotateDigitsRight(length));
    }
}
