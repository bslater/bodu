// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAdjustmentTests.CustomAction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class CustomAdjustmentTests
{
    /// <summary>
    /// Verifies that a Custom action dispatches to the registered handler and emits the handler's observed date.
    /// </summary>
    [TestMethod]
    public void CustomAction_WhenHandlerRegistered_ShouldUseHandlerDate()
    {
        AdjustmentHandlerRegistry handlers = new AdjustmentHandlerRegistry().Register("shift-ten", new ShiftTenHandler());
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml), new NotableDateServiceOptions { Handlers = handlers });

        NotableDate match = Single(service, 2025, "custom-holiday");

        Assert.AreEqual(
            (new DateOnly(2025, 3, 11), (DateOnly?)new DateOnly(2025, 3, 1), true),
            (match.Date, match.ActualDate, match.IsObserved));
    }

    /// <summary>
    /// Verifies that a Custom action with no registered handler leaves the occurrence on its calculated date.
    /// </summary>
    [TestMethod]
    public void CustomAction_WhenHandlerNotRegistered_ShouldLeaveDateUnchanged()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(CustomXml));

        NotableDate match = Single(service, 2025, "custom-holiday");

        Assert.AreEqual(
            (new DateOnly(2025, 3, 1), (DateOnly?)new DateOnly(2025, 3, 1)),
            (match.Date, match.ActualDate));
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
