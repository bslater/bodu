// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonWorkingDayTriggerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies the <see cref="AdjustmentTrigger.IfNonWorkingDay" /> and <see cref="AdjustmentTrigger.IfWorkingDay" />
/// triggers, which extend the weekend triggers by also accounting for a day already claimed by another non-working
/// occurrence. The fixture anchors each example on a distinct day in January 2026 (1 Jan Thu, 3 Jan Sat, 8 Jan Thu,
/// 10 Jan Sat, 15 Jan Thu) so the cases never interfere.
/// </summary>
[TestClass]
public sealed class NonWorkingDayTriggerTests
{
    private const string Territory = "ZZ";

    /// <summary>
    /// A fixture exercising every non-working-day and working-day trigger branch under the default Monday-to-Friday
    /// working week.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.nonworkingday">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="non-working-next-working-day" priority="100">
          <Trigger type="IfNonWorkingDay" />
          <Action type="MoveToNextWorkingDay" maxSearchDays="7" skipWeekends="true" skipNonWorkingDates="true" />
          <Emission mode="ObservedOnly" reason="Observed next working day" />
        </AdjustmentPolicy>
        <AdjustmentPolicy id="working-day-shift-two" priority="100">
          <Trigger type="IfWorkingDay" />
          <Action type="AddDays" days="2" />
          <Emission mode="ObservedOnly" reason="Shifted on working day" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="collide-a" displayName="Collide A" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-1"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="1" /></Strategy></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="collide-b" displayName="Collide B" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-1"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="1" /></Strategy>
              <Adjustments><Adjustment policyRef="non-working-next-working-day" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="weekend-holiday" displayName="Weekend Holiday" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-3"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="3" /></Strategy>
              <Adjustments><Adjustment policyRef="non-working-next-working-day" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="lone-working" displayName="Lone Working" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-8"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="8" /></Strategy>
              <Adjustments><Adjustment policyRef="non-working-next-working-day" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="working-shift" displayName="Working Shift" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-15"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="15" /></Strategy>
              <Adjustments><Adjustment policyRef="working-day-shift-two" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
        <NotableDate id="weekend-no-shift" displayName="Weekend No Shift" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules>
            <Rule id="jan-10"><Applicability><Territory code="ZZ" /></Applicability>
              <Strategy><Fixed month="January" day="10" /></Strategy>
              <Adjustments><Adjustment policyRef="working-day-shift-two" /></Adjustments></Rule>
          </Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// Builds a service over the fixture.
    /// </summary>
    /// <returns>A service over the fixture.</returns>
    private static INotableDateService Build() =>
        new NotableDateService(NotableDateResourceLoader.Load(Xml));

    /// <summary>
    /// Verifies that a non-working holiday colliding with another non-working holiday on a working day fires the
    /// non-working-day trigger and is observed on the next free working day.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenCollidingWithAnotherHoliday_ShouldObserveOnNextWorkingDay()
    {
        INotableDateService service = Build();

        // Collide A keeps the contested Thursday; Collide B does not appear on it.
        IReadOnlyList<NotableDate> thursday = service.Resolve(new DateOnly(2026, 1, 1), Territory);
        Assert.IsTrue(thursday.Any(n => n.NotableDateId == "collide-a" && !n.IsObserved), "Collide A on its actual day");
        Assert.IsFalse(thursday.Any(n => n.NotableDateId == "collide-b"), "Collide B moved off the contested day");

        // Collide B is observed on the next working day (Friday 2 January).
        IReadOnlyList<NotableDate> friday = service.Resolve(new DateOnly(2026, 1, 2), Territory);
        NotableDate observed = friday.Single(n => n.NotableDateId == "collide-b");
        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 1), observed.ActualDate);
    }

    /// <summary>
    /// Verifies that the non-working-day trigger fires for a holiday falling on a weekend and moves it to the next
    /// working day, skipping the weekend.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenHolidayFallsOnWeekend_ShouldObserveOnNextWorkingDay()
    {
        INotableDateService service = Build();

        Assert.IsFalse(
            service.Resolve(new DateOnly(2026, 1, 3), Territory).Any(n => n.NotableDateId == "weekend-holiday"),
            "Saturday actual suppressed");

        NotableDate observed = service.Resolve(new DateOnly(2026, 1, 5), Territory).Single(n => n.NotableDateId == "weekend-holiday");
        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 3), observed.ActualDate);
    }

    /// <summary>
    /// Verifies that the non-working-day trigger does not fire for a lone holiday on a working day, so the holiday is
    /// emitted on its actual date.
    /// </summary>
    [TestMethod]
    public void IfNonWorkingDay_WhenLoneHolidayOnWorkingDay_ShouldEmitActualDate()
    {
        NotableDate actual = Build().Resolve(new DateOnly(2026, 1, 8), Territory).Single(n => n.NotableDateId == "lone-working");

        Assert.IsFalse(actual.IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 8), actual.Date);
    }

    /// <summary>
    /// Verifies that the working-day trigger fires for a lone holiday on a working day and shifts it by the configured
    /// number of days.
    /// </summary>
    [TestMethod]
    public void IfWorkingDay_WhenHolidayOnWorkingDay_ShouldShiftOccurrence()
    {
        INotableDateService service = Build();

        Assert.IsFalse(
            service.Resolve(new DateOnly(2026, 1, 15), Territory).Any(n => n.NotableDateId == "working-shift"),
            "actual Thursday suppressed by observed-only");

        NotableDate observed = service.Resolve(new DateOnly(2026, 1, 17), Territory).Single(n => n.NotableDateId == "working-shift");
        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 15), observed.ActualDate);
    }

    /// <summary>
    /// Verifies that the working-day trigger does not fire for a holiday on a weekend, so the holiday is emitted on its
    /// actual date.
    /// </summary>
    [TestMethod]
    public void IfWorkingDay_WhenHolidayOnWeekend_ShouldEmitActualDate()
    {
        NotableDate actual = Build().Resolve(new DateOnly(2026, 1, 10), Territory).Single(n => n.NotableDateId == "weekend-no-shift");

        Assert.IsFalse(actual.IsObserved);
        Assert.AreEqual(new DateOnly(2026, 1, 10), actual.Date);
    }
}
