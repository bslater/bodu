// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAdjustmentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the <see cref="AdjustmentAction.ReplaceWithRule" /> reference action and the
/// <see cref="AdjustmentAction.Custom" /> handler action, including their validation and runtime behaviour.
/// </summary>
[TestClass]
public sealed class CustomAdjustmentTests
{
    /// <summary>
    /// A resource whose moved holiday replaces its date with the occurrence of an anchor concept.
    /// </summary>
    private const string ReplaceXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.replace">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="replace-with-anchor" priority="100">
          <Trigger type="Always" />
          <Action type="ReplaceWithRule" notableDateRef="anchor-day" />
          <Emission mode="ObservedOnly" reason="Moved to anchor" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="anchor-day" displayName="Anchor Day" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="10" /></Strategy></Rule></Rules>
        </NotableDate>
        <NotableDate id="moved-holiday" displayName="Moved Holiday" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="replace-with-anchor" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A resource whose holiday defers its observed date to a custom handler.
    /// </summary>
    private const string CustomXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.custom-action">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="shift-ten" priority="100">
          <Trigger type="Always" />
          <Action type="Custom" handlerKey="shift-ten" />
          <Emission mode="ObservedOnly" reason="Custom shift" />
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="custom-holiday" displayName="Custom Holiday" category="PublicHoliday" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Fixed month="March" day="1" /></Strategy><Adjustments><Adjustment policyRef="shift-ten" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A custom handler that shifts an occurrence forward by ten days.
    /// </summary>
    private sealed class ShiftTenHandler : IAdjustmentHandler
    {
        /// <inheritdoc />
        public DateOnly? Adjust(AdjustmentHandlerContext context) =>
            context.BaseDate.AddDays(10);
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
    /// Verifies that a ReplaceWithRule action moves the occurrence to the referenced rule's date for the same year while
    /// preserving the calculated date as the actual date.
    /// </summary>
    [TestMethod]
    public void ReplaceWithRule_WhenReferenceResolves_ShouldMoveToReferencedDate()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(ReplaceXml));

        NotableDate match = Single(service, 2025, "moved-holiday");

        Assert.AreEqual(new DateOnly(2025, 1, 10), match.Date);
        Assert.AreEqual(new DateOnly(2025, 1, 1), match.ActualDate);
        Assert.IsTrue(match.IsObserved);
    }

    /// <summary>
    /// Verifies that a ReplaceWithRule action without a notable-date reference fails validation.
    /// </summary>
    [TestMethod]
    public void ReplaceWithRule_WhenReferenceMissing_ShouldFailValidation()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.bad-replace">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="bad-replace" priority="100">
              <Trigger type="Always" />
              <Action type="ReplaceWithRule" />
              <Emission mode="ObservedOnly" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="bad-replace" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-REPLACE-MISSING", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a ReplaceWithRule action whose reference resolves to no rule fails validation.
    /// </summary>
    [TestMethod]
    public void ReplaceWithRule_WhenReferenceNotFound_ShouldFailValidation()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.unknown-replace">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="unknown-replace" priority="100">
              <Trigger type="Always" />
              <Action type="ReplaceWithRule" notableDateRef="does-not-exist" />
              <Emission mode="ObservedOnly" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="unknown-replace" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-REPLACE-MISSING", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a Custom action dispatches to the registered handler and emits the handler's observed date.
    /// </summary>
    [TestMethod]
    public void CustomAction_WhenHandlerRegistered_ShouldUseHandlerDate()
    {
        AdjustmentHandlerRegistry handlers = new AdjustmentHandlerRegistry().Register("shift-ten", new ShiftTenHandler());
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml), null, null, handlers);

        NotableDate match = Single(service, 2025, "custom-holiday");

        Assert.AreEqual(new DateOnly(2025, 3, 11), match.Date);
        Assert.AreEqual(new DateOnly(2025, 3, 1), match.ActualDate);
        Assert.IsTrue(match.IsObserved);
    }

    /// <summary>
    /// Verifies that a Custom action with no registered handler leaves the occurrence on its calculated date.
    /// </summary>
    [TestMethod]
    public void CustomAction_WhenHandlerNotRegistered_ShouldLeaveDateUnchanged()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml));

        NotableDate match = Single(service, 2025, "custom-holiday");

        Assert.AreEqual(new DateOnly(2025, 3, 1), match.Date);
        Assert.AreEqual(new DateOnly(2025, 3, 1), match.ActualDate);
    }

    /// <summary>
    /// Verifies that a Custom action without a handler key fails validation.
    /// </summary>
    [TestMethod]
    public void CustomAction_WhenHandlerKeyMissing_ShouldFailValidation()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.bad-custom">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="bad-custom" priority="100">
              <Trigger type="Always" />
              <Action type="Custom" />
              <Emission mode="ObservedOnly" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="bad-custom" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-HANDLER-MISSING", ex.Diagnostics);
    }
}
