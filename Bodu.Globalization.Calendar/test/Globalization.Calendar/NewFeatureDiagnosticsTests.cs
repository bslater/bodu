// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NewFeatureDiagnosticsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the negative and integration behaviour of the new occurrence-source, duration, and business-day features:
/// mutual-exclusivity and reference diagnostics, override duration replacement, and resource-order independence.
/// </summary>
[TestClass]
public sealed class NewFeatureDiagnosticsTests
{
    private const string Territory = "XX";

    /// <summary>
    /// Wraps a <c>NotableDates</c> body in a resource document.
    /// </summary>
    /// <param name="body">The document body (concepts and optional overrides).</param>
    /// <returns>The resource XML.</returns>
    private static string Document(string body) => $"""
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.diag">{body}</NotableDateResource>
        """;

    /// <summary>
    /// Verifies that declaring both a fixed <c>durationDays</c> and a calculated duration on one rule fails the load with
    /// a conflict diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenDurationConflict_ShouldFailWithConflictDiagnostic()
    {
        string xml = Document("""
          <NotableDates>
            <NotableDate id="event" displayName="Event" category="Other">
              <Rules>
                <Rule id="r" durationDays="3">
                  <Strategy><Fixed month="5" day="1" /></Strategy>
                  <Duration><UntilDate><Strategy><Fixed month="5" day="5" /></Strategy></UntilDate></Duration>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        """);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() => NotableDateResourceLoader.Load(xml));
        Assert.Contains(d => d.Code == "BODU-CAL-DURATION-CONFLICT", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a calculated duration whose end strategy references a missing rule fails the load with a
    /// duration-scoped reference diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenDurationEndStrategyReferenceMissing_ShouldFail()
    {
        string xml = Document("""
          <NotableDates>
            <NotableDate id="event" displayName="Event" category="Other">
              <Rules>
                <Rule id="r">
                  <Strategy><Fixed month="5" day="1" /></Strategy>
                  <Duration><UntilDate><Strategy><OffsetFromRule notableDateRef="does-not-exist" offsetDays="1" /></Strategy></UntilDate></Duration>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        """);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() => NotableDateResourceLoader.Load(xml));
        Assert.Contains(d => d.Code == "BODU-CAL-DURATION-OFFSET-MISSING", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a singular referential strategy referencing a recurrence rule fails the load with a
    /// recurring-reference diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenSingularReferenceIsRecurring_ShouldFail()
    {
        string xml = Document("""
          <NotableDates>
            <NotableDate id="weekly" displayName="Weekly" category="Other">
              <Rules><Rule id="r"><Recurrence><Weekly><Day dayOfWeek="Monday" /></Weekly></Recurrence></Rule></Rules>
            </NotableDate>
            <NotableDate id="near" displayName="Near" category="Other">
              <Rules><Rule id="r"><Strategy><WeekdayNearRule notableDateRef="weekly" dayOfWeek="Friday" direction="After" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        """);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() => NotableDateResourceLoader.Load(xml));
        Assert.Contains(d => d.Code == "BODU-CAL-REF-RECURRING", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that declaring both a strategy and a recurrence on one rule fails schema validation.
    /// </summary>
    [TestMethod]
    public void Load_WhenBothStrategyAndRecurrence_ShouldFailSchemaValidation()
    {
        string xml = Document("""
          <NotableDates>
            <NotableDate id="event" displayName="Event" category="Other">
              <Rules>
                <Rule id="r">
                  <Strategy><Fixed month="5" day="1" /></Strategy>
                  <Recurrence><Weekly><Day dayOfWeek="Monday" /></Weekly></Recurrence>
                </Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
        """);

        Assert.ThrowsExactly<NotableDateValidationException>(() => NotableDateResourceLoader.Load(xml));
    }

    /// <summary>
    /// Verifies that an override can replace a fixed duration with a calculated end-date duration, and that the rule then
    /// resolves using the calculated span only.
    /// </summary>
    [TestMethod]
    public void Override_WhenReplacingFixedDurationWithCalculated_ShouldResolveCalculated()
    {
        string xml = Document("""
          <NotableDates>
            <NotableDate id="event" displayName="Event" category="Other">
              <Rules>
                <Rule id="r" durationDays="7"><Strategy><Fixed month="5" day="1" /></Strategy></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
          <Overrides>
            <PatchRule notableDateRef="event" ruleRef="r">
              <Duration><UntilDate><Strategy><Fixed month="5" day="5" /></Strategy></UntilDate></Duration>
            </PatchRule>
          </Overrides>
        """);

        NotableDate result = new NotableDateService(NotableDateResourceLoader.Load(xml))
            .Resolve(new DateRange(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31)), Territory)
            .Single(r => r.NotableDateId == "event");

        Assert.AreEqual((new DateOnly(2026, 5, 1), 5, new DateOnly(2026, 5, 5)), (result.Date, result.DurationDays, result.EndDate));
    }

    /// <summary>
    /// Verifies that a business-day strategy's result does not depend on the declaration order of the non-working rules
    /// it consults.
    /// </summary>
    [TestMethod]
    public void WorkingDayInMonth_WhenNonWorkingRulesReordered_ShouldBeOrderIndependent()
    {
        const string newYear = """<NotableDate id="new-year" displayName="New Year" category="Other" defaultNonWorkingDay="true"><Rules><Rule id="r"><Strategy><Fixed month="1" day="1" /></Strategy></Rule></Rules></NotableDate>""";
        const string firstWorkingDay = """<NotableDate id="fwd" displayName="First Working Day" category="Other"><Rules><Rule id="r"><Strategy><WorkingDayInMonth month="1" ordinal="1" /></Strategy></Rule></Rules></NotableDate>""";

        DateOnly Resolve(string body) => new NotableDateService(NotableDateResourceLoader.Load(Document($"<NotableDates>{body}</NotableDates>")))
            .Resolve(new DateRange(new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31)), Territory)
            .Single(r => r.NotableDateId == "fwd").Date;

        Assert.AreEqual(Resolve(newYear + firstWorkingDay), Resolve(firstWorkingDay + newYear));
    }
}
