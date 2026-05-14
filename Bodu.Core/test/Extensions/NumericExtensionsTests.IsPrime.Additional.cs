// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.IsPrime.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="short"/> returns the expected result.
    /// </summary>
    [DataTestMethod]
    [DataRow((short)-7, false)]
    [DataRow((short)0, false)]
    [DataRow((short)1, false)]
    [DataRow((short)2, true)]
    [DataRow((short)97, true)]
    [DataRow((short)100, false)]
    public void IsPrime_Short_ShouldReturnExpected(short value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="long"/> returns the expected result.
    /// </summary>
    [DataTestMethod]
    [DataRow(-7L, false)]
    [DataRow(0L, false)]
    [DataRow(1L, false)]
    [DataRow(2L, true)]
    [DataRow(97L, true)]
    [DataRow(100L, false)]
    [DataRow(1_000_000_007L, true)]
    public void IsPrime_Long_ShouldReturnExpected(long value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="ushort"/> returns the expected result.
    /// </summary>
    [DataTestMethod]
    [DataRow((ushort)0, false)]
    [DataRow((ushort)1, false)]
    [DataRow((ushort)2, true)]
    [DataRow((ushort)97, true)]
    [DataRow((ushort)100, false)]
    [DataRow((ushort)65521, true)]
    public void IsPrime_UShort_ShouldReturnExpected(ushort value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="uint"/> returns the expected result.
    /// </summary>
    [DataTestMethod]
    [DataRow(0u, false)]
    [DataRow(1u, false)]
    [DataRow(2u, true)]
    [DataRow(97u, true)]
    [DataRow(100u, false)]
    [DataRow(1_000_000_007u, true)]
    public void IsPrime_UInt_ShouldReturnExpected(uint value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="ulong"/> returns the expected result for boundary values.
    /// </summary>
    [DataTestMethod]
    [DataRow(0ul, false)]
    [DataRow(1ul, false)]
    [DataRow(2ul, true)]
    public void IsPrime_ULong_ForBoundaryValues_ShouldReturnExpected(ulong value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());
}
