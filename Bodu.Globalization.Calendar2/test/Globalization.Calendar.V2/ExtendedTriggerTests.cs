// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExtendedTriggerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the leap-year, before/after-fixed-date, and nth-occurrence-in-month adjustment triggers.
/// </summary>
[TestClass]
public sealed class ExtendedTriggerTests
{
    /// <summary>
    /// A resource exercising each extended trigger through an observed-only adjustment.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.triggers">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="leap-back" priority="100">
          <Trigger type="IfLeapYear" />
          <Action type="AddDays" days="-1" />
          <Emission mode="ObservedOnly" reason="Leap year" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="before-feb" priority="100">
          <Trigger type="IfBeforeFixedDate" month="February" day="1" />
          <Action type="AddDays" days="5" />
          <Emission mode="ObservedOnly" reason="Before February" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="after-oct" priority="100">
          <Trigger type="IfAfterFixedDate" month="October" day="1" />
          <Action type="AddDays" days="3" />
          <Emission mode="ObservedOnly" reason="After October" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="nth-mon" priority="100">
          <Trigger type="IfNthOccurrenceInMonth" weekOrdinal="Second"><Weekday value="Monday" /></Trigger>
          <Action type="AddDays" days="1" />
          <Emission mode="ObservedOnly" reason="Second Monday" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="march-first" displayName="March First" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="March" day="1" /></Strategy><Adjustments><Adjustment policyRef="leap-back" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="jan-fifteen" displayName="Jan Fifteen" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="15" /></Strategy><Adjustments><Adjustment policyRef="before-feb" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="nov-fifteen" displayName="Nov Fifteen" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="November" day="15" /></Strategy><Adjustments><Adjustment policyRef="after-oct" /></Adjustments></Rule></Rules>
        </NotableDate>
        <NotableDate id="second-monday" displayName="Second Monday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><DayOfWeekInMonth month="June" dayOfWeek="Monday" weekOrdinal="Second" /></Strategy><Adjustments><Adjustment policyRef="nth-mon" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Gets a service over the trigger fixture.
    /// </summary>
    private static INotableDateService Service =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

    /// <summary>
    /// Resolves the single occurrence with the supplied id for a year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="id">The notable-date id.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(int year, string id) =>
        Service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX").Single(r => r.NotableDateId == id);

    /// <summary>
    /// Verifies that the leap-year trigger fires only in leap years.
    /// </summary>
    [TestMethod]
    public void IfLeapYear_FiresOnlyInLeapYears()
    {
        NotableDate leap = Single(2024, "march-first");
        Assert.IsTrue(leap.IsObserved);
        Assert.AreEqual(new DateOnly(2024, 2, 29), leap.Date);

        NotableDate common = Single(2025, "march-first");
        Assert.IsFalse(common.IsObserved);
        Assert.AreEqual(new DateOnly(2025, 3, 1), common.Date);
    }

    /// <summary>
    /// Verifies that the before-fixed-date trigger fires for an occurrence earlier than the comparison date.
    /// </summary>
    [TestMethod]
    public void IfBeforeFixedDate_FiresWhenEarlier()
    {
        NotableDate match = Single(2025, "jan-fifteen");
        Assert.IsTrue(match.IsObserved);
        Assert.AreEqual(new DateOnly(2025, 1, 20), match.Date);
    }

    /// <summary>
    /// Verifies that the after-fixed-date trigger fires for an occurrence later than the comparison date.
    /// </summary>
    [TestMethod]
    public void IfAfterFixedDate_FiresWhenLater()
    {
        NotableDate match = Single(2025, "nov-fifteen");
        Assert.IsTrue(match.IsObserved);
        Assert.AreEqual(new DateOnly(2025, 11, 18), match.Date);
    }

    /// <summary>
    /// Verifies that the nth-occurrence trigger fires when the occurrence is the configured nth weekday of its month.
    /// The second Monday of June 2025 is 9 June.
    /// </summary>
    [TestMethod]
    public void IfNthOccurrenceInMonth_FiresOnTheConfiguredOrdinal()
    {
        NotableDate match = Single(2025, "second-monday");
        Assert.IsTrue(match.IsObserved);
        Assert.AreEqual(new DateOnly(2025, 6, 10), match.Date);
    }
}
