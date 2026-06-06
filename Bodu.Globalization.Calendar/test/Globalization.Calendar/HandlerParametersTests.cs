// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HandlerParametersTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that author-supplied <c>Parameters</c> on an adjustment policy are parsed onto
/// <see cref="AdjustmentPolicy.HandlerParameters" /> and surfaced to custom action and trigger handlers through their
/// contexts, so one registered handler can be configured per policy by data.
/// </summary>
[TestClass]
public sealed class HandlerParametersTests
{
    private const string Territory = "XX";

    /// <summary>
    /// A resource that shifts New Year's Day by a parameterized number of days through a custom action handler.
    /// </summary>
    private const string ActionXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.b2a">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="shift-by-param">
          <Trigger type="Always" />
          <Action type="Custom" handlerKey="shift" />
          <Emission mode="ObservedOnly" reason="Shifted" />
          <Parameters>
            <Param key="days" value="5" />
          </Parameters>
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="new-year" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="shift-by-param" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A resource whose custom trigger fires only when a parameter opts in.
    /// </summary>
    private const string TriggerXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.b2t">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <AdjustmentPolicies>
        <AdjustmentPolicy id="conditional">
          <Trigger type="Custom" handlerKey="cond" />
          <Action type="AddDays" days="2" />
          <Emission mode="ObservedOnly" reason="Conditional" />
          <Parameters>
            <Param key="fire" value="yes" />
          </Parameters>
        </AdjustmentPolicy>
      </AdjustmentPolicies>
      <NotableDates>
        <NotableDate id="new-year" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
          <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy><Adjustments><Adjustment policyRef="conditional" /></Adjustments></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A custom action handler that shifts the base date by the policy's <c>days</c> parameter.
    /// </summary>
    private sealed class ShiftByParameterHandler : IAdjustmentHandler
    {
        /// <inheritdoc />
        public DateOnly? Adjust(AdjustmentHandlerContext context) =>
            context.Parameters.TryGetValue("days", out var value) && int.TryParse(value, out var days)
                ? context.BaseDate.AddDays(days)
                : null;
    }

    /// <summary>
    /// A custom trigger handler that fires only when its <c>fire</c> parameter equals <c>yes</c>.
    /// </summary>
    private sealed class ParameterTriggerHandler : IAdjustmentTriggerHandler
    {
        /// <inheritdoc />
        public bool ShouldAdjust(AdjustmentTriggerContext context) =>
            context.Parameters.TryGetValue("fire", out var value) && string.Equals(value, "yes", StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that a custom action handler reads the policy's parameters and shifts the observed date accordingly.
    /// </summary>
    [TestMethod]
    public void CustomActionHandler_ReceivesPolicyParameters()
    {
        AdjustmentHandlerRegistry handlers = new();
        handlers.Register("shift", new ShiftByParameterHandler());
        INotableDateService service = new NotableDateService(NotableDateResourceLoader.Load(ActionXml), null, null, handlers);

        NotableDate observed = service.Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)), Territory).Single();

        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2025, 1, 6), observed.Date);
    }

    /// <summary>
    /// Verifies that a custom trigger handler reads the policy's parameters to decide whether to fire.
    /// </summary>
    [TestMethod]
    public void CustomTriggerHandler_ReceivesPolicyParameters()
    {
        AdjustmentTriggerHandlerRegistry triggers = new();
        triggers.Register("cond", new ParameterTriggerHandler());
        INotableDateService service = new NotableDateService(NotableDateResourceLoader.Load(TriggerXml), null, null, null, triggers);

        NotableDate observed = service.Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)), Territory).Single();

        Assert.IsTrue(observed.IsObserved);
        Assert.AreEqual(new DateOnly(2025, 1, 3), observed.Date);
    }

    /// <summary>
    /// Verifies that declared parameters are parsed onto the policy model.
    /// </summary>
    [TestMethod]
    public void HandlerParameters_ParsedFromResource()
    {
        AdjustmentPolicy policy = NotableDateResourceLoader.Load(ActionXml).AdjustmentPolicies.Single();

        Assert.AreEqual("5", policy.HandlerParameters["days"]);
    }

    /// <summary>
    /// Verifies that a policy with no declared parameters exposes an empty, non-null parameter map.
    /// </summary>
    [TestMethod]
    public void HandlerParameters_WhenNoneDeclared_ShouldBeEmpty()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.b2e">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="plain">
              <Trigger type="IfWeekend" />
              <Action type="MoveToNextWorkingDay" />
              <Emission mode="ObservedOnly" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="n" displayName="N" category="PublicHoliday">
              <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        AdjustmentPolicy policy = NotableDateResourceLoader.Load(xml).AdjustmentPolicies.Single();

        Assert.AreEqual(0, policy.HandlerParameters.Count);
    }
}
