// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarWeekendDefinitionExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

[TestClass]
public class CalendarWeekendDefinitionExtensionsTests
{

    /// <summary>
    /// Provides the canonical mapping between <see cref="CalendarWeekendDefinition" /> values and the working-week
    /// <see cref="WeekPattern" /> they imply.
    /// </summary>
    public static IEnumerable<object[]> GetWeekendToWorkingWeekTestData()
    {
        yield return new object[] { CalendarWeekendDefinition.SaturdaySunday, WeekPattern.MondayToFriday };
        yield return new object[] { CalendarWeekendDefinition.FridaySaturday, WeekPattern.SundayToThursday };
        yield return new object[] { CalendarWeekendDefinition.ThursdayFriday, WeekPattern.SaturdayToWednesday };
        yield return new object[] { CalendarWeekendDefinition.FridayOnly, WeekPattern.SaturdayToThursday };
        yield return new object[] { CalendarWeekendDefinition.SundayOnly, WeekPattern.MondayToSaturday };
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekendDefinitionExtensions.ToCalendarWeekendDefinition" /> returns
    /// <see cref="CalendarWeekendDefinition.None" /> for a <see cref="WeekPattern" /> with every day selected.
    /// </summary>
    [TestMethod]
    public void ToCalendarWeekendDefinition_WhenAllDays_ShouldReturnNone()
    {
        WeekPattern allDays = ~WeekPattern.Empty;

        Assert.AreEqual(CalendarWeekendDefinition.None, allDays.ToCalendarWeekendDefinition());
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekendDefinitionExtensions.ToCalendarWeekendDefinition" /> returns
    /// <see cref="CalendarWeekendDefinition.Custom" /> when the supplied <see cref="WeekPattern" /> does not match any
    /// built-in definition.
    /// </summary>
    [TestMethod]
    public void ToCalendarWeekendDefinition_WhenNonStandardPattern_ShouldReturnCustom()
    {
        var oddPattern = new WeekPattern(DayOfWeek.Tuesday, DayOfWeek.Thursday);

        Assert.AreEqual(CalendarWeekendDefinition.Custom, oddPattern.ToCalendarWeekendDefinition());
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekendDefinitionExtensions.ToCalendarWeekendDefinition" /> round-trips every
    /// built-in mapping.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetWeekendToWorkingWeekTestData))]
    public void ToCalendarWeekendDefinition_WhenRoundTripFromBuiltIn_ShouldReturnOriginal(CalendarWeekendDefinition weekend, WeekPattern workingWeek)
    {
        var roundTripped = workingWeek.ToCalendarWeekendDefinition();

        Assert.AreEqual(weekend, roundTripped);
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekendDefinitionExtensions.ToWeekPattern" /> maps every built-in weekend
    /// definition to the expected working-week <see cref="WeekPattern" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetWeekendToWorkingWeekTestData))]
    public void ToWeekPattern_WhenBuiltInWeekend_ShouldReturnExpectedPattern(CalendarWeekendDefinition weekend, WeekPattern expected)
    {
        var actual = weekend.ToWeekPattern();

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekendDefinitionExtensions.ToWeekPattern" /> throws
    /// <see cref="ArgumentException" /> when <see cref="CalendarWeekendDefinition.Custom" /> is supplied without a
    /// provider.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenCustomAndProviderIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = CalendarWeekendDefinition.Custom.ToWeekPattern();
        });
    }

    /// <summary>
    /// Verifies that <see cref="CalendarWeekendDefinitionExtensions.ToWeekPattern" /> returns a
    /// <see cref="WeekPattern" /> with all seven days selected when called with
    /// <see cref="CalendarWeekendDefinition.None" />.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenNone_ShouldReturnAllDays()
    {
        var actual = CalendarWeekendDefinition.None.ToWeekPattern();

        Assert.AreEqual(7, actual.Count);
    }

    /// <summary>
    /// Verifies that the <see cref="IWeekendDefinitionProvider" /> overload throws
    /// <see cref="ArgumentNullException" /> when the provider is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        IWeekendDefinitionProvider? provider = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CalendarWeekendDefinitionExtensions.ToWeekPattern(provider!);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="IWeekendDefinitionProvider" /> overload of
    /// <see cref="CalendarWeekendDefinitionExtensions.ToWeekPattern(IWeekendDefinitionProvider)" /> includes the
    /// non-weekend days returned by the provider in the resulting <see cref="WeekPattern" /> — exercising both
    /// the included and excluded branches of the inner loop.
    /// </summary>
    [TestMethod]
    public void ToWeekPattern_WhenProviderReturnsMixedWeekendMask_ShouldIncludeNonWeekendDays()
    {
        // Provider treats Friday and Saturday as the weekend; all others are working days.
        IWeekendDefinitionProvider provider = new TestWeekendProvider(DayOfWeek.Friday, DayOfWeek.Saturday);

        WeekPattern pattern = provider.ToWeekPattern();

        Assert.IsTrue(pattern.Contains(DayOfWeek.Sunday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Monday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Tuesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Wednesday));
        Assert.IsTrue(pattern.Contains(DayOfWeek.Thursday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Friday));
        Assert.IsFalse(pattern.Contains(DayOfWeek.Saturday));
    }

    /// <summary>
    /// Lightweight test provider whose <see cref="IsWeekend(DayOfWeek)" /> simply consults a configured
    /// <see cref="HashSet{T}" /> of weekend days.
    /// </summary>
    private sealed class TestWeekendProvider : IWeekendDefinitionProvider
    {

        private readonly HashSet<DayOfWeek> _weekendDays;

        public TestWeekendProvider(params DayOfWeek[] weekendDays)
        {
            _weekendDays = new HashSet<DayOfWeek>(weekendDays);
        }

        public bool IsWeekend(DayOfWeek dayOfWeek) => _weekendDays.Contains(dayOfWeek);

    }

}
