// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomTriggerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="AdjustmentTrigger.Custom" /> trigger, including its registry dispatch, lenient behaviour when
/// no handler is registered, the context exposed to a handler, and its validation.
/// </summary>
[TestClass]
public sealed class CustomTriggerTests
{
    /// <summary>
    /// A resource whose holiday defers its firing decision to a custom trigger handler and shifts ten days when it fires.
    /// </summary>
    private const string CustomXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.custom-trigger">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="leap-shift" priority="100">
          <Trigger type="Custom" handlerKey="fire-in-leap" />
          <Action type="AddDays" days="10" />
          <Emission mode="ObservedOnly" reason="Leap-year shift" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="custom-trigger-holiday" displayName="Custom Trigger Holiday" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="March" day="1" /></Strategy><Adjustments><Adjustment policyRef="leap-shift" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A custom trigger handler that fires only when the occurrence falls in a Gregorian leap year.
    /// </summary>
    private sealed class FireInLeapYearHandler : IAdjustmentTriggerHandler
    {
        /// <inheritdoc />
        public bool ShouldAdjust(AdjustmentTriggerContext context) =>
            DateTime.IsLeapYear(context.BaseDate.Year);
    }

    /// <summary>
    /// A custom trigger handler that records every context it receives and never fires.
    /// </summary>
    private sealed class CapturingHandler : IAdjustmentTriggerHandler
    {
        /// <summary>Gets the territory and policy id observed across invocations.</summary>
        public HashSet<(string Territory, string PolicyId)> Seen { get; } = new();

        /// <summary>Gets the base dates observed across invocations.</summary>
        public List<DateOnly> SeenBaseDates { get; } = new();

        /// <inheritdoc />
        public bool ShouldAdjust(AdjustmentTriggerContext context)
        {
            Seen.Add((context.Territory, context.Policy.Id));
            SeenBaseDates.Add(context.BaseDate);
            return false;
        }
    }

    /// <summary>
    /// Resolves the single occurrence with the supplied id for a year over the supplied service.
    /// </summary>
    /// <param name="service">The service to query.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="id">The notable-date id.</param>
    /// <returns>The matching occurrence.</returns>
    private static NotableDate Single(INotableDateService service, int year, string id) =>
        service.Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX").Single(r => r.NotableDateId == id);

    /// <summary>
    /// Verifies that a Custom trigger whose handler reports firing applies the policy's action.
    /// </summary>
    [TestMethod]
    public void CustomTrigger_WhenHandlerReportsFire_ShouldApplyAction()
    {
        AdjustmentTriggerHandlerRegistry triggers = new AdjustmentTriggerHandlerRegistry().Register("fire-in-leap", new FireInLeapYearHandler());
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml), null, null, null, triggers);

        NotableDate match = Single(service, 2024, "custom-trigger-holiday");

        Assert.AreEqual(new DateOnly(2024, 3, 11), match.Date);
        Assert.AreEqual(new DateOnly(2024, 3, 1), match.ActualDate);
        Assert.IsTrue(match.IsObserved);
    }

    /// <summary>
    /// Verifies that a Custom trigger whose handler declines to fire leaves the occurrence on its calculated date.
    /// </summary>
    [TestMethod]
    public void CustomTrigger_WhenHandlerReportsNoFire_ShouldLeaveUnadjusted()
    {
        AdjustmentTriggerHandlerRegistry triggers = new AdjustmentTriggerHandlerRegistry().Register("fire-in-leap", new FireInLeapYearHandler());
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml), null, null, null, triggers);

        NotableDate match = Single(service, 2025, "custom-trigger-holiday");

        Assert.AreEqual(new DateOnly(2025, 3, 1), match.Date);
        Assert.IsFalse(match.IsObserved);
    }

    /// <summary>
    /// Verifies that a Custom trigger with no registered handler does not fire, leaving the occurrence unadjusted.
    /// </summary>
    [TestMethod]
    public void CustomTrigger_WhenHandlerNotRegistered_ShouldNotFire()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml));

        NotableDate match = Single(service, 2024, "custom-trigger-holiday");

        Assert.AreEqual(new DateOnly(2024, 3, 1), match.Date);
        Assert.IsFalse(match.IsObserved);
    }

    /// <summary>
    /// Verifies that the context passed to a Custom trigger handler carries the requested territory, the calculated base
    /// date, and the firing policy.
    /// </summary>
    [TestMethod]
    public void CustomTrigger_WhenEvaluated_ShouldExposeContext()
    {
        CapturingHandler handler = new();
        AdjustmentTriggerHandlerRegistry triggers = new AdjustmentTriggerHandlerRegistry().Register("fire-in-leap", handler);
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml), null, null, null, triggers);

        _ = Single(service, 2025, "custom-trigger-holiday");

        Assert.Contains(("XX", "leap-shift"), handler.Seen);
        Assert.Contains(new DateOnly(2025, 3, 1), handler.SeenBaseDates);
    }

    /// <summary>
    /// Verifies that a Custom trigger without a handler key fails validation.
    /// </summary>
    [TestMethod]
    public void CustomTrigger_WhenHandlerKeyMissing_ShouldFailValidation()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.bad-trigger">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="bad-trigger" priority="100">
              <Trigger type="Custom" />
              <Action type="None" />
              <Emission mode="ActualOnly" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="bad-trigger" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-TRIGGER-HANDLER-MISSING", ex.Diagnostics);
    }
}
