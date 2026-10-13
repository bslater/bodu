// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.LoadJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

public sealed partial class ParserValidationTests
{
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
    /// Verifies that the JSON path accepts a custom algorithm key declared by the supplied registry, matching the XML
    /// overload that takes a registry, and that the loaded rule resolves through the registered algorithm.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenCustomRegistryDeclaresKey_ShouldLoadAndResolve()
    {
        const string json =
            """
            { "schemaVersion": "1.0", "resourceId": "test.custom-json",
              "notableDates": [ { "id": "pi-day", "displayName": "Pi Day", "category": "Observance",
                "rules": [ { "id": "r", "strategy": { "algorithm": { "key": "pi-day" } } } ] } ] }
            """;

        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
            .Register("pi-day", new DelegateNotableDateAlgorithm(year => new DateOnly(year, 3, 14)));

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json, _ => null, registry);
        NotableDateService service = new(resource, new NotableDateServiceOptions { Algorithms = registry });

        Assert.AreEqual(
            new DateOnly(2026, 3, 14),
            service.Resolve(new DateRange(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), "XX").Single().Date);
    }

    /// <summary>
    /// Verifies that a JSON rule with no recognized occurrence source - no strategy, an empty strategy, or only an
    /// unrecognized strategy kind - is rejected with the <c>BODU-CAL-RULE-SOURCE</c> diagnostic rather than resolving
    /// to 1 January.
    /// </summary>
    /// <param name="rule">The JSON rule object under test.</param>
    [TestMethod]
    [DataRow("""{ "id": "r" }""", DisplayName = "No strategy or recurrence")]
    [DataRow("""{ "id": "r", "strategy": { } }""", DisplayName = "Empty strategy")]
    [DataRow("""{ "id": "r", "strategy": { "fixd": { "month": 3, "day": 1 } } }""", DisplayName = "Unrecognized strategy kind")]
    public void LoadJson_WhenRuleHasNoOccurrenceSource_ShouldThrowRuleSourceDiagnostic(string rule)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday", "rules": [ __RULE__ ] } ] }
            """.Replace("__RULE__", rule, StringComparison.Ordinal);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-RULE-SOURCE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-RULE-SOURCE");
    }

    /// <summary>
    /// Verifies that a JSON rule declaring more than one occurrence source - a strategy and a recurrence, or two
    /// strategy kinds - is rejected with the <c>BODU-CAL-RULE-SOURCE</c> diagnostic rather than silently using one.
    /// </summary>
    /// <param name="rule">The JSON rule object under test.</param>
    [TestMethod]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } }, "recurrence": { "weekly": { "days": [ "Monday" ] } } }""", DisplayName = "Strategy and recurrence")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 }, "algorithm": { "key": "western-easter" } } }""", DisplayName = "Two strategy kinds")]
    public void LoadJson_WhenRuleHasMoreThanOneOccurrenceSource_ShouldThrowRuleSourceDiagnostic(string rule)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday", "rules": [ __RULE__ ] } ] }
            """.Replace("__RULE__", rule, StringComparison.Ordinal);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-RULE-SOURCE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-RULE-SOURCE");
    }

    /// <summary>
    /// Verifies that a JSON <c>PatchRule</c> override declaring more than one occurrence source - a strategy and a
    /// recurrence, or two strategy kinds - is rejected with the <c>BODU-CAL-RULE-SOURCE</c> diagnostic rather than
    /// silently patching in one of them.
    /// </summary>
    /// <param name="patch">The JSON <c>PatchRule</c> override object under test.</param>
    [TestMethod]
    [DataRow("""{ "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "strategy": { "fixed": { "month": 4, "day": 1 } }, "recurrence": { "weekly": { "days": [ "Monday" ] } } }""", DisplayName = "Strategy and recurrence")]
    [DataRow("""{ "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "strategy": { "fixed": { "month": 4, "day": 1 }, "algorithm": { "key": "western-easter" } } }""", DisplayName = "Two strategy kinds")]
    public void LoadJson_WhenPatchRuleHasMoreThanOneOccurrenceSource_ShouldThrowRuleSourceDiagnostic(string patch)
    {
        string json = PatchRuleJson(patch);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-RULE-SOURCE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-RULE-SOURCE");
    }

    /// <summary>
    /// Verifies that a JSON <c>PatchRule</c> override whose strategy names no recognized kind is rejected with the
    /// <c>BODU-CAL-RULE-SOURCE</c> diagnostic rather than patching the rule to 1 January.
    /// </summary>
    /// <param name="patch">The JSON <c>PatchRule</c> override object under test.</param>
    [TestMethod]
    [DataRow("""{ "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "strategy": { } }""", DisplayName = "Empty strategy")]
    [DataRow("""{ "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "strategy": { "fixd": { "month": 4, "day": 1 } } }""", DisplayName = "Unrecognized strategy kind")]
    public void LoadJson_WhenPatchRuleStrategyNamesNoKind_ShouldThrowRuleSourceDiagnostic(string patch)
    {
        string json = PatchRuleJson(patch);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-RULE-SOURCE" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-RULE-SOURCE");
    }

    /// <summary>
    /// Verifies that a JSON <c>untilDate</c> whose strategy names no recognized kind is rejected with the
    /// <c>BODU-CAL-DURATION-NOEND</c> diagnostic rather than ending the span on 1 January.
    /// </summary>
    /// <param name="strategy">The JSON strategy object of the <c>untilDate</c> under test.</param>
    [TestMethod]
    [DataRow("""{ }""", DisplayName = "Empty strategy")]
    [DataRow("""{ "fixd": { "month": 3, "day": 5 } }""", DisplayName = "Unrecognized strategy kind")]
    public void LoadJson_WhenUntilDateStrategyNamesNoKind_ShouldThrowDurationNoEndDiagnostic(string strategy)
    {
        string json = UntilDateJson(strategy);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DURATION-NOEND" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-DURATION-NOEND");
    }

    /// <summary>
    /// Verifies that a JSON <c>untilDate</c> whose strategy names two kinds is rejected with the
    /// <c>BODU-CAL-DURATION-CONFLICT</c> diagnostic rather than silently ending the span on one of them.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenUntilDateStrategyHasTwoKinds_ShouldThrowDurationConflictDiagnostic()
    {
        string json = UntilDateJson("""{ "fixed": { "month": 3, "day": 5 }, "algorithm": { "key": "western-easter" } }""");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-DURATION-CONFLICT" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-DURATION-CONFLICT");
    }

    /// <summary>
    /// Builds a JSON document whose one rule, a fixed 1 March, is patched by a <c>PatchRule</c> override.
    /// </summary>
    /// <param name="patch">The JSON <c>PatchRule</c> override object.</param>
    /// <returns>The JSON document.</returns>
    private static string PatchRuleJson(string patch) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.bad",
          "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ],
          "overrides": [ __PATCH__ ] }
        """.Replace("__PATCH__", patch, StringComparison.Ordinal);

    /// <summary>
    /// Builds a JSON document whose one rule starts on 1 March and ends on the date an <c>untilDate</c> strategy selects.
    /// </summary>
    /// <param name="strategy">The JSON strategy object of the <c>untilDate</c>.</param>
    /// <returns>The JSON document.</returns>
    private static string UntilDateJson(string strategy) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.bad",
          "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } },
              "duration": { "untilDate": { "strategy": __STRATEGY__ } } } ] } ] }
        """.Replace("__STRATEGY__", strategy, StringComparison.Ordinal);
}
