// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.IsPrime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{

    /// <summary>
    /// Verifies that <c>IsPrime</c> returns <see langword="true"/> for known small primes and <see langword="false"/> otherwise.
    /// </summary>
    [TestMethod]
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
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="int.MaxValue"/> as a Mersenne prime
    /// (<c>M31 = 2^31 - 1</c>).
    /// </summary>
    [TestMethod]
    public void IsPrime_Int_WhenMaxValue_ShouldReturnTrue() =>
        Assert.IsTrue(int.MaxValue.IsPrime());

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
    /// Verifies that <c>IsPrime</c> on <see cref="long"/> returns the expected result.
    /// </summary>
    [TestMethod]
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
    /// Verifies that <c>IsPrime</c> rejects <c>long.MinValue</c> without throwing.
    /// </summary>
    [TestMethod]
    public void IsPrime_Long_WhenMinValue_ShouldReturnFalse() =>
        Assert.IsFalse(long.MinValue.IsPrime());
    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="short"/> returns the expected result.
    /// </summary>
    [TestMethod]
    [DataRow((short)-7, false)]
    [DataRow((short)0, false)]
    [DataRow((short)1, false)]
    [DataRow((short)2, true)]
    [DataRow((short)97, true)]
    [DataRow((short)100, false)]
    public void IsPrime_Short_ShouldReturnExpected(short value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="short.MaxValue"/> as composite
    /// (<c>32767 = 7 * 31 * 151</c>).
    /// </summary>
    [TestMethod]
    public void IsPrime_Short_WhenMaxValue_ShouldReturnFalse() =>
        Assert.IsFalse(short.MaxValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> rejects <c>short.MinValue</c> without throwing.
    /// </summary>
    [TestMethod]
    public void IsPrime_Short_WhenMinValue_ShouldReturnFalse() =>
        Assert.IsFalse(short.MinValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="uint"/> returns the expected result.
    /// </summary>
    [TestMethod]
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
    [TestMethod]
    [DataRow(0ul, false)]
    [DataRow(1ul, false)]
    [DataRow(2ul, true)]
    public void IsPrime_ULong_ForBoundaryValues_ShouldReturnExpected(ulong value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="ulong"/> returns <see langword="false"/> for a large composite.
    /// </summary>
    [TestMethod]
    public void IsPrime_ULong_WhenLargeComposite_ShouldReturnFalse() =>
        Assert.IsFalse((1_000_000_007UL * 3UL).IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="ulong"/> returns <see langword="true"/> for a large known prime.
    /// </summary>
    [TestMethod]
    public void IsPrime_ULong_WhenLargeKnownPrime_ShouldReturnTrue() =>
        Assert.IsTrue(1_000_000_007UL.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="ulong.MaxValue"/> (<c>2^64 - 1</c>)
    /// as composite. The smallest prime factor is <c>3</c>, so the trial-division loop rejects quickly.
    /// </summary>
    [TestMethod]
    public void IsPrime_ULong_WhenMaxValue_ShouldReturnFalse() =>
        Assert.IsFalse(ulong.MaxValue.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> on <see cref="ushort"/> returns the expected result.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0, false)]
    [DataRow((ushort)1, false)]
    [DataRow((ushort)2, true)]
    [DataRow((ushort)97, true)]
    [DataRow((ushort)100, false)]
    [DataRow((ushort)65521, true)]
    public void IsPrime_UShort_ShouldReturnExpected(ushort value, bool expected) =>
        Assert.AreEqual(expected, value.IsPrime());

    /// <summary>
    /// Verifies that <c>IsPrime</c> correctly identifies <see cref="ushort.MaxValue"/> as composite
    /// (<c>65535 = 3 * 5 * 17 * 257</c>).
    /// </summary>
    [TestMethod]
    public void IsPrime_UShort_WhenMaxValue_ShouldReturnFalse() =>
        Assert.IsFalse(ushort.MaxValue.IsPrime());

}
