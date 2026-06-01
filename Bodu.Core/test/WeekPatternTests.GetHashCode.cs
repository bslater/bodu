// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.GetHashCode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that <see cref="WeekPattern.GetHashCode" /> returns the same value as the hash code of
    /// the underlying <see cref="byte" /> bitmask across all 128 valid permutations.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenCalled_ShouldMatchInternalByteHashCode()
    {
        for (byte value = 0; value <= 127; value++)
        {
            var pattern = WeekPattern.FromByte(value);
            Assert.AreEqual(value.GetHashCode(), pattern.GetHashCode(),
                $"Hash mismatch for byte value {value}.");
        }
    }

    /// <summary>
    /// Verifies that two <see cref="WeekPattern" /> instances with different selected days return
    /// different hash codes.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenDifferentDays_ShouldReturnDifferentHash()
    {
        Assert.AreNotEqual(
            new WeekPattern(DayOfWeek.Monday).GetHashCode(),
            new WeekPattern(DayOfWeek.Tuesday).GetHashCode());
    }
    /// <summary>
    /// Verifies that two <see cref="WeekPattern" /> instances with identical selected days return the
    /// same hash code.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenSameDays_ShouldReturnSameHash()
    {
        var p1 = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday);
        var p2 = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday);
        Assert.AreEqual(p1.GetHashCode(), p2.GetHashCode());
    }

}
