// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.Load.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class ParserValidationTests
{
    /// <summary>
    /// Verifies that loading <see langword="null" /> XML throws <see cref="ArgumentNullException" /> naming the
    /// <c>xml</c> parameter.
    /// </summary>
    [TestMethod]
    public void Load_WhenXmlIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateResourceLoader.Load((string)null!);
        });

        Assert.AreEqual("xml", ex.ParamName);
    }

    /// <summary>
    /// Verifies that loading empty or white-space XML throws <see cref="ArgumentException" /> naming the <c>xml</c>
    /// parameter.
    /// </summary>
    /// <param name="xml">The empty or white-space content.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t\n  ")]
    public void Load_WhenXmlIsWhiteSpace_ShouldThrowArgumentException(string xml)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.AreEqual("xml", ex.ParamName);
    }

    /// <summary>
    /// Verifies that XML that is not well-formed throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenXmlIsNotWellFormed_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateResourceLoader.Load("<NotableDateResource><Unclosed></NotableDateResource>");
        });
    }

    /// <summary>
    /// Verifies that a strategy value outside the strategy's own range - which the schema reports but the parse carries
    /// on past - is reported as a <c>BODU-CAL-STRATEGY</c> error inside a <see cref="NotableDateValidationException" />
    /// rather than escaping as the strategy constructor's <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="strategy">The strategy element under test.</param>
    [TestMethod]
    [DataRow("""<OrdinalDayOfMonth month="February" ordinal="-40" />""", DisplayName = "Ordinal day of month beyond 31")]
    [DataRow("""<IsoWeekDate week="54" dayOfWeek="Monday" />""", DisplayName = "ISO week 54")]
    [DataRow("""<DayOfYear ordinal="0" />""", DisplayName = "Day of year zero")]
    public void Load_WhenStrategyValueIsOutOfRange_ShouldThrowStrategyDiagnostic(string strategy)
    {
        string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.bad">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy>__STRATEGY__</Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """.Replace("__STRATEGY__", strategy, StringComparison.Ordinal);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-STRATEGY" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-STRATEGY");
    }

    /// <summary>
    /// Verifies that an imported resource whose content is not well-formed is reported as a <c>BODU-CAL-SYNTAX</c> error
    /// inside a <see cref="NotableDateValidationException" />, rather than escaping as a <see cref="FormatException" />
    /// that the documentation reserves for the loaded document itself.
    /// </summary>
    [TestMethod]
    public void Load_WhenImportIsNotWellFormed_ShouldThrowSyntaxDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.bad-import">
              <Imports><Import resource="broken" /></Imports>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml, name => name == "broken" ? "<NotableDateResource><Unclosed>" : null);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-SYNTAX" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("broken", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-SYNTAX");
    }

    /// <summary>
    /// Verifies that a rule whose <c>Strategy</c> declares no strategy child is rejected with a schema diagnostic, since
    /// the XSD requires exactly one strategy choice.
    /// </summary>
    [TestMethod]
    public void Load_WhenRuleHasNoStrategy_ShouldThrowSchemaDiagnostic()
    {
        AssertFixtureFailsWith("invalid-no-strategy.xml", "BODU-CAL-SCHEMA");
    }

    /// <summary>
    /// Verifies that a rule whose <c>Strategy</c> declares two strategy children is rejected with a schema diagnostic,
    /// since the XSD permits exactly one strategy choice.
    /// </summary>
    [TestMethod]
    public void Load_WhenRuleHasMultipleStrategies_ShouldThrowSchemaDiagnostic()
    {
        AssertFixtureFailsWith("invalid-multiple-strategies.xml", "BODU-CAL-SCHEMA");
    }

    /// <summary>
    /// Verifies that a rule declaring both a <c>Strategy</c> and a <c>Recurrence</c> is reported with the
    /// <c>BODU-CAL-RULE-SOURCE</c> diagnostic that the JSON path shares, alongside the schema diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenRuleHasStrategyAndRecurrence_ShouldThrowRuleSourceDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.bad">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules>
                    <Rule id="r">
                      <Strategy><Fixed month="March" day="1" /></Strategy>
                      <Recurrence><Weekly><Day dayOfWeek="Monday" /></Weekly></Recurrence>
                    </Rule>
                  </Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-RULE-SOURCE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-RULE-SOURCE");
    }

    /// <summary>
    /// Verifies that a <c>PatchRule</c> override declaring both a <c>Strategy</c> and a <c>Recurrence</c>, which the
    /// schema permits, is rejected with the <c>BODU-CAL-RULE-SOURCE</c> diagnostic rather than silently patching in the
    /// recurrence.
    /// </summary>
    [TestMethod]
    public void Load_WhenPatchRuleHasStrategyAndRecurrence_ShouldThrowRuleSourceDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.bad">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules>
                    <Rule id="r"><Strategy><Fixed month="March" day="1" /></Strategy></Rule>
                  </Rules>
                </NotableDate>
              </NotableDates>
              <Overrides>
                <PatchRule notableDateRef="x" ruleRef="r">
                  <Strategy><Fixed month="April" day="1" /></Strategy>
                  <Recurrence><Weekly><Day dayOfWeek="Monday" /></Weekly></Recurrence>
                </PatchRule>
              </Overrides>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-RULE-SOURCE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-RULE-SOURCE");
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is not a seven-character working-week pattern is
    /// reported with the <c>BODU-CAL-WORKING-DAYS</c> diagnostic rather than escaping as a
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Load_WhenWorkingDaysMalformed_ShouldThrowWorkingDaysDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.bad">
              <ResolutionPolicy workingDays="MTWTF" />
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r"><Strategy><Fixed month="March" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-WORKING-DAYS" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-WORKING-DAYS");
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is a letter mask, which <see cref="DayOfWeekSet" />
    /// reads but the schema's binary pattern rejects, is reported with the <c>BODU-CAL-WORKING-DAYS</c> diagnostic that
    /// the JSON path shares, alongside the schema diagnostic.
    /// </summary>
    /// <param name="workingDays">The letter mask.</param>
    [TestMethod]
    [DataRow("_MTWTF_")]
    [DataRow("MTWTF__")]
    public void Load_WhenWorkingDaysIsLetterMask_ShouldThrowWorkingDaysDiagnostic(string workingDays)
    {
        string xml = WorkingDaysXml(workingDays);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-WORKING-DAYS" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-WORKING-DAYS");
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is the documented Sunday-first binary pattern loads as
    /// that working week.
    /// </summary>
    [TestMethod]
    public void Load_WhenWorkingDaysIsBinary_ShouldLoadTheWorkingWeek()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(WorkingDaysXml("1111100"));

        Assert.AreEqual(DayOfWeekSet.SundayToThursday, resource.ResolutionPolicy.WorkingWeek);
    }

    /// <summary>
    /// Builds an XML document with one fixed rule whose resolution policy declares the supplied working week.
    /// </summary>
    /// <param name="workingDays">The <c>workingDays</c> value.</param>
    /// <returns>The XML document.</returns>
    private static string WorkingDaysXml(string workingDays) =>
        """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.working-days">
          <ResolutionPolicy workingDays="__DAYS__" />
          <NotableDates>
            <NotableDate id="x" displayName="X" category="PublicHoliday">
              <Rules><Rule id="r"><Strategy><Fixed month="March" day="1" /></Strategy></Rule></Rules>
            </NotableDate>
          </NotableDates>
        </NotableDateResource>
        """.Replace("__DAYS__", workingDays, StringComparison.Ordinal);

    /// <summary>
    /// Verifies that an adjustment trigger's <c>month</c>, which the schema types as free text, holding neither a month
    /// name nor a number from 1 to 12 is rejected with the <c>BODU-CAL-VALUE</c> diagnostic rather than treated as
    /// absent, which leaves the trigger unable to fire.
    /// </summary>
    /// <param name="month">The malformed month.</param>
    [TestMethod]
    [DataRow("Juneteenth")]
    [DataRow("13")]
    public void Load_WhenTriggerMonthMalformed_ShouldThrowValueDiagnostic(string month)
    {
        string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.trigger-month">
              <AdjustmentPolicies>
                <AdjustmentPolicy id="policy">
                  <Trigger type="IfBeforeFixedDate" month="__MONTH__" day="15" />
                  <Action type="AddDays" days="1" />
                  <Emission mode="ObservedOnly" />
                </AdjustmentPolicy>
              </AdjustmentPolicies>
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules>
                    <Rule id="r">
                      <Strategy><Fixed month="March" day="1" /></Strategy>
                      <Adjustments><Adjustment policyRef="policy" /></Adjustments>
                    </Rule>
                  </Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """.Replace("__MONTH__", month, StringComparison.Ordinal);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains(month, StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that an integer-valued XML attribute holding text that is not an integer is reported with the
    /// <c>BODU-CAL-VALUE</c> diagnostic as well as the schema's, as the JSON loader reports it.
    /// </summary>
    [TestMethod]
    public void Load_WhenIntegerValueMalformed_ShouldThrowValueDiagnostic()
    {
        const string xml = """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.priority">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules><Rule id="r" priority="high"><Strategy><Fixed month="March" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("high", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that the v1 <c>Holiday</c> category token, renamed to <c>PublicHoliday</c> in v2, is rejected with a
    /// schema diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenCategoryIsUnknown_ShouldThrowSchemaDiagnostic()
    {
        AssertFixtureFailsWith("invalid-unknown-category.xml", "BODU-CAL-SCHEMA");
    }

    /// <summary>
    /// Verifies that an unknown adjustment trigger token (<c>IfFullMoon</c>) is rejected with a schema diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenTriggerIsUnknown_ShouldThrowSchemaDiagnostic()
    {
        AssertFixtureFailsWith("invalid-unknown-trigger.xml", "BODU-CAL-SCHEMA");
    }

    /// <summary>
    /// Verifies that an unknown adjustment action token (the v1 <c>ReplaceWithNamedDate</c>, renamed to
    /// <c>ReplaceWithRule</c>) is rejected with a schema diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenActionIsUnknown_ShouldThrowSchemaDiagnostic()
    {
        AssertFixtureFailsWith("invalid-unknown-action.xml", "BODU-CAL-SCHEMA");
    }

    /// <summary>
    /// Verifies that an impossible fixed Gregorian date (30 February) is rejected with the <c>BODU-CAL-DAY</c>
    /// diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenFixedDateIsImpossible_ShouldThrowDayDiagnostic()
    {
        AssertFixtureFailsWith("invalid-impossible-date.xml", "BODU-CAL-DAY");
    }

    /// <summary>
    /// Verifies that a duplicate rule identifier within a concept is rejected with the <c>BODU-CAL-DUP-RULE</c>
    /// diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenRuleIdsCollide_ShouldThrowDuplicateRuleDiagnostic()
    {
        AssertFixtureFailsWith("invalid-duplicate-rule-id.xml", "BODU-CAL-DUP-RULE");
    }

    /// <summary>
    /// Verifies that a duplicate concept identifier is rejected with the <c>BODU-CAL-DUP-ND</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenNotableDateIdsCollide_ShouldThrowDuplicateNotableDateDiagnostic()
    {
        AssertFixtureFailsWith("invalid-duplicate-notable-id.xml", "BODU-CAL-DUP-ND");
    }

    /// <summary>
    /// Verifies that a duplicate adjustment-policy identifier is rejected with the <c>BODU-CAL-DUP-POLICY</c>
    /// diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenPolicyIdsCollide_ShouldThrowDuplicatePolicyDiagnostic()
    {
        AssertFixtureFailsWith("invalid-duplicate-policy-id.xml", "BODU-CAL-DUP-POLICY");
    }

    /// <summary>
    /// Verifies that a rule referencing an unknown adjustment policy is rejected with the <c>BODU-CAL-ADJREF</c>
    /// diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenAdjustmentReferenceIsUnresolved_ShouldThrowAdjustmentRefDiagnostic()
    {
        AssertFixtureFailsWith("invalid-unknown-policy-ref.xml", "BODU-CAL-ADJREF");
    }

    /// <summary>
    /// Verifies that an offset strategy referencing a missing rule is rejected with the
    /// <c>BODU-CAL-OFFSET-MISSING</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenOffsetReferenceIsUnresolved_ShouldThrowOffsetMissingDiagnostic()
    {
        AssertFixtureFailsWith("invalid-offset-missing.xml", "BODU-CAL-OFFSET-MISSING");
    }

    /// <summary>
    /// Verifies that an algorithm strategy with an unrecognised key is rejected with the <c>BODU-CAL-ALGORITHM</c>
    /// diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenAlgorithmKeyIsUnknown_ShouldThrowAlgorithmDiagnostic()
    {
        AssertFixtureFailsWith("invalid-unknown-algorithm.xml", "BODU-CAL-ALGORITHM");
    }

    /// <summary>
    /// Verifies that an inverted applicability year range is rejected with the <c>BODU-CAL-YEARS</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenFromYearAfterToYear_ShouldThrowYearsDiagnostic()
    {
        AssertFixtureFailsWith("invalid-fromyear-after-toyear.xml", "BODU-CAL-YEARS");
    }

    /// <summary>
    /// Verifies that a <c>Custom</c> action without a handler key is rejected with the
    /// <c>BODU-CAL-HANDLER-MISSING</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenCustomActionHasNoHandler_ShouldThrowHandlerMissingDiagnostic()
    {
        AssertFixtureFailsWith("invalid-custom-action-no-handler.xml", "BODU-CAL-HANDLER-MISSING");
    }

    /// <summary>
    /// Verifies that a <c>Custom</c> trigger without a handler key is rejected with the
    /// <c>BODU-CAL-TRIGGER-HANDLER-MISSING</c> diagnostic.
    /// </summary>
    [TestMethod]
    public void Load_WhenCustomTriggerHasNoHandler_ShouldThrowTriggerHandlerMissingDiagnostic()
    {
        AssertFixtureFailsWith("invalid-custom-trigger-no-handler.xml", "BODU-CAL-TRIGGER-HANDLER-MISSING");
    }

    /// <summary>
    /// Verifies that a failing document reports every independent fault in one pass: a single load over a document with
    /// a duplicate rule id, an unresolved adjustment reference, and an impossible fixed date surfaces all three
    /// diagnostics together.
    /// </summary>
    [TestMethod]
    public void Load_WhenDocumentHasMultipleFaults_ShouldAggregateDiagnostics()
    {
        const string xml =
            """
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.multi-fault">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="PublicHoliday">
                  <Rules>
                    <Rule id="r"><Strategy><Fixed month="February" day="30" /></Strategy>
                      <Adjustments><Adjustment policyRef="missing-policy" /></Adjustments></Rule>
                    <Rule id="r"><Strategy><Fixed month="January" day="1" /></Strategy></Rule>
                  </Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DAY", ex.Diagnostics, "BODU-CAL-DAY");
        Assert.Contains(d => d.Code == "BODU-CAL-ADJREF", ex.Diagnostics, "BODU-CAL-ADJREF");
        Assert.Contains(d => d.Code == "BODU-CAL-DUP-RULE", ex.Diagnostics, "BODU-CAL-DUP-RULE");
    }
}
