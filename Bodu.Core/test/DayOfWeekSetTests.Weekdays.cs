// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.Weekdays.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class DayOfWeekSetTests
{

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Weekdays" /> contains exactly Monday through Friday, with
    /// Saturday and Sunday unselected, and reports a count of five.
    /// </summary>
    [TestMethod]
    public void Weekdays_WhenAccessed_ShouldContainWeekdays()
    {
        DayOfWeekSet days = DayOfWeekSet.Weekdays;

        Assert.AreEqual(5, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Monday));
        Assert.IsTrue(days.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(days.Contains(DayOfWeek.Friday));
        Assert.IsFalse(days.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(days.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Weekdays" /> returns a consistent value across multiple
    /// accesses.
    /// </summary>
    [TestMethod]
    public void Weekdays_WhenAccessedMultipleTimes_ShouldReturnConsistentValue() => Assert.AreEqual(DayOfWeekSet.Weekdays, DayOfWeekSet.Weekdays);

}
