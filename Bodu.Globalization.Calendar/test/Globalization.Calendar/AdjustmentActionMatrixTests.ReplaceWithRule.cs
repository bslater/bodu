// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdjustmentActionMatrixTests.ReplaceWithRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class AdjustmentActionMatrixTests
{
    /// <summary>
    /// Verifies that <see cref="AdjustmentAction.ReplaceWithRule" /> replaces the occurrence date with the referenced
    /// concept's occurrence for the same year, preserving the calculated date as the actual date.
    /// </summary>
    [TestMethod]
    public void ReplaceWithRule_WhenReferenceResolves_ShouldUseReferencedDate()
    {
        const string xml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.replace-action">
          <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
          <AdjustmentPolicies>
            <AdjustmentPolicy id="replace" priority="100">
              <Trigger type="Always" />
              <Action type="ReplaceWithRule" notableDateRef="target" />
              <Emission mode="ObservedOnly" reason="Replaced" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="target" displayName="Target" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="July" day="15" /></Strategy></Rule></Rules>
            </NotableDate>
            <NotableDate id="replacement" displayName="Replacement" category="PublicHoliday" defaultNonWorkingDay="true">
              <Rules><Rule id="x"><Strategy><Fixed month="July" day="1" /></Strategy><Adjustments><Adjustment policyRef="replace" /></Adjustments></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

        NotableDateService service = new(NotableDateResourceLoader.Load(xml));

        NotableDate match = Single(service, "replacement", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));

        Assert.AreEqual(
            (new DateOnly(2026, 7, 15), (DateOnly?)new DateOnly(2026, 7, 1), true),
            (match.Date, match.ActualDate, match.IsObserved));
    }
}
