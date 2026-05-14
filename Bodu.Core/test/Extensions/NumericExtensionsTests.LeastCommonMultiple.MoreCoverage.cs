// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.LeastCommonMultiple.MoreCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
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
    /// Verifies that every array overload of <c>LeastCommonMultiple</c> rejects a <see langword="null"/> array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LcmArrayNullActions))]
    public void LeastCommonMultiple_AllArrayOverloads_WhenArrayIsNull_ShouldThrowArgumentNullException(Action action) =>
        Assert.ThrowsExactly<ArgumentNullException>(action);

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
    /// Verifies that every array overload of <c>LeastCommonMultiple</c> rejects an empty array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LcmArrayEmptyActions))]
    public void LeastCommonMultiple_AllArrayOverloads_WhenArrayIsEmpty_ShouldThrowArgumentException(Action action) =>
        Assert.ThrowsExactly<ArgumentException>(action);

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
    /// Verifies that <c>LeastCommonMultiple</c> throws <see cref="OverflowException"/> for narrow types
    /// where the result exceeds the type range.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Short_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = ((short)1000).LeastCommonMultiple((short)1001);
        });

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
    /// Verifies that <c>LeastCommonMultiple</c> on <see cref="long"/> throws when the result exceeds <see cref="long.MaxValue"/>.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_Long_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = long.MaxValue.LeastCommonMultiple(long.MaxValue - 1);
        });

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
    /// Verifies that <c>LeastCommonMultiple</c> on <see cref="ulong"/> throws when the multiplication overflows.
    /// </summary>
    [TestMethod]
    public void LeastCommonMultiple_ULong_WhenResultOverflows_ShouldThrowOverflowException() =>
        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = ulong.MaxValue.LeastCommonMultiple(ulong.MaxValue - 1ul);
        });

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
}
