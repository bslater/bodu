// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Comparison.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{
    /// <summary>
    /// Verifies that the <c>&lt;</c> operator orders patterns by their underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand bitmask.</param>
    /// <param name="right">The right operand bitmask.</param>
    /// <param name="expected">The expected result of <c>left &lt; right</c>.</param>
    [TestMethod]
    [DataRow((byte)1, (byte)2, true)]
    [DataRow((byte)2, (byte)1, false)]
    [DataRow((byte)3, (byte)3, false)]
    [DataRow((byte)0, (byte)127, true)]
    public void LessThanOperator_WhenComparingPatterns_ShouldOrderByBitmask(byte left, byte right, bool expected) =>
        Assert.AreEqual(expected, WeekPattern.FromByte(left) < WeekPattern.FromByte(right));

    /// <summary>
    /// Verifies that the <c>&lt;=</c> operator orders patterns by their underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand bitmask.</param>
    /// <param name="right">The right operand bitmask.</param>
    /// <param name="expected">The expected result of <c>left &lt;= right</c>.</param>
    [TestMethod]
    [DataRow((byte)1, (byte)2, true)]
    [DataRow((byte)2, (byte)1, false)]
    [DataRow((byte)3, (byte)3, true)]
    [DataRow((byte)127, (byte)0, false)]
    public void LessThanOrEqualOperator_WhenComparingPatterns_ShouldOrderByBitmask(byte left, byte right, bool expected) =>
        Assert.AreEqual(expected, WeekPattern.FromByte(left) <= WeekPattern.FromByte(right));

    /// <summary>
    /// Verifies that the <c>&gt;</c> operator orders patterns by their underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand bitmask.</param>
    /// <param name="right">The right operand bitmask.</param>
    /// <param name="expected">The expected result of <c>left &gt; right</c>.</param>
    [TestMethod]
    [DataRow((byte)2, (byte)1, true)]
    [DataRow((byte)1, (byte)2, false)]
    [DataRow((byte)3, (byte)3, false)]
    [DataRow((byte)127, (byte)0, true)]
    public void GreaterThanOperator_WhenComparingPatterns_ShouldOrderByBitmask(byte left, byte right, bool expected) =>
        Assert.AreEqual(expected, WeekPattern.FromByte(left) > WeekPattern.FromByte(right));

    /// <summary>
    /// Verifies that the <c>&gt;=</c> operator orders patterns by their underlying bitmask value.
    /// </summary>
    /// <param name="left">The left operand bitmask.</param>
    /// <param name="right">The right operand bitmask.</param>
    /// <param name="expected">The expected result of <c>left &gt;= right</c>.</param>
    [TestMethod]
    [DataRow((byte)2, (byte)1, true)]
    [DataRow((byte)1, (byte)2, false)]
    [DataRow((byte)3, (byte)3, true)]
    [DataRow((byte)0, (byte)127, false)]
    public void GreaterThanOrEqualOperator_WhenComparingPatterns_ShouldOrderByBitmask(byte left, byte right, bool expected) =>
        Assert.AreEqual(expected, WeekPattern.FromByte(left) >= WeekPattern.FromByte(right));
}
