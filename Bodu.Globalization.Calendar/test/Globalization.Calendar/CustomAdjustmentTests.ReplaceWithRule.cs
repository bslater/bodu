// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAdjustmentTests.ReplaceWithRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class CustomAdjustmentTests
{
    /// <summary>
    /// Verifies that a ReplaceWithRule action moves the occurrence to the referenced rule's date for the same year while
    /// preserving the calculated date as the actual date.
    /// </summary>
    [TestMethod]
    public void ReplaceWithRule_WhenReferenceResolves_ShouldMoveToReferencedDate()
    {
        NotableDateService service = new(NotableDateResourceLoader.Load(ReplaceXml));

        NotableDate match = Single(service, 2025, "moved-holiday");

        Assert.AreEqual(
            (new DateOnly(2025, 1, 10), (DateOnly?)new DateOnly(2025, 1, 1), true),
            (match.Date, match.ActualDate, match.IsObserved));
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
}
