// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.GreatestCommonDivisor.MoreCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="short"/> throws when an argument is negative.
    /// </summary>
    [DataTestMethod]
    [DataRow((short)-1, (short)10)]
    [DataRow((short)10, (short)-1)]
    [DataRow(short.MinValue, (short)10)]
    public void GreatestCommonDivisor_Short_WhenNegative_ShouldThrowArgumentOutOfRangeException(short a, short b) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = a.GreatestCommonDivisor(b);
        });

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="long"/> throws when an argument is negative.
    /// </summary>
    [DataTestMethod]
    [DataRow(-1L, 10L)]
    [DataRow(10L, -1L)]
    [DataRow(long.MinValue, 10L)]
    public void GreatestCommonDivisor_Long_WhenNegative_ShouldThrowArgumentOutOfRangeException(long a, long b) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = a.GreatestCommonDivisor(b);
        });

    /// <summary>
    /// Provides a data source for verifying that every array overload of <c>GreatestCommonDivisor</c>
    /// throws <see cref="ArgumentNullException"/> when the array is <see langword="null"/>.
    /// </summary>
    public static IEnumerable<object[]> GcdArrayNullActions =>
    [
        new object[] { (Action)(() => { _ = ((short[])null!).GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = ((long[])null!).GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = ((ushort[])null!).GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = ((uint[])null!).GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = ((ulong[])null!).GreatestCommonDivisor(); }) },
    ];

    /// <summary>
    /// Verifies that every array overload of <c>GreatestCommonDivisor</c> rejects a <see langword="null"/> array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GcdArrayNullActions))]
    public void GreatestCommonDivisor_AllArrayOverloads_WhenArrayIsNull_ShouldThrowArgumentNullException(Action action) =>
        Assert.ThrowsExactly<ArgumentNullException>(action);

    /// <summary>
    /// Provides a data source for verifying that every array overload of <c>GreatestCommonDivisor</c>
    /// throws <see cref="ArgumentException"/> when the array is empty.
    /// </summary>
    public static IEnumerable<object[]> GcdArrayEmptyActions =>
    [
        new object[] { (Action)(() => { _ = Array.Empty<short>().GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<long>().GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<ushort>().GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<uint>().GreatestCommonDivisor(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<ulong>().GreatestCommonDivisor(); }) },
    ];

    /// <summary>
    /// Verifies that every array overload of <c>GreatestCommonDivisor</c> rejects an empty array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GcdArrayEmptyActions))]
    public void GreatestCommonDivisor_AllArrayOverloads_WhenArrayIsEmpty_ShouldThrowArgumentException(Action action) =>
        Assert.ThrowsExactly<ArgumentException>(action);

    /// <summary>
    /// Verifies that the <see cref="short"/> array overload rejects a negative element.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_ShortArray_WhenAnyValueIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new short[] { 6, -2, 8 }.GreatestCommonDivisor();
        });

    /// <summary>
    /// Verifies that the <see cref="long"/> array overload rejects a negative element.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_LongArray_WhenAnyValueIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new long[] { 6L, -2L, 8L }.GreatestCommonDivisor();
        });

    /// <summary>
    /// Verifies that a single-element array returns the element itself for every overload.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_SingleElementArray_ShouldReturnElement()
    {
        Assert.AreEqual((short)42, new short[] { 42 }.GreatestCommonDivisor());
        Assert.AreEqual(42, new int[] { 42 }.GreatestCommonDivisor());
        Assert.AreEqual(42L, new long[] { 42L }.GreatestCommonDivisor());
        Assert.AreEqual((ushort)42, new ushort[] { 42 }.GreatestCommonDivisor());
        Assert.AreEqual(42u, new uint[] { 42u }.GreatestCommonDivisor());
        Assert.AreEqual(42ul, new ulong[] { 42ul }.GreatestCommonDivisor());
    }

    /// <summary>
    /// Verifies that an array containing a zero element returns the GCD of the remaining elements
    /// (mathematically, <c>gcd(0, n) = n</c>).
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenContainsZero_ShouldReturnGcdOfNonZeroElements()
    {
        Assert.AreEqual(6, new int[] { 0, 12, 18 }.GreatestCommonDivisor());
        Assert.AreEqual(6, new int[] { 12, 0, 18 }.GreatestCommonDivisor());
        Assert.AreEqual(0, new int[] { 0, 0, 0 }.GreatestCommonDivisor());
    }

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> on <see cref="int"/> returns the value itself for
    /// boundary maximum inputs.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_Int_WhenBothAreMaxValue_ShouldReturnMaxValue() =>
        Assert.AreEqual(int.MaxValue, int.MaxValue.GreatestCommonDivisor(int.MaxValue));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> on consecutive <see cref="int"/> max-range inputs returns <c>1</c>.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_Int_WhenInputsAreConsecutiveMaxRange_ShouldReturnOne() =>
        Assert.AreEqual(1, int.MaxValue.GreatestCommonDivisor(int.MaxValue - 1));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> is associative for <see cref="int"/> inputs.
    /// </summary>
    [DataTestMethod]
    [DataRow(12, 18, 24)]
    [DataRow(7, 11, 13)]
    [DataRow(48, 18, 36)]
    [DataRow(100, 75, 50)]
    public void GreatestCommonDivisor_Int_ShouldBeAssociative(int a, int b, int c)
    {
        int leftGroup = a.GreatestCommonDivisor(b).GreatestCommonDivisor(c);
        int rightGroup = a.GreatestCommonDivisor(b.GreatestCommonDivisor(c));
        Assert.AreEqual(leftGroup, rightGroup);
    }
}
