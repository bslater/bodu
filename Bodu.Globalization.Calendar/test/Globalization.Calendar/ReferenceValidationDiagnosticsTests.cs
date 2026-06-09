// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReferenceValidationDiagnosticsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the semantic-reference validation diagnostics on <see cref="NotableDateRuleValidator" />: an ambiguous
/// offset reference (a multi-rule notable date referenced without a specific rule) and an out-of-range fixed day.
/// </summary>
[TestClass]
public class ReferenceValidationDiagnosticsTests
{
    private const string AmbiguousOffsetReference = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.offset-ambiguous">
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="PublicHoliday">
              <Rules>
                <Rule id="r1"><Strategy><Fixed month="January" day="1" /></Strategy></Rule>
                <Rule id="r2"><Strategy><Fixed month="January" day="2" /></Strategy></Rule>
              </Rules>
            </NotableDate>
            <NotableDate id="dependent" displayName="Dependent" category="Observance">
              <Rules>
                <Rule id="ro"><Strategy><OffsetFromRule notableDateRef="anchor" offsetDays="1" /></Strategy></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    private const string ImpossibleFixedDay = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.bad-day">
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="February" day="30" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    private const string AmbiguousReplaceReference = """
        <?xml version="1.0" encoding="utf-8"?>
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.replace-ambiguous">
          <AdjustmentPolicies>
            <AdjustmentPolicy id="replace-amb" priority="100">
              <Trigger type="IfDayOfWeek"><Weekday value="Sunday" /></Trigger>
              <Action type="ReplaceWithRule" notableDateRef="anchor" />
              <Emission mode="ObservedOnly" reason="Replaced" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="PublicHoliday">
              <Rules>
                <Rule id="r1"><Strategy><Fixed month="January" day="1" /></Strategy></Rule>
                <Rule id="r2"><Strategy><Fixed month="January" day="2" /></Strategy></Rule>
              </Rules>
            </NotableDate>
            <NotableDate id="trigger" displayName="Trigger" category="PublicHoliday">
              <Rules>
                <Rule id="t">
                  <Strategy><Fixed month="July" day="4" /></Strategy>
                  <Adjustments><Adjustment policyRef="replace-amb" /></Adjustments>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """;

    /// <summary>
    /// Verifies that a replace-with-rule action referencing a notable date with multiple rules — without naming a
    /// specific rule — reports the <c>BODU-CAL-REPLACE-AMBIGUOUS</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenReplaceReferenceIsAmbiguous_ShouldReportAmbiguousDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(AmbiguousReplaceReference);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-REPLACE-AMBIGUOUS", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that an offset strategy referencing a notable date with multiple rules — without naming a specific rule
    /// — reports the <c>BODU-CAL-OFFSET-AMBIGUOUS</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenOffsetReferenceIsAmbiguous_ShouldReportAmbiguousDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(AmbiguousOffsetReference);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-OFFSET-AMBIGUOUS", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a fixed Gregorian date whose day exceeds the month's length reports the <c>BODU-CAL-DAY</c>
    /// diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenFixedDayExceedsMonthLength_ShouldReportDayDiagnostic()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(ImpossibleFixedDay);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DAY", ex.Diagnostics);
    }
}
