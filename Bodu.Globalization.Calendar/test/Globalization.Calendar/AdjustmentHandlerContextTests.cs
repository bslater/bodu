// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentHandlerContextTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that a custom adjustment handler can read the territory, resolution context, and occupancy probe exposed by
/// <see cref="AdjustmentHandlerContext" />.
/// </summary>
[TestClass]
public class AdjustmentHandlerContextTests
{
    private const string Xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.handler-context">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="probe">
              <Trigger type="Always" />
              <Action type="Custom" handlerKey="probe" />
              <Emission mode="ObservedOnly" reason="Probed" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="new-year" displayName="New Year's Day" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules>
                <Rule id="x">
                  <Strategy><Fixed month="January" day="1" /></Strategy>
                  <Adjustments><Adjustment policyRef="probe" /></Adjustments>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// A handler that reads the territory, resolution context, and occupancy probe before shifting two days forward.
    /// </summary>
    private sealed class ContextProbingHandler
        : IAdjustmentHandler
    {
        /// <inheritdoc />
        public DateOnly? Adjust(AdjustmentHandlerContext context)
        {
            Assert.IsFalse(string.IsNullOrEmpty(context.Territory));
            Assert.IsNotNull(context.ResolutionContext);
            _ = context.IsOccupied(context.BaseDate);

            return context.BaseDate.AddDays(2);
        }
    }

    /// <summary>
    /// Verifies that a custom handler observing the context produces the shifted observed date.
    /// </summary>
    [TestMethod]
    public void CustomHandler_ShouldReadContextAndProduceObservedDate()
    {
        var handlers = new AdjustmentHandlerRegistry();
        handlers.Register("probe", new ContextProbingHandler());
        var service = new NotableDateService(NotableDateResourceLoader.Load(Xml), new NotableDateServiceOptions { Handlers = handlers });

        NotableDate observed = service
            .Resolve(new DateRange(new DateOnly(2025, 1, 1), new DateOnly(2025, 1, 10)), "XX")
            .Single();

        Assert.AreEqual(new DateOnly(2025, 1, 3), observed.Date);
    }
}
