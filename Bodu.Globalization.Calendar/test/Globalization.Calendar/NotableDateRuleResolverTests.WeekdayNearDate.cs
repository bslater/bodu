// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolverTests.WeekdayNearDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the behaviour of <see cref="NotableDateRuleResolver" /> for the
/// <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy — the target weekday positioned on or after, on or
/// before, or nearest to a fixed reference month and day.
/// </summary>
[TestClass]
public sealed class NotableDateRuleResolverWeekdayNearDateTests
{
    private static NotableDateRule WeekdayRule(string name, DayOfWeek dayOfWeek, int month, int day, WeekdayProximity proximity) =>
        new()
        {
            Name = name,
            Strategy = DateResolutionStrategy.WeekdayNearDate,
            Category = NotableDateCategory.Holiday,
            DayOfWeek = dayOfWeek,
            Month = month,
            Day = day,
            WeekdayProximity = proximity,
        };

    private static DateTime? Resolve(NotableDateRule rule, int year) =>
        new NotableDateRuleResolver([rule]).ResolveAnchorDate(rule, year);

    /// <summary>
    /// Verifies that an <see cref="WeekdayProximity.OnOrAfter" /> rule advances forward to the first matching weekday
    /// after the reference date (Nordic Midsummer Day 2024 — the Saturday on or after 20 June — is 22 June).
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenOnOrAfterAndReferenceBeforeTarget_ShouldAdvanceForward()
    {
        NotableDateRule rule = WeekdayRule("Midsummer Day", DayOfWeek.Saturday, 6, 20, WeekdayProximity.OnOrAfter);

        Assert.AreEqual(new DateTime(2024, 6, 22), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that an <see cref="WeekdayProximity.OnOrAfter" /> rule returns the reference date itself when it already
    /// falls on the target weekday (20 June 2025 is not a Saturday, but 21 June 2025 is, so the rule lands on it
    /// directly in that year's window).
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenOnOrAfterAndReferenceIsTargetWeekday_ShouldReturnReference()
    {
        // 21 June 2025 is a Saturday; a Saturday-on-or-after-21-June rule resolves to 21 June itself.
        NotableDateRule rule = WeekdayRule("Saturday Anchor", DayOfWeek.Saturday, 6, 21, WeekdayProximity.OnOrAfter);

        Assert.AreEqual(new DateTime(2025, 6, 21), Resolve(rule, 2025));
    }

    /// <summary>
    /// Verifies that an <see cref="WeekdayProximity.OnOrBefore" /> rule retreats backward to the most recent matching
    /// weekday on or before the reference date (German Repentance Day 2024 — the Wednesday on or before 22 November — is
    /// 20 November).
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenOnOrBeforeAndReferenceAfterTarget_ShouldRetreatBackward()
    {
        NotableDateRule rule = WeekdayRule("Repentance Day", DayOfWeek.Wednesday, 11, 22, WeekdayProximity.OnOrBefore);

        Assert.AreEqual(new DateTime(2024, 11, 20), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that an <see cref="WeekdayProximity.OnOrBefore" /> rule returns the reference date itself when it
    /// already falls on the target weekday (20 November 2024 is a Wednesday).
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenOnOrBeforeAndReferenceIsTargetWeekday_ShouldReturnReference()
    {
        NotableDateRule rule = WeekdayRule("Wednesday Anchor", DayOfWeek.Wednesday, 11, 20, WeekdayProximity.OnOrBefore);

        Assert.AreEqual(new DateTime(2024, 11, 20), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that a <see cref="WeekdayProximity.Nearest" /> rule selects the forward occurrence when it is closer
    /// (the Monday nearest to 1 November 2024, a Friday, is 4 November — three days forward versus four days back).
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenNearestAndForwardIsCloser_ShouldSelectForward()
    {
        NotableDateRule rule = WeekdayRule("Nearest Monday", DayOfWeek.Monday, 11, 1, WeekdayProximity.Nearest);

        Assert.AreEqual(new DateTime(2024, 11, 4), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that a <see cref="WeekdayProximity.Nearest" /> rule selects the backward occurrence when it is closer
    /// (the Monday nearest to 6 November 2024, a Wednesday, is 4 November — two days back versus five days forward).
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenNearestAndBackwardIsCloser_ShouldSelectBackward()
    {
        NotableDateRule rule = WeekdayRule("Nearest Monday", DayOfWeek.Monday, 11, 6, WeekdayProximity.Nearest);

        Assert.AreEqual(new DateTime(2024, 11, 4), Resolve(rule, 2024));
    }

    /// <summary>
    /// Verifies that a rule whose reference (year, month, day) is not a valid Gregorian date resolves to
    /// <see langword="null" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void ResolveAnchorDate_WhenReferenceDateInvalid_ShouldReturnNull()
    {
        NotableDateRule rule = WeekdayRule("Invalid Reference", DayOfWeek.Monday, 2, 30, WeekdayProximity.OnOrAfter);

        Assert.IsNull(Resolve(rule, 2024));
    }
}
