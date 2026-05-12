// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Weekdays.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{
    /// <summary>
    /// Verifies that <see cref="WeekPattern.Weekdays" /> contains exactly Monday through Friday, with
    /// Saturday and Sunday unselected, and reports a count of five.
    /// </summary>
    [TestMethod]
    public void Weekdays_WhenAccessed_ShouldContainWeekdays()
    {
        WeekPattern pattern = WeekPattern.Weekdays;

        Assert.AreEqual(5, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Friday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Weekdays" /> returns a consistent value across multiple
    /// accesses.
    /// </summary>
    [TestMethod]
    public void Weekdays_WhenAccessedMultipleTimes_ShouldReturnConsistentValue() => Assert.AreEqual(WeekPattern.Weekdays, WeekPattern.Weekdays);
}
