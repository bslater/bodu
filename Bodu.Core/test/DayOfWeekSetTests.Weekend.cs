// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.Weekend.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class DayOfWeekSetTests
{

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Weekend" /> contains exactly Saturday and Sunday, with all
    /// weekday days unselected, and reports a count of two.
    /// </summary>
    [TestMethod]
    public void Weekend_WhenAccessed_ShouldContainWeekendDays()
    {
        DayOfWeekSet days = DayOfWeekSet.Weekend;

        Assert.AreEqual(2, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(days.Contains(DayOfWeek.Sunday));
        Assert.IsFalse(days.Contains(DayOfWeek.Monday));
        Assert.IsFalse(days.Contains(DayOfWeek.Tuesday));
        Assert.IsFalse(days.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(days.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(days.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Weekend" /> returns a consistent value across multiple
    /// accesses.
    /// </summary>
    [TestMethod]
    public void Weekend_WhenAccessedMultipleTimes_ShouldReturnConsistentValue() => Assert.AreEqual(DayOfWeekSet.Weekend, DayOfWeekSet.Weekend);

}
