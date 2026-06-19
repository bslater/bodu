// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.None.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentActionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.None" /> leaves the occurrence date unchanged even though the trigger
    /// fires; under an actual-only emission the single emitted occurrence is the unchanged actual date with no policy id.
    /// </summary>
    [TestMethod]
    public void None_WhenAlwaysTriggerAndActualOnly_ShouldEmitUnchangedActual()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.none-action">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="noop" priority="100">
              <Trigger type="Always" />
              <Action type="None" />
              <Emission mode="ActualOnly" reason="No-op" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="probe" displayName="Probe" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="July" day="1" /></Strategy><Adjustments><Adjustment policyRef="noop" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        NotableDate match = Single(service, "probe", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 5));

        Assert.AreEqual(
            (new DateOnly(2026, 7, 1), false, (string?)null),
            (match.Date, match.IsObserved, match.AdjustmentPolicyId));
    }
}
