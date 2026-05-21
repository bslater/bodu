// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.GreatestCommonDivisor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{

    /// <summary>
    /// Provides a data source for verifying that every array overload of <c>GreatestCommonDivisor</c>
    /// throws <see cref="ArgumentException"/> when the array is empty.
    /// </summary>
    public static IEnumerable<object[]> GcdArrayEmptyActions =>
    [
        [(Action)(() => { _ = Array.Empty<short>().GreatestCommonDivisor(); })],
        [(Action)(() => { _ = Array.Empty<long>().GreatestCommonDivisor(); })],
        [(Action)(() => { _ = Array.Empty<ushort>().GreatestCommonDivisor(); })],
        [(Action)(() => { _ = Array.Empty<uint>().GreatestCommonDivisor(); })],
        [(Action)(() => { _ = Array.Empty<ulong>().GreatestCommonDivisor(); })],
    ];
    /// <summary>
    /// Provides a data source for verifying that every array overload of <c>GreatestCommonDivisor</c>
    /// throws <see cref="ArgumentNullException"/> when the array is <see langword="null"/>.
    /// </summary>
    public static IEnumerable<object[]> GcdArrayNullActions =>
    [
        [(Action)(() => { _ = ((short[])null!).GreatestCommonDivisor(); })],
        [(Action)(() => { _ = ((long[])null!).GreatestCommonDivisor(); })],
        [(Action)(() => { _ = ((ushort[])null!).GreatestCommonDivisor(); })],
        [(Action)(() => { _ = ((uint[])null!).GreatestCommonDivisor(); })],
        [(Action)(() => { _ = ((ulong[])null!).GreatestCommonDivisor(); })],
    ];

    /// <summary>
    /// Verifies that every array overload of <c>GreatestCommonDivisor</c> rejects an empty array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GcdArrayEmptyActions))]
    public void GreatestCommonDivisor_AllArrayOverloads_WhenArrayIsEmpty_ShouldThrowArgumentException(Action action) =>
        Assert.ThrowsExactly<ArgumentException>(action);

    /// <summary>
    /// Verifies that every array overload of <c>GreatestCommonDivisor</c> rejects a <see langword="null"/> array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GcdArrayNullActions))]
    public void GreatestCommonDivisor_AllArrayOverloads_WhenArrayIsNull_ShouldThrowArgumentNullException(Action action) =>
        Assert.ThrowsExactly<ArgumentNullException>(action);

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> is associative for <see cref="int"/> inputs.
    /// </summary>
    [TestMethod]
    [DataRow(12, 18, 24)]
    [DataRow(7, 11, 13)]
    [DataRow(48, 18, 36)]
    [DataRow(100, 75, 50)]
    public void GreatestCommonDivisor_Int_ShouldBeAssociative(int a, int b, int c)
    {
        var leftGroup = a.GreatestCommonDivisor(b).GreatestCommonDivisor(c);
        var rightGroup = a.GreatestCommonDivisor(b.GreatestCommonDivisor(c));
        Assert.AreEqual(leftGroup, rightGroup);
    }

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> is commutative for <see cref="int"/> inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(12, 18)]
    [DataRow(7, 13)]
    [DataRow(48, 18)]
    public void GreatestCommonDivisor_Int_ShouldBeCommutative(int a, int b) =>
        Assert.AreEqual(a.GreatestCommonDivisor(b), b.GreatestCommonDivisor(a));

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
    /// Verifies that <c>GreatestCommonDivisor(value, value)</c> equals <c>value</c>.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(12)]
    [DataRow(1_000_000_007)]
    public void GreatestCommonDivisor_Int_WhenInputsAreEqual_ShouldReturnInput(int value) =>
        Assert.AreEqual(value, value.GreatestCommonDivisor(value));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> returns the correct GCD for representative pairs of <see cref="int"/>.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(12, 0, 12)]
    [DataRow(0, 18, 18)]
    [DataRow(12, 18, 6)]
    [DataRow(15, 25, 5)]
    [DataRow(7, 13, 1)]
    [DataRow(48, 18, 6)]
    public void GreatestCommonDivisor_Int_WhenInputsAreNonNegative_ShouldReturnExpectedGcd(int a, int b, int expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> throws <see cref="ArgumentOutOfRangeException"/> for negative left-hand inputs.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_Int_WhenLeftIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = (-1).GreatestCommonDivisor(10);
        });

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor(value, 1)</c> always returns <c>1</c>.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(48)]
    [DataRow(1_000_000_007)]
    public void GreatestCommonDivisor_Int_WhenOtherIsOne_ShouldReturnOne(int value) =>
        Assert.AreEqual(1, value.GreatestCommonDivisor(1));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> throws <see cref="ArgumentOutOfRangeException"/> for negative right-hand inputs.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_Int_WhenRightIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 10.GreatestCommonDivisor(-1);
        });

    /// <summary>
    /// Verifies that the <see cref="int"/> array overload throws when an element is negative.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenAnyValueIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new int[] { 6, -2, 8 }.GreatestCommonDivisor();
        });

    /// <summary>
    /// Verifies that the <see cref="int"/> array overload throws <see cref="ArgumentException"/> when empty.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenArrayIsEmpty_ShouldThrowArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Array.Empty<int>().GreatestCommonDivisor();
        });

    /// <summary>
    /// Verifies that the <see cref="int"/> array overload throws <see cref="ArgumentNullException"/> when null.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        int[]? values = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = values!.GreatestCommonDivisor();
        });
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
    /// Verifies that the <see cref="int"/> array overload returns the GCD of every element.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_IntArray_WhenInputsAreNonNegative_ShouldReturnExpectedGcd() =>
        Assert.AreEqual(12, new int[] { 24, 36, 60 }.GreatestCommonDivisor());

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> is commutative for <see cref="long"/> inputs.
    /// </summary>
    [TestMethod]
    [DataRow(12L, 18L)]
    [DataRow(1_000_000_000L, 750_000_000L)]
    public void GreatestCommonDivisor_Long_ShouldBeCommutative(long a, long b) =>
        Assert.AreEqual(a.GreatestCommonDivisor(b), b.GreatestCommonDivisor(a));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="long"/> returns the expected value for
    /// non-negative inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0L, 0L, 0L)]
    [DataRow(12L, 18L, 6L)]
    [DataRow(1_000_000_000L, 750_000_000L, 250_000_000L)]
    [DataRow(7L, 13L, 1L)]
    public void GreatestCommonDivisor_Long_ShouldReturnExpected(long a, long b, long expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="long"/> throws when an argument is negative.
    /// </summary>
    [TestMethod]
    [DataRow(-1L, 10L)]
    [DataRow(10L, -1L)]
    [DataRow(long.MinValue, 10L)]
    public void GreatestCommonDivisor_Long_WhenNegative_ShouldThrowArgumentOutOfRangeException(long a, long b) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = a.GreatestCommonDivisor(b);
        });

    /// <summary>
    /// Verifies that the <see cref="long"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_LongArray_ShouldReturnExpected() =>
        Assert.AreEqual(250_000_000L, new long[] { 1_000_000_000L, 750_000_000L, 500_000_000L }.GreatestCommonDivisor());

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
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="short"/> returns the expected value for
    /// non-negative inputs.
    /// </summary>
    [TestMethod]
    [DataRow((short)0, (short)0, (short)0)]
    [DataRow((short)12, (short)18, (short)6)]
    [DataRow((short)100, (short)75, (short)25)]
    [DataRow((short)7, (short)13, (short)1)]
    public void GreatestCommonDivisor_Short_ShouldReturnExpected(short a, short b, short expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="short"/> throws when an argument is negative.
    /// </summary>
    [TestMethod]
    [DataRow((short)-1, (short)10)]
    [DataRow((short)10, (short)-1)]
    [DataRow(short.MinValue, (short)10)]
    public void GreatestCommonDivisor_Short_WhenNegative_ShouldThrowArgumentOutOfRangeException(short a, short b) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = a.GreatestCommonDivisor(b);
        });

    /// <summary>
    /// Verifies that the <see cref="short"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_ShortArray_ShouldReturnExpected() =>
        Assert.AreEqual((short)12, new short[] { 24, 36, 60 }.GreatestCommonDivisor());

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
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="uint"/> returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow(0u, 0u, 0u)]
    [DataRow(48u, 18u, 6u)]
    [DataRow(1_000_000_000u, 750_000_000u, 250_000_000u)]
    public void GreatestCommonDivisor_UInt_ShouldReturnExpected(uint a, uint b, uint expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that the <see cref="uint"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_UIntArray_ShouldReturnExpected() =>
        Assert.AreEqual(12u, new uint[] { 48u, 36u, 60u }.GreatestCommonDivisor());

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="ulong"/> returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow(0ul, 0ul, 0ul)]
    [DataRow(48ul, 18ul, 6ul)]
    [DataRow(1_000_000_007ul, 1_000_000_009ul, 1ul)]
    public void GreatestCommonDivisor_ULong_ShouldReturnExpected(ulong a, ulong b, ulong expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="ulong"/> works for very large coprime values.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_ULong_WhenCoprime_ShouldReturnOne() =>
        Assert.AreEqual(1UL, 1_000_000_007UL.GreatestCommonDivisor(1_000_000_009UL));

    /// <summary>
    /// Verifies that the <see cref="ulong"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_ULongArray_ShouldReturnExpected() =>
        Assert.AreEqual(12ul, new ulong[] { 48ul, 36ul, 60ul }.GreatestCommonDivisor());

    /// <summary>
    /// Verifies that <c>GreatestCommonDivisor</c> for <see cref="ushort"/> returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0, (ushort)0, (ushort)0)]
    [DataRow((ushort)12, (ushort)18, (ushort)6)]
    [DataRow((ushort)1024, (ushort)512, (ushort)512)]
    public void GreatestCommonDivisor_UShort_ShouldReturnExpected(ushort a, ushort b, ushort expected) =>
        Assert.AreEqual(expected, a.GreatestCommonDivisor(b));

    /// <summary>
    /// Verifies that the <see cref="ushort"/> array overload returns the expected GCD.
    /// </summary>
    [TestMethod]
    public void GreatestCommonDivisor_UShortArray_ShouldReturnExpected() =>
        Assert.AreEqual((ushort)12, new ushort[] { 24, 36, 60 }.GreatestCommonDivisor());

}
