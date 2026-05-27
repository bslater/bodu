// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.Enumeration.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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

    /// <summary>
    /// Verifies that <see cref="WeekPattern.GetEnumerator" /> returns the public struct type
    /// <see cref="WeekPattern.Enumerator" /> so that the C# <c>foreach</c> pattern binds to it directly and
    /// avoids the heap allocation that an interface-typed return would trigger when boxed.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenInvoked_ShouldReturnStructEnumeratorType()
    {
        WeekPattern pattern = WeekPattern.Weekdays;

        WeekPattern.Enumerator enumerator = pattern.GetEnumerator();

        Assert.IsTrue(typeof(WeekPattern.Enumerator).IsValueType);
        Assert.IsTrue(enumerator.MoveNext());
    }

    /// <summary>
    /// Verifies that the struct <see cref="WeekPattern.Enumerator" /> implements <see cref="IEnumerator{T}" /> so
    /// that callers that need an interface-typed enumerator can still box it explicitly.
    /// </summary>
    [TestMethod]
    public void Enumerator_ShouldImplementIEnumeratorOfDayOfWeek()
    {
        Assert.IsTrue(typeof(IEnumerator<DayOfWeek>).IsAssignableFrom(typeof(WeekPattern.Enumerator)));
        Assert.IsTrue(typeof(IDisposable).IsAssignableFrom(typeof(WeekPattern.Enumerator)));
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Enumerator.MoveNext" /> walks the selected days and that
    /// <see cref="WeekPattern.Enumerator.Current" /> reports the most recently yielded day. Drives the enumerator
    /// directly without using <c>foreach</c> so the contract is observed in isolation.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenWalkedManually_ShouldYieldExpectedDaysInOrder()
    {
        // 0b1010101 → Sun, Tue, Thu, Sat.
        var pattern = WeekPattern.FromByte(0b1010101);
        WeekPattern.Enumerator enumerator = pattern.GetEnumerator();
        var observed = new List<DayOfWeek>();

        while (enumerator.MoveNext())
            observed.Add(enumerator.Current);

        CollectionAssert.AreEqual(
            new[] { DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday },
            observed);
    }

    /// <summary>
    /// Verifies that <see cref="WeekPattern.Enumerator.Reset" /> rewinds the enumerator so a second walk yields
    /// the same sequence as the first.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetAfterWalk_ShouldRestartFromBeginning()
    {
        WeekPattern pattern = WeekPattern.Weekend;
        WeekPattern.Enumerator enumerator = pattern.GetEnumerator();

        List<DayOfWeek> first = DrainEnumerator(ref enumerator);
        enumerator.Reset();
        List<DayOfWeek> second = DrainEnumerator(ref enumerator);

        CollectionAssert.AreEqual(first, second);

        static List<DayOfWeek> DrainEnumerator(ref WeekPattern.Enumerator e)
        {
            var values = new List<DayOfWeek>();
            while (e.MoveNext())
                values.Add(e.Current);
            return values;
        }
    }
}
