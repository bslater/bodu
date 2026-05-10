// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Operators.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{
    // --------------------------------------------------------------------
    // Bitwise operators
    // --------------------------------------------------------------------

    /// <summary>
    /// Verifies that applying the <c>|</c> operator with <see cref="WeekPattern.Empty" /> returns a
    /// pattern equal to the original.
    /// </summary>
    [TestMethod]
    public void OrOperator_WhenCombinedWithEmpty_ShouldReturnOriginal()
    {
        var original = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Tuesday);
        Assert.AreEqual(original, original | WeekPattern.Empty);
    }

    /// <summary>
    /// Verifies that applying the <c>|</c> operator with itself returns a pattern equal to the original.
    /// </summary>
    [TestMethod]
    public void OrOperator_WhenCombinedWithItself_ShouldReturnSame()
    {
        var original = new WeekPattern(DayOfWeek.Monday);
        Assert.AreEqual(original, original | original);
    }

    /// <summary>
    /// Verifies that the <c>|</c> operator correctly unites two non-overlapping patterns.
    /// </summary>
    [TestMethod]
    public void OrOperator_WhenDisjointSets_ShouldUniteCorrectly()
    {
        WeekPattern result = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday)
                   | new WeekPattern(DayOfWeek.Friday);

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsTrue(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.AreEqual(3, result.Count);
    }

    /// <summary>
    /// Verifies that the bitwise OR of <see cref="WeekPattern.Weekdays" /> and
    /// <see cref="WeekPattern.Weekend" /> produces a fully-populated seven-day pattern.
    /// </summary>
    [TestMethod]
    public void OrOperator_WhenWeekdaysCombinedWithWeekend_ShouldProduceFullWeek()
    {
        WeekPattern full = WeekPattern.Weekdays | WeekPattern.Weekend;

        Assert.AreEqual(7, full.Count);
        Assert.AreEqual(WeekPattern.FromByte(0b1111111), full);
    }

    /// <summary>
    /// Verifies that applying the <c>&amp;</c> operator with <see cref="WeekPattern.Empty" /> returns
    /// an empty pattern.
    /// </summary>
    [TestMethod]
    public void AndOperator_WhenCombinedWithEmpty_ShouldReturnEmpty()
    {
        var original = new WeekPattern(DayOfWeek.Monday);
        Assert.AreEqual(WeekPattern.Empty, original & WeekPattern.Empty);
    }

    /// <summary>
    /// Verifies that applying the <c>&amp;</c> operator with itself returns a pattern equal to the original.
    /// </summary>
    [TestMethod]
    public void AndOperator_WhenCombinedWithItself_ShouldReturnSame()
    {
        var original = new WeekPattern(DayOfWeek.Wednesday);
        Assert.AreEqual(original, original & original);
    }

    /// <summary>
    /// Verifies that the bitwise AND of <see cref="WeekPattern.Weekdays" /> and
    /// <see cref="WeekPattern.Weekend" /> returns an empty pattern, confirming the two are disjoint.
    /// </summary>
    [TestMethod]
    public void AndOperator_WhenWeekdaysIntersectedWithWeekend_ShouldReturnEmpty() => Assert.AreEqual(WeekPattern.Empty, WeekPattern.Weekdays & WeekPattern.Weekend);

    /// <summary>
    /// Verifies that applying the <c>^</c> operator with <see cref="WeekPattern.Empty" /> returns a
    /// pattern equal to the original.
    /// </summary>
    [TestMethod]
    public void XorOperator_WhenCombinedWithEmpty_ShouldReturnOriginal()
    {
        var original = new WeekPattern(DayOfWeek.Friday);
        Assert.AreEqual(original, original ^ WeekPattern.Empty);
    }

    /// <summary>
    /// Verifies that applying the <c>^</c> operator with itself produces an empty pattern.
    /// </summary>
    [TestMethod]
    public void XorOperator_WhenCombinedWithItself_ShouldReturnEmpty()
    {
        var original = new WeekPattern(DayOfWeek.Thursday);
        Assert.AreEqual(WeekPattern.Empty, original ^ original);
    }

    /// <summary>
    /// Verifies that the <c>^</c> operator retains only days that appear in exactly one operand when
    /// the two patterns partially overlap.
    /// </summary>
    [TestMethod]
    public void XorOperator_WhenPartialOverlap_ShouldRetainNonOverlappingDays()
    {
        var a = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday);
        var b = new WeekPattern(DayOfWeek.Wednesday, DayOfWeek.Friday);
        WeekPattern result = a ^ b;

        Assert.IsTrue(result.Contains(DayOfWeek.Monday), "Monday appears only in a — should be retained.");
        Assert.IsFalse(result.Contains(DayOfWeek.Wednesday), "Wednesday appears in both — should be cancelled.");
        Assert.IsTrue(result.Contains(DayOfWeek.Friday), "Friday appears only in b — should be retained.");
    }

    /// <summary>
    /// Verifies that applying the <c>~</c> operator to <see cref="WeekPattern.Empty" /> produces a
    /// fully-populated seven-day pattern.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenAppliedToEmpty_ShouldReturnFullSet() => Assert.AreEqual(WeekPattern.FromByte(0b1111111), ~WeekPattern.Empty);

    /// <summary>
    /// Verifies that applying the <c>~</c> operator to a fully-populated pattern returns an empty pattern.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenAppliedToFullSet_ShouldReturnEmpty() => Assert.AreEqual(WeekPattern.Empty, ~WeekPattern.FromByte(0b1111111));

    /// <summary>
    /// Verifies that the complement of <see cref="WeekPattern.Weekdays" /> equals
    /// <see cref="WeekPattern.Weekend" />.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenAppliedToWeekdays_ShouldReturnWeekend() => Assert.AreEqual(WeekPattern.Weekend, ~WeekPattern.Weekdays);

    /// <summary>
    /// Verifies that the complement of <see cref="WeekPattern.Weekend" /> equals
    /// <see cref="WeekPattern.Weekdays" />.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenAppliedToWeekend_ShouldReturnWeekdays() => Assert.AreEqual(WeekPattern.Weekdays, ~WeekPattern.Weekend);

    /// <summary>
    /// Verifies that applying the <c>~</c> operator twice returns the original pattern, confirming the
    /// double-complement identity.
    /// </summary>
    [TestMethod]
    public void ComplementOperator_WhenAppliedTwice_ShouldReturnOriginal()
    {
        var original = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Thursday);
        Assert.AreEqual(original, ~~original);
    }

    // --------------------------------------------------------------------
    // Equality operators
    // --------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>==</c> operator returns <see langword="true" /> for two instances with
    /// identical selected days.
    /// </summary>
    [TestMethod]
    public void EqualityOperator_WhenSameDays_ShouldReturnTrue()
    {
        var a = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Friday);
        var b = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Friday);
        Assert.IsTrue(a == b);
    }

    /// <summary>
    /// Verifies that the <c>==</c> operator returns <see langword="false" /> for two instances with
    /// different selected days.
    /// </summary>
    [TestMethod]
    public void EqualityOperator_WhenDifferentDays_ShouldReturnFalse() => Assert.IsFalse(new WeekPattern(DayOfWeek.Monday) == new WeekPattern(DayOfWeek.Tuesday));

    /// <summary>
    /// Verifies that the <c>!=</c> operator returns <see langword="true" /> for two instances with
    /// different selected days.
    /// </summary>
    [TestMethod]
    public void InequalityOperator_WhenDifferentDays_ShouldReturnTrue() => Assert.IsTrue(new WeekPattern(DayOfWeek.Monday) != new WeekPattern(DayOfWeek.Tuesday));

    /// <summary>
    /// Verifies that the <c>!=</c> operator returns <see langword="false" /> for two instances with
    /// identical selected days.
    /// </summary>
    [TestMethod]
    public void InequalityOperator_WhenSameDays_ShouldReturnFalse()
    {
        var a = new WeekPattern(DayOfWeek.Wednesday);
        var b = new WeekPattern(DayOfWeek.Wednesday);
        Assert.IsFalse(a != b);
    }

    // --------------------------------------------------------------------
    // Implicit byte conversion
    // --------------------------------------------------------------------

    /// <summary>
    /// Verifies that the implicit conversion to <see cref="byte" /> preserves the underlying bitmask
    /// value across a round-trip.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0)]
    [DataRow((byte)1)]
    [DataRow((byte)5)]
    [DataRow((byte)127)]
    public void ImplicitConversionToByte_WhenRoundTripped_ShouldMatchOriginal(byte original)
    {
        WeekPattern pattern = WeekPattern.FromByte(original);
        byte converted = pattern;
        Assert.AreEqual(original, converted);
    }

    // --------------------------------------------------------------------
    // Numeric conversion methods
    // --------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ToByte" /> returns the underlying bitmask value.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0)]
    [DataRow((byte)1)]
    [DataRow((byte)127)]
    public void ToByte_WhenCalled_ShouldReturnUnderlyingBitmask(byte value) => Assert.AreEqual(value, WeekPattern.FromByte(value).ToByte());

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ToInt32" /> returns the underlying bitmask value as an
    /// <see cref="int" />.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0)]
    [DataRow((byte)1)]
    [DataRow((byte)127)]
    public void ToInt32_WhenCalled_ShouldReturnUnderlyingBitmaskAsInt(byte value) => Assert.AreEqual((int)value, WeekPattern.FromByte(value).ToInt32());

    /// <summary>
    /// Verifies that <see cref="WeekPattern.ToByte" />, <see cref="WeekPattern.ToInt32" />, and the
    /// implicit <see cref="byte" /> conversion all return consistent values for the same instance.
    /// </summary>
    [TestMethod]
    public void NumericConversions_WhenCalledOnSameInstance_ShouldProduceConsistentValues()
    {
        var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

        var viaToByte = pattern.ToByte();
        var viaToInt32 = pattern.ToInt32();
        byte viaImplicitByte = pattern;

        Assert.AreEqual(viaToByte, (byte)viaToInt32, "ToByte and ToInt32 should agree.");
        Assert.AreEqual(viaToByte, viaImplicitByte, "ToByte and implicit byte should agree.");
    }
}
