// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Contains.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{
    /// <summary>
    /// Verifies that <see cref="WeekPattern.Contains" /> returns <see langword="true" /> for every day
    /// that was explicitly selected at construction.
    /// </summary>
    [TestMethod]
    [DataRow(DayOfWeek.Sunday)]
    [DataRow(DayOfWeek.Monday)]
    [DataRow(DayOfWeek.Tuesday)]
    [DataRow(DayOfWeek.Wednesday)]
    [DataRow(DayOfWeek.Thursday)]
    [DataRow(DayOfWeek.Friday)]
    [DataRow(DayOfWeek.Saturday)]
    public void Contains_WhenDayIsSelected_ShouldReturnTrue(DayOfWeek day)
    {
        var pattern = new WeekPattern(day);
        Assert.IsTrue(pattern.Contains(day));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Contains" /> returns <see langword="false" /> for days that
    /// were not selected.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDayIsNotSelected_ShouldReturnFalse()
    {
        var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday);

        Assert.IsFalse(pattern.Contains(DayOfWeek.Sunday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Contains" /> returns <see langword="false" /> for every day
    /// when called on <see cref="WeekPattern.Empty" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenCalledOnEmpty_ShouldReturnFalseForAllDays()
    {
        var pattern = WeekPattern.Empty;

        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            Assert.IsFalse(pattern.Contains(day), $"{day} should not be contained in Empty.");
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Contains" /> returns the same result as the indexer getter
    /// for every valid <see cref="DayOfWeek" />, confirming they are equivalent.
    /// </summary>
    [TestMethod]
    public void Contains_WhenComparedToIndexer_ShouldReturnIdenticalResult()
    {
        var pattern = WeekPattern.Weekdays;

        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            Assert.AreEqual(pattern[day], pattern.Contains(day),
                $"Contains and indexer should agree for {day}.");
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Contains" /> throws <see cref="ArgumentOutOfRangeException" />
    /// when an invalid <see cref="DayOfWeek" /> value is supplied.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(7)]
    [DataRow(99)]
    public void Contains_WhenInvalidDay_ShouldThrowArgumentOutOfRangeException(int invalidDay)
    {
        var pattern = WeekPattern.Empty;
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = pattern.Contains((DayOfWeek)invalidDay);
        });
    }
}
