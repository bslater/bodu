// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the argument-, schema-, and semantic-validation contracts of <see cref="NotableDateResourceLoader" /> for
/// both the XML and JSON ingestion paths. Ported from the v1 <c>NotableDateRuleParserTests.Exceptions</c> /
/// <c>NotableDateRuleJsonParserTests.Exceptions</c> / <c>MonthValidation</c> rows, mapped onto the v2 failure model: an
/// <see cref="ArgumentNullException" /> for null content, an <see cref="ArgumentException" /> for white-space content, a
/// <see cref="FormatException" /> for malformed input, and a <see cref="NotableDateValidationException" /> whose
/// <see cref="NotableDateValidationException.Diagnostics" /> carry a stable code for schema and semantic faults.
/// </summary>
[TestClass]
public sealed class ParserValidationTests
{
    /// <summary>
    /// Asserts that loading the supplied invalid fixture throws a <see cref="NotableDateValidationException" /> carrying
    /// the expected error diagnostic.
    /// </summary>
    /// <param name="fileName">The invalid fixture file name.</param>
    /// <param name="expectedCode">The diagnostic code expected among the failure diagnostics.</param>
    private static void AssertFixtureFailsWith(string fileName, string expectedCode)
    {
        var xml = NotableDateFixtures.ReadText(fileName);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(xml);
        });

        Assert.Contains(
            d => d.Code == expectedCode && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics,
            $"Expected diagnostic '{expectedCode}'. Actual: {string.Join("; ", ex.Diagnostics.Select(d => d.Code))}");
    }

    // -----------------------------------------------------------------------------------------
    // Argument validation: null / white-space content
    // -----------------------------------------------------------------------------------------

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
    /// Verifies that loading <see langword="null" /> JSON throws <see cref="ArgumentNullException" /> naming the
    /// <c>json</c> parameter.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenJsonIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson((string)null!);
        });

        Assert.AreEqual("json", ex.ParamName);
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
    /// Verifies that loading empty or white-space JSON throws <see cref="ArgumentException" /> naming the <c>json</c>
    /// parameter.
    /// </summary>
    /// <param name="json">The empty or white-space content.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\r\n\t")]
    public void LoadJson_WhenJsonIsWhiteSpace_ShouldThrowArgumentException(string json)
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.AreEqual("json", ex.ParamName);
    }

    // -----------------------------------------------------------------------------------------
    // Malformed input
    // -----------------------------------------------------------------------------------------

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
    /// Verifies that JSON that is not well-formed throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenJsonIsNotWellFormed_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson("{ not valid json ");
        });
    }

    // -----------------------------------------------------------------------------------------
    // Schema violations (XSD-expressible faults surfaced as BODU-CAL-SCHEMA)
    // -----------------------------------------------------------------------------------------

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

    // -----------------------------------------------------------------------------------------
    // Semantic faults (XSD cannot express; surfaced with a specific code)
    // -----------------------------------------------------------------------------------------

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

    // -----------------------------------------------------------------------------------------
    // Semantic faults reproduced through the JSON ingestion path
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the JSON path reports an impossible fixed date with the <c>BODU-CAL-DAY</c> diagnostic, matching
    /// the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenFixedDateIsImpossible_ShouldThrowDayDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 2, "day": 30 } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DAY", ex.Diagnostics, "BODU-CAL-DAY");
    }

    /// <summary>
    /// Verifies that the JSON path reports an unresolved offset reference with the <c>BODU-CAL-OFFSET-MISSING</c>
    /// diagnostic, matching the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenOffsetReferenceIsUnresolved_ShouldThrowOffsetMissingDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "offsetFromRule": { "notableDateRef": "does-not-exist", "offsetDays": 1 } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-OFFSET-MISSING", ex.Diagnostics, "BODU-CAL-OFFSET-MISSING");
    }

    /// <summary>
    /// Verifies that the JSON path reports an unknown algorithm key with the <c>BODU-CAL-ALGORITHM</c> diagnostic,
    /// matching the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenAlgorithmKeyIsUnknown_ShouldThrowAlgorithmDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "algorithm": { "key": "not-a-real-algorithm" } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-ALGORITHM", ex.Diagnostics, "BODU-CAL-ALGORITHM");
    }

    /// <summary>
    /// Verifies that the JSON path reports an inverted applicability year range with the <c>BODU-CAL-YEARS</c>
    /// diagnostic, matching the XML path.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenFromYearAfterToYear_ShouldThrowYearsDiagnostic()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "applicability": { "fromYear": 2030, "toYear": 2000 }, "strategy": { "fixed": { "month": 1, "day": 1 } } } ] } ] }
            """;

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-YEARS", ex.Diagnostics, "BODU-CAL-YEARS");
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
