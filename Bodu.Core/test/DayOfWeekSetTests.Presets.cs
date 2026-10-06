// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.Presets.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class DayOfWeekSetTests
{

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.MondayToFriday" /> is equal to <see cref="DayOfWeekSet.Weekdays" />.
    /// </summary>
    [TestMethod]
    public void MondayToFriday_WhenAccessed_ShouldEqualWeekdays() => Assert.AreEqual(DayOfWeekSet.Weekdays, DayOfWeekSet.MondayToFriday);

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.MondayToSaturday" /> contains Monday through Saturday and excludes Sunday.
    /// </summary>
    [TestMethod]
    public void MondayToSaturday_WhenAccessed_ShouldContainMondayThroughSaturday()
    {
        DayOfWeekSet days = DayOfWeekSet.MondayToSaturday;

        Assert.AreEqual(6, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Monday));
        Assert.IsTrue(days.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(days.Contains(DayOfWeek.Friday));
        Assert.IsTrue(days.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(days.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.MondayToThursdayAndSaturday" /> contains Monday through Thursday and Saturday,
    /// excluding Friday and Sunday.
    /// </summary>
    [TestMethod]
    public void MondayToThursdayAndSaturday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet days = DayOfWeekSet.MondayToThursdayAndSaturday;

        Assert.AreEqual(5, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Monday));
        Assert.IsTrue(days.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(days.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(days.Contains(DayOfWeek.Friday));
        Assert.IsFalse(days.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SaturdayToThursday" /> contains Saturday through Thursday and excludes Friday.
    /// </summary>
    [TestMethod]
    public void SaturdayToThursday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet days = DayOfWeekSet.SaturdayToThursday;

        Assert.AreEqual(6, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(days.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(days.Contains(DayOfWeek.Monday));
        Assert.IsTrue(days.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(days.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SaturdayToWednesday" /> contains Saturday through Wednesday and excludes
    /// Thursday and Friday.
    /// </summary>
    [TestMethod]
    public void SaturdayToWednesday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet days = DayOfWeekSet.SaturdayToWednesday;

        Assert.AreEqual(5, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(days.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(days.Contains(DayOfWeek.Monday));
        Assert.IsTrue(days.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(days.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(days.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SundayToFriday" /> contains Sunday through Friday and excludes Saturday.
    /// </summary>
    [TestMethod]
    public void SundayToFriday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet days = DayOfWeekSet.SundayToFriday;

        Assert.AreEqual(6, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(days.Contains(DayOfWeek.Monday));
        Assert.IsTrue(days.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(days.Contains(DayOfWeek.Friday));
        Assert.IsFalse(days.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SundayToThursday" /> contains Sunday through Thursday and excludes Friday and
    /// Saturday.
    /// </summary>
    [TestMethod]
    public void SundayToThursday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet days = DayOfWeekSet.SundayToThursday;

        Assert.AreEqual(5, days.Count);
        Assert.IsTrue(days.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(days.Contains(DayOfWeek.Monday));
        Assert.IsTrue(days.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(days.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(days.Contains(DayOfWeek.Friday));
        Assert.IsFalse(days.Contains(DayOfWeek.Saturday));
    }

}
