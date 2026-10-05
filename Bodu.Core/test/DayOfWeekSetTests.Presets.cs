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
        DayOfWeekSet pattern = DayOfWeekSet.MondayToSaturday;

        Assert.AreEqual(6, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Friday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.MondayToThursdayAndSaturday" /> contains Monday through Thursday and Saturday,
    /// excluding Friday and Sunday.
    /// </summary>
    [TestMethod]
    public void MondayToThursdayAndSaturday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet pattern = DayOfWeekSet.MondayToThursdayAndSaturday;

        Assert.AreEqual(5, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Saturday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Sunday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SaturdayToThursday" /> contains Saturday through Thursday and excludes Friday.
    /// </summary>
    [TestMethod]
    public void SaturdayToThursday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet pattern = DayOfWeekSet.SaturdayToThursday;

        Assert.AreEqual(6, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SaturdayToWednesday" /> contains Saturday through Wednesday and excludes
    /// Thursday and Friday.
    /// </summary>
    [TestMethod]
    public void SaturdayToWednesday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet pattern = DayOfWeekSet.SaturdayToWednesday;

        Assert.AreEqual(5, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Saturday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SundayToFriday" /> contains Sunday through Friday and excludes Saturday.
    /// </summary>
    [TestMethod]
    public void SundayToFriday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet pattern = DayOfWeekSet.SundayToFriday;

        Assert.AreEqual(6, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Friday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Verifies that <see cref="DayOfWeekSet.SundayToThursday" /> contains Sunday through Thursday and excludes Friday and
    /// Saturday.
    /// </summary>
    [TestMethod]
    public void SundayToThursday_WhenAccessed_ShouldContainExpectedDays()
    {
        DayOfWeekSet pattern = DayOfWeekSet.SundayToThursday;

        Assert.AreEqual(5, pattern.Count);
        Assert.IsTrue(pattern.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Saturday));
    }

}
