// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Enumeration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu;

public partial class WeekPatternTests
{

    /// <summary>
    /// Verifies that the generic <see cref="WeekPattern.GetEnumerator" /> yields each selected
    /// <see cref="System.DayOfWeek" /> in Sunday-first order for a known bitmask.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAllDaysSelected_ShouldYieldEveryDayInSundayFirstOrder()
    {
        var pattern = WeekPattern.FromByte(0b1111111);

        var days = new List<DayOfWeek>();
        foreach (DayOfWeek day in pattern)
            days.Add(day);

        CollectionAssert.AreEqual(
            new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
            days);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.GetEnumerator" /> yields no values when the pattern is empty.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenPatternIsEmpty_ShouldYieldNothing()
    {
        var pattern = WeekPattern.FromByte(0);
        using IEnumerator<DayOfWeek> enumerator = pattern.GetEnumerator();
        Assert.IsFalse(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.GetEnumerator" /> yields only the selected days when the pattern is sparse.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenWeekdaysOnly_ShouldYieldMondayThroughFriday()
    {
        // 0b0111110 in Sunday-first order: Sunday=0, Monday=1, ..., Saturday=0 → Mon–Fri selected.
        var pattern = WeekPattern.FromByte(0b0111110);

        var days = new List<DayOfWeek>();
        foreach (DayOfWeek day in pattern)
            days.Add(day);

        CollectionAssert.AreEqual(
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
            days);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable.GetEnumerator" /> on a <see cref="WeekPattern" /> walks the same
    /// <see cref="DayOfWeek" /> values as the generic enumerator.
    /// </summary>
    [TestMethod]
    public void NonGenericGetEnumerator_WhenPatternHasDays_ShouldYieldSameSequence()
    {
        var pattern = WeekPattern.FromByte(0b1010101);

        IEnumerable nonGeneric = pattern;
        var observed = new List<DayOfWeek>();
        foreach (var day in nonGeneric)
            observed.Add((DayOfWeek)day);

        // 0b1010101 in Sunday-first order: Sun=1, Mon=0, Tue=1, Wed=0, Thu=1, Fri=0, Sat=1.
        CollectionAssert.AreEqual(
            new[] { DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday },
            observed);
    }

}
