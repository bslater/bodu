// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Presets.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that <see cref="WeekPattern.MondayToFriday" /> is equal to <see cref="WeekPattern.Weekdays" />.
    /// </summary>
    [TestMethod]
    public void MondayToFriday_WhenAccessed_ShouldEqualWeekdays() => Assert.AreEqual(WeekPattern.Weekdays, WeekPattern.MondayToFriday);

    /// <summary>
    /// Verifies that <see cref="WeekPattern.MondayToSaturday" /> contains Monday through Saturday and excludes Sunday.
    /// </summary>
    [TestMethod]
    public void MondayToSaturday_WhenAccessed_ShouldContainMondayThroughSaturday()
    {
        WeekPattern pattern = WeekPattern.MondayToSaturday;

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
    /// Verifies that <see cref="WeekPattern.MondayToThursdayAndSaturday" /> contains Monday through Thursday and Saturday,
    /// excluding Friday and Sunday.
    /// </summary>
    [TestMethod]
    public void MondayToThursdayAndSaturday_WhenAccessed_ShouldContainExpectedDays()
    {
        WeekPattern pattern = WeekPattern.MondayToThursdayAndSaturday;

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
    /// Verifies that <see cref="WeekPattern.SaturdayToThursday" /> contains Saturday through Thursday and excludes Friday.
    /// </summary>
    [TestMethod]
    public void SaturdayToThursday_WhenAccessed_ShouldContainExpectedDays()
    {
        WeekPattern pattern = WeekPattern.SaturdayToThursday;

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
    /// Verifies that <see cref="WeekPattern.SaturdayToWednesday" /> contains Saturday through Wednesday and excludes
    /// Thursday and Friday.
    /// </summary>
    [TestMethod]
    public void SaturdayToWednesday_WhenAccessed_ShouldContainExpectedDays()
    {
        WeekPattern pattern = WeekPattern.SaturdayToWednesday;

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
    /// Verifies that <see cref="WeekPattern.SundayToFriday" /> contains Sunday through Friday and excludes Saturday.
    /// </summary>
    [TestMethod]
    public void SundayToFriday_WhenAccessed_ShouldContainExpectedDays()
    {
        WeekPattern pattern = WeekPattern.SundayToFriday;

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
    /// Verifies that <see cref="WeekPattern.SundayToThursday" /> contains Sunday through Thursday and excludes Friday and
    /// Saturday.
    /// </summary>
    [TestMethod]
    public void SundayToThursday_WhenAccessed_ShouldContainExpectedDays()
    {
        WeekPattern pattern = WeekPattern.SundayToThursday;

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
