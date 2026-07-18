// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Equals.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Equals(byte)" /> returns the expected result when comparing
    /// against a raw bitmask using the typed overload directly.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0, true)]
    [DataRow((byte)1, (byte)1, true)]
    [DataRow((byte)127, (byte)127, true)]
    [DataRow((byte)3, (byte)7, false)]
    [DataRow((byte)5, (byte)10, false)]
    public void EqualsByte_WhenComparing_ShouldReturnExpectedResult(byte first, byte second, bool expected) => Assert.AreEqual(expected, WeekPattern.FromByte(first).Equals(second));

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns <see langword="false" /> for a boxed
    /// <see cref="byte" />, even when its value matches the underlying bitmask, so that object-level equality stays
    /// symmetric with <see cref="byte.Equals(object)" />.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0)]
    [DataRow((byte)1, (byte)1)]
    [DataRow((byte)127, (byte)127)]
    [DataRow((byte)7, (byte)5)]
    [DataRow((byte)0, (byte)1)]
    public void EqualsObject_WhenBoxedByte_ShouldReturnFalse(byte patternMask, byte compareMask)
    {
        var pattern = WeekPattern.FromByte(patternMask);
        Assert.IsFalse(pattern.Equals((object)compareMask));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Equals(object)" /> is symmetric with <see cref="byte.Equals(object)" />:
    /// comparing a <see cref="WeekPattern" /> to a boxed <see cref="byte" /> yields the same result in both directions
    /// (always <see langword="false" />), even when their bitmasks match.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0)]
    [DataRow((byte)1)]
    [DataRow((byte)42)]
    [DataRow((byte)127)]
    public void EqualsObject_WhenComparedWithBoxedByte_ShouldBeSymmetric(byte mask)
    {
        var pattern = WeekPattern.FromByte(mask);
        object boxedByte = mask;
        object boxedPattern = pattern;

        Assert.AreEqual(boxedByte.Equals(boxedPattern), pattern.Equals(boxedByte));
        Assert.IsFalse(pattern.Equals(boxedByte));
        Assert.IsFalse(boxedByte.Equals(boxedPattern));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns the expected result when the
    /// argument is a boxed <see cref="WeekPattern" />.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0, true)]
    [DataRow((byte)1, (byte)1, true)]
    [DataRow((byte)0, (byte)1, false)]
    [DataRow((byte)5, (byte)10, false)]
    public void EqualsObject_WhenComparingWeekPattern_ShouldReturnExpectedResult(byte first, byte second, bool expected)
    {
        var p1 = WeekPattern.FromByte(first);
        var p2 = WeekPattern.FromByte(second);
        Assert.AreEqual(expected, p1.Equals((object)p2));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns <see langword="false" /> when the
    /// argument is of an incompatible type.
    /// </summary>
    [TestMethod]
    public void EqualsObject_WhenDifferentType_ShouldReturnFalse() => Assert.IsFalse(new WeekPattern(DayOfWeek.Monday).Equals("not a WeekPattern"));
    /// <summary>
    /// Verifies that <see cref="WeekPattern.Equals(object)" /> returns <see langword="false" /> when the
    /// argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EqualsObject_WhenNull_ShouldReturnFalse() => Assert.IsFalse(new WeekPattern(DayOfWeek.Monday).Equals(null));

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Equals(WeekPattern)" /> returns the expected result when
    /// comparing two instances by value.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0, (byte)0, true)]
    [DataRow((byte)1, (byte)1, true)]
    [DataRow((byte)0, (byte)1, false)]
    [DataRow((byte)7, (byte)5, false)]
    public void EqualsWeekPattern_WhenComparing_ShouldReturnExpectedResult(byte first, byte second, bool expected) => Assert.AreEqual(expected, WeekPattern.FromByte(first).Equals(WeekPattern.FromByte(second)));

}
