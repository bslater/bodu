// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDaysOfWeekTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Bodu.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu;

[TestClass]
public class WorkingDaysOfWeekTests
{
    /// <summary>
    /// Provides the canonical mapping between <see cref="WorkingDaysOfWeek" /> values and their
    /// expected selected <see cref="DayOfWeek" /> sets.
    /// </summary>
    public static IEnumerable<object[]> GetNamedPresetTestData()
    {
        yield return new object[] { WorkingDaysOfWeek.MondayToFriday, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } };
        yield return new object[] { WorkingDaysOfWeek.MondayToSaturday, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday } };
        yield return new object[] { WorkingDaysOfWeek.MondayToThursdayAndSaturday, new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Saturday } };
        yield return new object[] { WorkingDaysOfWeek.SaturdayToThursday, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday } };
        yield return new object[] { WorkingDaysOfWeek.SaturdayToWednesday, new[] { DayOfWeek.Saturday, DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday } };
        yield return new object[] { WorkingDaysOfWeek.SundayToFriday, new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } };
        yield return new object[] { WorkingDaysOfWeek.SundayToThursday, new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday } };
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern" /> maps each named preset to a
    /// <see cref="WeekPattern" /> that selects exactly the expected days.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNamedPresetTestData), DynamicDataSourceType.Method)]
    public void ToWeekPattern_WhenNamedPreset_ShouldSelectExpectedDays(WorkingDaysOfWeek value, DayOfWeek[] expectedDays)
    {
        WeekPattern pattern = value.ToWeekPattern();

        Assert.AreEqual(expectedDays.Length, pattern.Count);
        foreach (DayOfWeek day in expectedDays)
            Assert.IsTrue(pattern.Contains(day), $"Expected pattern for {value} to include {day}.");
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern" /> throws
    /// <see cref="ArgumentException" /> when called with <see cref="WorkingDaysOfWeek.Custom" />.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenCustom_ShouldThrowExactlyArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = WorkingDaysOfWeek.Custom.ToWeekPattern();
        });
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWeekPattern" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the enum value is not defined.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenUndefinedEnumValue_ShouldThrowExactlyArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = ((WorkingDaysOfWeek)99).ToWeekPattern();
        });
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWorkingDaysOfWeek" /> round-trips every named
    /// preset back to its original <see cref="WorkingDaysOfWeek" /> value.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNamedPresetTestData), DynamicDataSourceType.Method)]
    public void ToWorkingDaysOfWeek_WhenRoundTripFromNamedPreset_ShouldReturnOriginal(WorkingDaysOfWeek value, DayOfWeek[] _)
    {
        WeekPattern pattern = value.ToWeekPattern();

        WorkingDaysOfWeek roundTripped = pattern.ToWorkingDaysOfWeek();

        Assert.AreEqual(value, roundTripped);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.ToWorkingDaysOfWeek" /> returns
    /// <see cref="WorkingDaysOfWeek.Custom" /> for a <see cref="WeekPattern" /> that does not match any named preset.
    /// </summary>
    [TestMethod]
    public void ToWorkingDaysOfWeek_WhenNotANamedPreset_ShouldReturnCustom()
    {
        WeekPattern oddPattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

        WorkingDaysOfWeek value = oddPattern.ToWorkingDaysOfWeek();

        Assert.AreEqual(WorkingDaysOfWeek.Custom, value);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.TryGetWorkingDaysOfWeek" /> returns
    /// <see langword="true" /> and the expected value for every named preset.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNamedPresetTestData), DynamicDataSourceType.Method)]
    public void TryGetWorkingDaysOfWeek_WhenNamedPreset_ShouldReturnTrueAndExpectedValue(WorkingDaysOfWeek value, DayOfWeek[] _)
    {
        WeekPattern pattern = value.ToWeekPattern();

        var success = pattern.TryGetWorkingDaysOfWeek(out WorkingDaysOfWeek actual);

        Assert.IsTrue(success);
        Assert.AreEqual(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="WorkingDaysOfWeekExtensions.TryGetWorkingDaysOfWeek" /> returns
    /// <see langword="false" /> and yields <see cref="WorkingDaysOfWeek.Custom" /> when no named preset matches.
    /// </summary>
    [TestMethod]
    public void TryGetWorkingDaysOfWeek_WhenNotANamedPreset_ShouldReturnFalseAndCustom()
    {
        WeekPattern oddPattern = new WeekPattern(DayOfWeek.Tuesday, DayOfWeek.Thursday);

        var success = oddPattern.TryGetWorkingDaysOfWeek(out WorkingDaysOfWeek value);

        Assert.IsFalse(success);
        Assert.AreEqual(WorkingDaysOfWeek.Custom, value);
    }
}
