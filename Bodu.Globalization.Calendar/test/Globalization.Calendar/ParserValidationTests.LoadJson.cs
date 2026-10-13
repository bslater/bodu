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
    /// Verifies that a JSON resolution policy whose <c>workingDays</c> is not a seven-character working-week pattern is
    /// reported with the <c>BODU-CAL-WORKING-DAYS</c> diagnostic rather than escaping as a
    /// <see cref="FormatException" />.
    /// </summary>
    /// <param name="workingDays">The malformed working-week pattern.</param>
    [TestMethod]
    [DataRow("MTWTF")]
    [DataRow("01111100")]
    [DataRow("0111112")]
    public void LoadJson_WhenWorkingDaysMalformed_ShouldThrowWorkingDaysDiagnostic(string workingDays)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.bad",
              "resolutionPolicy": { "workingDays": "__DAYS__" },
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ] }
            """.Replace("__DAYS__", workingDays, StringComparison.Ordinal);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-WORKING-DAYS" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-WORKING-DAYS");
    }

    /// <summary>
    /// The recognized value of every enumeration-valued field the JSON document under the enumeration tests holds, keyed
    /// by the placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_enumFieldValues = new(StringComparer.Ordinal)
    {
        ["duplicatePolicy"] = "Error",
        ["sameDayCollisionPolicy"] = "KeepAll",
        ["spanCollisionPolicy"] = "KeepAll",
        ["priorityDirection"] = "HigherWins",
        ["observedDateRangePolicy"] = "ObservedOccurrenceControlsInclusion",
        ["categoryPrecedence"] = "PublicHoliday",
        ["scope.calendars"] = "Gregorian",
        ["scope.categories"] = "PublicHoliday",
        ["trigger.type"] = "IfDayOfWeek",
        ["trigger.weekdays"] = "Saturday",
        ["trigger.weekOrdinal"] = "First",
        ["action.type"] = "MoveToNextWeekday",
        ["action.dayOfWeek"] = "Monday",
        ["emission.mode"] = "ObservedOnly",
        ["notableDate.category"] = "PublicHoliday",
        ["rule.category"] = "Observance",
        ["applicability.calendar"] = "Gregorian",
        ["dayOfWeekInMonth.dayOfWeek"] = "Monday",
        ["dayOfWeekInMonth.weekOrdinal"] = "First",
        ["weekdayNearDate.dayOfWeek"] = "Monday",
        ["weekdayNearDate.direction"] = "OnOrAfter",
        ["relativeWeekdayInMonth.dayOfWeek"] = "Monday",
        ["relativeWeekdayInMonth.weekOrdinal"] = "Last",
        ["relativeWeekdayInMonth.relativeDayOfWeek"] = "Tuesday",
        ["relativeWeekdayInMonth.direction"] = "After",
        ["weekdayNearRule.dayOfWeek"] = "Friday",
        ["weekdayNearRule.direction"] = "Before",
        ["nthWeekdayFromRule.dayOfWeek"] = "Sunday",
        ["isoWeekDate.dayOfWeek"] = "Wednesday",
        ["untilDate.startBoundary"] = "Inclusive",
        ["untilDate.endBoundary"] = "Exclusive",
        ["untilDate.selection"] = "FirstAfterStart",
        ["weekly.days"] = "Thursday",
        ["monthlyDay.invalidDayBehavior"] = "UseLastDayOfMonth",
        ["monthlyWeekday.dayOfWeek"] = "Friday",
        ["monthlyWeekday.weekOrdinal"] = "Second",
        ["patch.category"] = "Civic",
        ["use.category"] = "Cultural",
    };

    /// <summary>
    /// Verifies that the enumeration test document loads when every enumeration-valued field holds a recognized value,
    /// so each rejection the per-field test observes is caused by the one value it replaces.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryEnumValueRecognized_ShouldLoad()
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(EnumFieldJson(null, null), EnumImportResolver);

        Assert.AreEqual("test.enums", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an enumeration-valued JSON field holding a value that names no member is rejected with the
    /// <c>BODU-CAL-ENUM</c> diagnostic rather than replaced by the field's default.
    /// </summary>
    /// <param name="field">The placeholder of the field holding the unrecognized value.</param>
    [TestMethod]
    [DataRow("duplicatePolicy")]
    [DataRow("sameDayCollisionPolicy")]
    [DataRow("spanCollisionPolicy")]
    [DataRow("priorityDirection")]
    [DataRow("observedDateRangePolicy")]
    [DataRow("categoryPrecedence")]
    [DataRow("scope.calendars")]
    [DataRow("scope.categories")]
    [DataRow("trigger.type")]
    [DataRow("trigger.weekdays")]
    [DataRow("trigger.weekOrdinal")]
    [DataRow("action.type")]
    [DataRow("action.dayOfWeek")]
    [DataRow("emission.mode")]
    [DataRow("notableDate.category")]
    [DataRow("rule.category")]
    [DataRow("applicability.calendar")]
    [DataRow("dayOfWeekInMonth.dayOfWeek")]
    [DataRow("dayOfWeekInMonth.weekOrdinal")]
    [DataRow("weekdayNearDate.dayOfWeek")]
    [DataRow("weekdayNearDate.direction")]
    [DataRow("relativeWeekdayInMonth.dayOfWeek")]
    [DataRow("relativeWeekdayInMonth.weekOrdinal")]
    [DataRow("relativeWeekdayInMonth.relativeDayOfWeek")]
    [DataRow("relativeWeekdayInMonth.direction")]
    [DataRow("weekdayNearRule.dayOfWeek")]
    [DataRow("weekdayNearRule.direction")]
    [DataRow("nthWeekdayFromRule.dayOfWeek")]
    [DataRow("isoWeekDate.dayOfWeek")]
    [DataRow("untilDate.startBoundary")]
    [DataRow("untilDate.endBoundary")]
    [DataRow("untilDate.selection")]
    [DataRow("weekly.days")]
    [DataRow("monthlyDay.invalidDayBehavior")]
    [DataRow("monthlyWeekday.dayOfWeek")]
    [DataRow("monthlyWeekday.weekOrdinal")]
    [DataRow("patch.category")]
    [DataRow("use.category")]
    public void LoadJson_WhenEnumValueUnrecognized_ShouldThrowEnumDiagnostic(string field)
    {
        string json = EnumFieldJson(field, "Bogus");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-ENUM" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("Bogus", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-ENUM");
    }

    /// <summary>
    /// Verifies that an enumeration-valued JSON field holding a member's number rather than its name is rejected with the
    /// <c>BODU-CAL-ENUM</c> diagnostic, as the schema and the XML loader reject it.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEnumValueIsNumeric_ShouldThrowEnumDiagnostic()
    {
        string json = EnumFieldJson("sameDayCollisionPolicy", "1");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-ENUM" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-ENUM");
    }

    /// <summary>
    /// Verifies that an enumeration-valued JSON field naming a member in a different case is recognized, so the loader
    /// keeps reading member names case-insensitively.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEnumValueDiffersInCase_ShouldLoadTheNamedMember()
    {
        string json = EnumFieldJson("sameDayCollisionPolicy", "highestpriorityonly");

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);

        Assert.AreEqual(CollisionPolicy.HighestPriorityOnly, resource.ResolutionPolicy.SameDayCollisionPolicy);
    }

    /// <summary>
    /// Resolves the one resource the enumeration test document imports.
    /// </summary>
    /// <param name="name">The imported resource's name.</param>
    /// <returns>The imported resource's JSON, or <see langword="null" /> for any other name.</returns>
    private static string? EnumImportResolver(string name) =>
        name == "lib"
            ? """
              { "schemaVersion": "1.0", "resourceId": "test.lib",
                "notableDates": [ { "id": "lib-day", "displayName": "Library Day", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 8, "day": 8 } } } ] } ] }
              """
            : null;

    /// <summary>
    /// Builds a JSON document holding every enumeration-valued field the loader reads, each set to its recognized value
    /// except the one named, which holds the supplied value.
    /// </summary>
    /// <param name="field">The placeholder of the field to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named field holds.</param>
    /// <returns>The JSON document.</returns>
    private static string EnumFieldJson(string? field, string? value)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.enums",
              "resolutionPolicy": { "duplicatePolicy": "@duplicatePolicy@", "sameDayCollisionPolicy": "@sameDayCollisionPolicy@",
                "spanCollisionPolicy": "@spanCollisionPolicy@", "priorityDirection": "@priorityDirection@",
                "observedDateRangePolicy": "@observedDateRangePolicy@", "categoryPrecedence": [ "@categoryPrecedence@" ] },
              "adjustmentPolicies": [ { "id": "policy",
                "scope": { "calendars": [ "@scope.calendars@" ], "categories": [ "@scope.categories@" ] },
                "trigger": { "type": "@trigger.type@", "weekdays": [ "@trigger.weekdays@" ], "weekOrdinal": "@trigger.weekOrdinal@" },
                "action": { "type": "@action.type@", "dayOfWeek": "@action.dayOfWeek@" },
                "emission": { "mode": "@emission.mode@" } } ],
              "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "category": "@use.category@" } ] } ],
              "notableDates": [
                { "id": "anchor", "displayName": "Anchor", "category": "@notableDate.category@",
                  "rules": [ { "id": "r", "category": "@rule.category@", "applicability": { "calendar": "@applicability.calendar@" },
                    "strategy": { "fixed": { "month": 3, "day": 1 } },
                    "duration": { "untilDate": { "startBoundary": "@untilDate.startBoundary@", "endBoundary": "@untilDate.endBoundary@",
                      "selection": "@untilDate.selection@", "strategy": { "fixed": { "month": 3, "day": 5 } } } },
                    "adjustments": [ "policy" ] } ] },
                { "id": "dow-in-month", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "dayOfWeekInMonth": { "month": 4, "dayOfWeek": "@dayOfWeekInMonth.dayOfWeek@", "weekOrdinal": "@dayOfWeekInMonth.weekOrdinal@" } } } ] },
                { "id": "near-date", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "weekdayNearDate": { "month": 5, "day": 10, "dayOfWeek": "@weekdayNearDate.dayOfWeek@", "direction": "@weekdayNearDate.direction@" } } } ] },
                { "id": "relative", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "relativeWeekdayInMonth": { "month": 6, "dayOfWeek": "@relativeWeekdayInMonth.dayOfWeek@",
                    "weekOrdinal": "@relativeWeekdayInMonth.weekOrdinal@", "relativeDayOfWeek": "@relativeWeekdayInMonth.relativeDayOfWeek@",
                    "direction": "@relativeWeekdayInMonth.direction@" } } } ] },
                { "id": "near-rule", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "weekdayNearRule": { "notableDateRef": "anchor", "ruleRef": "r",
                    "dayOfWeek": "@weekdayNearRule.dayOfWeek@", "direction": "@weekdayNearRule.direction@" } } } ] },
                { "id": "nth-from-rule", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "anchor", "ruleRef": "r",
                    "dayOfWeek": "@nthWeekdayFromRule.dayOfWeek@", "ordinal": 2 } } } ] },
                { "id": "iso-week", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "isoWeekDate": { "week": 20, "dayOfWeek": "@isoWeekDate.dayOfWeek@" } } } ] },
                { "id": "weekly", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "weekly": { "days": [ "@weekly.days@" ] } } } ] },
                { "id": "monthly-day", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "monthlyDay": { "dayOfMonth": 31, "invalidDayBehavior": "@monthlyDay.invalidDayBehavior@" } } } ] },
                { "id": "monthly-weekday", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "monthlyWeekday": { "dayOfWeek": "@monthlyWeekday.dayOfWeek@", "weekOrdinal": "@monthlyWeekday.weekOrdinal@" } } } ] } ],
              "overrides": [ { "operation": "PatchRule", "notableDateRef": "dow-in-month", "ruleRef": "r", "category": "@patch.category@" } ] }
            """;

        foreach ((string key, string recognized) in s_enumFieldValues)
            json = json.Replace("@" + key + "@", key == field ? value : recognized, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Verifies that a JSON resolution policy whose <c>workingDays</c> is a letter mask, which
    /// <see cref="DayOfWeekSet" /> reads but the schema's binary pattern rejects, is reported with the
    /// <c>BODU-CAL-WORKING-DAYS</c> diagnostic, as the XML loader rejects it.
    /// </summary>
    /// <param name="workingDays">The letter mask.</param>
    [TestMethod]
    [DataRow("_MTWTF_")]
    [DataRow("MTWTF__")]
    [DataRow("-MTWTF-")]
    public void LoadJson_WhenWorkingDaysIsLetterMask_ShouldThrowWorkingDaysDiagnostic(string workingDays)
    {
        string json = WorkingDaysJson(workingDays);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-WORKING-DAYS" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-WORKING-DAYS");
    }

    /// <summary>
    /// Verifies that a JSON resolution policy whose <c>workingDays</c> is the documented Sunday-first binary pattern
    /// loads as that working week.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenWorkingDaysIsBinary_ShouldLoadTheWorkingWeek()
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(WorkingDaysJson("1111100"));

        Assert.AreEqual(DayOfWeekSet.SundayToThursday, resource.ResolutionPolicy.WorkingWeek);
    }

    /// <summary>
    /// Builds a JSON document with one fixed rule whose resolution policy declares the supplied working week.
    /// </summary>
    /// <param name="workingDays">The <c>workingDays</c> value.</param>
    /// <returns>The JSON document.</returns>
    private static string WorkingDaysJson(string workingDays) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.working-days",
          "resolutionPolicy": { "workingDays": "__DAYS__" },
          "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ] }
        """.Replace("__DAYS__", workingDays, StringComparison.Ordinal);

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
