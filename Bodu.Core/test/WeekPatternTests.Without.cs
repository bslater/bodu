// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Without.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{
    /// <summary>
    /// Verifies that <see cref="WeekPattern.Without" /> returns a new instance that no longer contains
    /// the removed day.
    /// </summary>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    [DataRow(DayOfWeek.Friday)]
    [DataRow(DayOfWeek.Saturday)]
    public void Without_WhenSelectedDayProvided_ShouldReturnInstanceWithDayRemoved(DayOfWeek day)
    {
        var original = WeekPattern.FromByte(0b1111111);
        WeekPattern result = original.Without(day);

        Assert.IsFalse(result.Contains(day));
        Assert.AreEqual(6, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Without" /> preserves all remaining selected days and only
    /// removes the specified day.
    /// </summary>
    [TestMethod]
    public void Without_WhenCalled_ShouldPreserveOtherDays()
    {
        var original = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
        WeekPattern result = original.Without(DayOfWeek.Wednesday);

        Assert.IsTrue(result.Contains(DayOfWeek.Monday));
        Assert.IsFalse(result.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(result.Contains(DayOfWeek.Friday));
        Assert.AreEqual(2, result.Count);
    }

    /// <summary>
    /// Verifies that calling <see cref="WeekPattern.Without" /> for a day that is not selected returns
    /// an instance equal to the original.
    /// </summary>
    [TestMethod]
    public void Without_WhenDayNotSelected_ShouldReturnEquivalentInstance()
    {
        var original = new WeekPattern(DayOfWeek.Monday);
        WeekPattern result = original.Without(DayOfWeek.Friday);

        Assert.AreEqual(original, result);
        Assert.AreEqual(1, result.Count);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Without" /> does not mutate the original instance,
    /// confirming the immutable value-type contract.
    /// </summary>
    [TestMethod]
    public void Without_WhenCalled_ShouldNotMutateOriginal()
    {
        var original = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday);
        _ = original.Without(DayOfWeek.Monday);

        Assert.IsTrue(original.Contains(DayOfWeek.Monday),
            "Without must not modify the receiver; WeekPattern is immutable.");
        Assert.AreEqual(2, original.Count);
    }

    /// <summary>
    /// Verifies that removing all days one by one via chained <see cref="WeekPattern.Without" /> calls
    /// produces a pattern equal to <see cref="WeekPattern.Empty" />.
    /// </summary>
    [TestMethod]
    public void Without_WhenAllDaysRemoved_ShouldProduceEmptyPattern()
    {
        WeekPattern result = WeekPattern.FromByte(0b1111111)
            .Without(DayOfWeek.Sunday)
            .Without(DayOfWeek.Monday)
            .Without(DayOfWeek.Tuesday)
            .Without(DayOfWeek.Wednesday)
            .Without(DayOfWeek.Thursday)
            .Without(DayOfWeek.Friday)
            .Without(DayOfWeek.Saturday);

        Assert.AreEqual(WeekPattern.Empty, result);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Without" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when an invalid <see cref="DayOfWeek" /> value is supplied.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(99)]
    public void Without_WhenInvalidDay_ShouldThrowArgumentOutOfRangeException(int invalidDay)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = WeekPattern.Weekdays.Without((DayOfWeek)invalidDay);
        });
    }
}
