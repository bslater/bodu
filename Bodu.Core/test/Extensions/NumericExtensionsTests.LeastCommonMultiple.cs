// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.LeastCommonMultiple.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Provides a data source for verifying that every array overload of <c>LeastCommonMultiple</c>
    /// throws <see cref="ArgumentNullException"/> when the array is <see langword="null"/>.
    /// </summary>
    public static IEnumerable<object[]> LcmArrayNullActions =>
    [
        new object[] { (Action)(() => { _ = ((short[])null!).LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = ((long[])null!).LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = ((ushort[])null!).LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = ((uint[])null!).LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = ((ulong[])null!).LeastCommonMultiple(); }) },
    ];

    /// <summary>
    /// Provides a data source for verifying that every array overload of <c>LeastCommonMultiple</c>
    /// throws <see cref="ArgumentException"/> when the array is empty.
    /// </summary>
    public static IEnumerable<object[]> LcmArrayEmptyActions =>
    [
        new object[] { (Action)(() => { _ = Array.Empty<short>().LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<long>().LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<ushort>().LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<uint>().LeastCommonMultiple(); }) },
        new object[] { (Action)(() => { _ = Array.Empty<ulong>().LeastCommonMultiple(); }) },
    ];

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="short"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow((short)0, (short)0, (short)0)]
    [DataRow((short)4, (short)6, (short)12)]
    [DataRow((short)21, (short)6, (short)42)]
    [DataRow((short)7, (short)13, (short)91)]
    public void LeastCommonMultiple_Short_ShouldReturnExpected(short a, short b, short expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="short"/> throws when an argument is negative.
    /// </summary>
    [DataTestMethod]
    [DataRow((short)-1, (short)10)]
    [DataRow((short)10, (short)-1)]
    public void LeastCommonMultiple_Short_WhenNegative_ShouldThrowArgumentOutOfRangeException(short a, short b) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = a.LeastCommonMultiple(b);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> on <see cref="short"/> throws when the result exceeds <see cref="short.MaxValue"/>.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Short_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = ((short)1000).LeastCommonMultiple((short)1001);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> returns the correct LCM for representative pairs of <see cref="int"/>.
    /// </summary>
    [DataTestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(12, 0, 0)]
    [DataRow(0, 18, 0)]
    [DataRow(4, 6, 12)]
    [DataRow(21, 6, 42)]
    [DataRow(7, 13, 91)]
    [DataRow(8, 12, 24)]
    public void LeastCommonMultiple_Int_WhenInputsAreNonNegative_ShouldReturnExpectedLcm(int a, int b, int expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> throws <see cref="ArgumentOutOfRangeException"/> for negative left-hand inputs.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Int_WhenLeftIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = (-1).LeastCommonMultiple(10);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> throws <see cref="ArgumentOutOfRangeException"/> for negative right-hand inputs.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Int_WhenRightIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = 10.LeastCommonMultiple(-1);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> throws <see cref="OverflowException"/> when the result does
    /// not fit in <see cref="int"/>.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Int_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = int.MaxValue.LeastCommonMultiple(int.MaxValue - 1);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="long"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0L, 0L, 0L)]
    [DataRow(4L, 6L, 12L)]
    [DataRow(21L, 6L, 42L)]
    [DataRow(1_000_000L, 1_500_000L, 3_000_000L)]
    public void LeastCommonMultiple_Long_ShouldReturnExpected(long a, long b, long expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="long"/> throws when an argument is negative.
    /// </summary>
    [DataTestMethod]
    [DataRow(-1L, 10L)]
    [DataRow(10L, -1L)]
    public void LeastCommonMultiple_Long_WhenNegative_ShouldThrowArgumentOutOfRangeException(long a, long b) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = a.LeastCommonMultiple(b);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> on <see cref="long"/> throws when the result exceeds <see cref="long.MaxValue"/>.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Long_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = long.MaxValue.LeastCommonMultiple(long.MaxValue - 1);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="ushort"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow((ushort)0, (ushort)0, (ushort)0)]
    [DataRow((ushort)4, (ushort)6, (ushort)12)]
    [DataRow((ushort)21, (ushort)6, (ushort)42)]
    public void LeastCommonMultiple_UShort_ShouldReturnExpected(ushort a, ushort b, ushort expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> on <see cref="ushort"/> throws when the result exceeds <see cref="ushort.MaxValue"/>.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_UShort_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = ((ushort)1000).LeastCommonMultiple((ushort)1001);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="uint"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0u, 0u, 0u)]
    [DataRow(4u, 6u, 12u)]
    [DataRow(1_000_000u, 1_500_000u, 3_000_000u)]
    public void LeastCommonMultiple_UInt_ShouldReturnExpected(uint a, uint b, uint expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> on <see cref="uint"/> throws when the result exceeds <see cref="uint.MaxValue"/>.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_UInt_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = uint.MaxValue.LeastCommonMultiple(uint.MaxValue - 1u);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="ulong"/> returns the expected value.
    /// </summary>
    [DataTestMethod]
    [DataRow(0ul, 0ul, 0ul)]
    [DataRow(4ul, 6ul, 12ul)]
    [DataRow(1_000_000_000_000ul, 1_500_000_000_000ul, 3_000_000_000_000ul)]
    public void LeastCommonMultiple_ULong_ShouldReturnExpected(ulong a, ulong b, ulong expected) =>
        Assert.AreEqual(expected, a.LeastCommonMultiple(b));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> for <see cref="ulong"/> works for moderate values without overflow.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ULong_WhenSmallCoprime_ShouldReturnProduct() =>
        Assert.AreEqual(1_000_000_007UL * 1_000_000_009UL, 1_000_000_007UL.LeastCommonMultiple(1_000_000_009UL));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> on <see cref="ulong"/> throws when the multiplication overflows.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ULong_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = ulong.MaxValue.LeastCommonMultiple(ulong.MaxValue - 1ul);
        });

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple(value, value)</c> equals <c>value</c>.
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(12)]
    public void LeastCommonMultiple_Int_WhenInputsAreEqual_ShouldReturnInput(int value) =>
        Assert.AreEqual(value, value.LeastCommonMultiple(value));

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple(value, 1)</c> always equals <c>value</c> for positive inputs.
    /// </summary>
    [DataTestMethod]
    [DataRow(1)]
    [DataRow(7)]
    [DataRow(48)]
    [DataRow(1_000_000)]
    public void LeastCommonMultiple_Int_WhenOtherIsOne_ShouldReturnValue(int value)
    {
        Assert.AreEqual(value, value.LeastCommonMultiple(1));
        Assert.AreEqual(value, 1.LeastCommonMultiple(value));
    }

    /// <summary>
    /// Verifies that <c>LeastCommonMultiple</c> is commutative.
    /// </summary>
    [DataTestMethod]
    [DataRow(0, 0)]
    [DataRow(4, 6)]
    [DataRow(21, 6)]
    [DataRow(7, 13)]
    public void LeastCommonMultiple_Int_ShouldBeCommutative(int a, int b) =>
        Assert.AreEqual(a.LeastCommonMultiple(b), b.LeastCommonMultiple(a));

    /// <summary>
    /// Verifies the identity <c>gcd(a, b) * lcm(a, b) == a * b</c> for representative positive pairs.
    /// </summary>
    [DataTestMethod]
    [DataRow(12L, 18L)]
    [DataRow(15L, 25L)]
    [DataRow(7L, 13L)]
    [DataRow(48L, 18L)]
    [DataRow(100L, 75L)]
    public void GcdAndLcm_ShouldSatisfyGcdTimesLcmEqualsProduct(long a, long b)
    {
        long gcd = a.GreatestCommonDivisor(b);
        long lcm = a.LeastCommonMultiple(b);
        Assert.AreEqual(a * b, gcd * lcm);
    }

    /// <summary>
    /// Verifies that every array overload of <c>LeastCommonMultiple</c> rejects a <see langword="null"/> array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LcmArrayNullActions))]
    public void LeastCommonMultiple_AllArrayOverloads_WhenArrayIsNull_ShouldThrowArgumentNullException(Action action) =>
        Assert.ThrowsExactly<ArgumentNullException>(action);

    /// <summary>
    /// Verifies that every array overload of <c>LeastCommonMultiple</c> rejects an empty array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LcmArrayEmptyActions))]
    public void LeastCommonMultiple_AllArrayOverloads_WhenArrayIsEmpty_ShouldThrowArgumentException(Action action) =>
        Assert.ThrowsExactly<ArgumentException>(action);

    /// <summary>
    /// Verifies that the <see cref="int"/> array overload throws <see cref="ArgumentException"/> when empty.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_IntArray_WhenArrayIsEmpty_ShouldThrowArgumentException() =>
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Array.Empty<int>().LeastCommonMultiple();
        });

    /// <summary>
    /// Verifies that the <see cref="short"/> array overload rejects a negative element.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ShortArray_WhenAnyValueIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new short[] { 6, -2, 8 }.LeastCommonMultiple();
        });

    /// <summary>
    /// Verifies that the <see cref="long"/> array overload rejects a negative element.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_LongArray_WhenAnyValueIsNegative_ShouldThrowArgumentOutOfRangeException() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new long[] { 6L, -2L, 8L }.LeastCommonMultiple();
        });

    /// <summary>
    /// Verifies that the <see cref="int"/> array overload returns the LCM of every element.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_IntArray_WhenInputsAreNonNegative_ShouldReturnExpectedLcm() =>
        Assert.AreEqual(24, new int[] { 4, 6, 8 }.LeastCommonMultiple());

    /// <summary>
    /// Verifies that the <see cref="short"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ShortArray_ShouldReturnExpected() =>
        Assert.AreEqual((short)24, new short[] { 4, 6, 8 }.LeastCommonMultiple());

    /// <summary>
    /// Verifies that the <see cref="long"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_LongArray_ShouldReturnExpected() =>
        Assert.AreEqual(24L, new long[] { 4L, 6L, 8L }.LeastCommonMultiple());

    /// <summary>
    /// Verifies that the <see cref="ushort"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_UShortArray_ShouldReturnExpected() =>
        Assert.AreEqual((ushort)24, new ushort[] { 4, 6, 8 }.LeastCommonMultiple());

    /// <summary>
    /// Verifies that the <see cref="uint"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_UIntArray_ShouldReturnExpected() =>
        Assert.AreEqual(24u, new uint[] { 4u, 6u, 8u }.LeastCommonMultiple());

    /// <summary>
    /// Verifies that the <see cref="ulong"/> array overload returns the expected LCM.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ULongArray_ShouldReturnExpected() =>
        Assert.AreEqual(24ul, new ulong[] { 4ul, 6ul, 8ul }.LeastCommonMultiple());

    /// <summary>
    /// Verifies that a single-element array returns the element itself for every overload.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_SingleElementArray_ShouldReturnElement()
    {
        Assert.AreEqual((short)42, new short[] { 42 }.LeastCommonMultiple());
        Assert.AreEqual(42, new int[] { 42 }.LeastCommonMultiple());
        Assert.AreEqual(42L, new long[] { 42L }.LeastCommonMultiple());
        Assert.AreEqual((ushort)42, new ushort[] { 42 }.LeastCommonMultiple());
        Assert.AreEqual(42u, new uint[] { 42u }.LeastCommonMultiple());
        Assert.AreEqual(42ul, new ulong[] { 42ul }.LeastCommonMultiple());
    }

    /// <summary>
    /// Verifies that an array containing a zero element produces a zero LCM (mathematically,
    /// <c>lcm(0, n) = 0</c>).
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_IntArray_WhenContainsZero_ShouldReturnZero()
    {
        Assert.AreEqual(0, new int[] { 0, 12, 18 }.LeastCommonMultiple());
        Assert.AreEqual(0, new int[] { 12, 0, 18 }.LeastCommonMultiple());
    }
}
