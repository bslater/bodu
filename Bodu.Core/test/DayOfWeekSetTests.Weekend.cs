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
        DayOfWeekSet pattern = DayOfWeekSet.Weekend;

        Assert.AreEqual(2, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Sunday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Monday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.Weekend" /> returns a consistent value across multiple
    /// accesses.
    /// </summary>
    [TestMethod]
    public void Weekend_WhenAccessedMultipleTimes_ShouldReturnConsistentValue() => Assert.AreEqual(DayOfWeekSet.Weekend, DayOfWeekSet.Weekend);

}
