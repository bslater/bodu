// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParserValidationTests.LoadJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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

    /// <summary>
    /// The well-formed value, as a JSON literal, of every integer-, Boolean-, date- and month-valued field the JSON
    /// document under the scalar tests holds, keyed by the placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_scalarFieldValues = new(StringComparer.Ordinal)
    {
        ["policy.priority"] = "5",
        ["scope.fromYear"] = "2000",
        ["scope.toYear"] = "2100",
        ["scope.onlyYears"] = "2026",
        ["scope.exceptYears"] = "2027",
        ["trigger.month"] = "\"June\"",
        ["trigger.day"] = "15",
        ["action.days"] = "1",
        ["action.maxSearchDays"] = "7",
        ["action.skipWeekends"] = "true",
        ["action.skipNonWorkingDates"] = "false",
        ["emission.nonWorking"] = "true",
        ["use.nonWorking"] = "true",
        ["notableDate.defaultNonWorkingDay"] = "true",
        ["notableDate.defaultDurationDays"] = "1",
        ["rule.priority"] = "3",
        ["rule.nonWorking"] = "true",
        ["rule.durationDays"] = "2",
        ["applicability.fromYear"] = "2000",
        ["applicability.toYear"] = "2100",
        ["applicability.everyYears"] = "1",
        ["applicability.anchorYear"] = "2000",
        ["applicability.onlyYears"] = "2026",
        ["applicability.exceptYears"] = "2027",
        ["fixed.day"] = "1",
        ["fixed.sweepCalendarYears"] = "false",
        ["fixed.skipLeapMonth"] = "false",
        ["weekdayNearDate.day"] = "10",
        ["offsetFromRule.offsetDays"] = "3",
        ["weekdayNearRule.referenceYearOffset"] = "0",
        ["nthWeekdayFromRule.ordinal"] = "2",
        ["nthWeekdayFromRule.referenceYearOffset"] = "0",
        ["workingDayOffsetFromRule.offsetWorkingDays"] = "-1",
        ["workingDayOffsetFromRule.referenceYearOffset"] = "0",
        ["workingDayInMonth.ordinal"] = "1",
        ["ordinalDayOfMonth.ordinal"] = "2",
        ["isoWeekDate.week"] = "20",
        ["dayOfYear.ordinal"] = "100",
        ["dailyInterval.anchorDate"] = "\"2024-01-01\"",
        ["dailyInterval.intervalDays"] = "7",
        ["weekly.intervalWeeks"] = "2",
        ["weekly.anchorDate"] = "\"2024-01-04\"",
        ["monthlyDay.dayOfMonth"] = "15",
        ["monthlyDay.intervalMonths"] = "1",
        ["monthlyDay.anchorDate"] = "\"2024-01-15\"",
        ["monthlyWeekday.intervalMonths"] = "1",
        ["monthlyWeekday.anchorDate"] = "\"2024-01-12\"",
        ["patch.priority"] = "4",
        ["patch.nonWorking"] = "false",
        ["patch.durationDays"] = "1",
    };

    /// <summary>
    /// Verifies that the scalar test document loads when every integer-, Boolean-, date- and month-valued field holds a
    /// well-formed value, so each rejection the per-field tests observe is caused by the one value they replace.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryScalarValueWellFormed_ShouldLoad()
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(ScalarFieldJson(null, null), EnumImportResolver);

        Assert.AreEqual("test.scalars", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an integer-valued JSON field holding text that is not an integer is rejected with the
    /// <c>BODU-CAL-VALUE</c> diagnostic rather than replaced by the field's default.
    /// </summary>
    /// <param name="field">The placeholder of the field holding the malformed value.</param>
    [TestMethod]
    [DataRow("policy.priority")]
    [DataRow("scope.fromYear")]
    [DataRow("scope.toYear")]
    [DataRow("trigger.day")]
    [DataRow("action.days")]
    [DataRow("action.maxSearchDays")]
    [DataRow("notableDate.defaultDurationDays")]
    [DataRow("rule.priority")]
    [DataRow("rule.durationDays")]
    [DataRow("applicability.fromYear")]
    [DataRow("applicability.toYear")]
    [DataRow("applicability.everyYears")]
    [DataRow("applicability.anchorYear")]
    [DataRow("fixed.day")]
    [DataRow("weekdayNearDate.day")]
    [DataRow("offsetFromRule.offsetDays")]
    [DataRow("weekdayNearRule.referenceYearOffset")]
    [DataRow("nthWeekdayFromRule.ordinal")]
    [DataRow("nthWeekdayFromRule.referenceYearOffset")]
    [DataRow("workingDayOffsetFromRule.offsetWorkingDays")]
    [DataRow("workingDayOffsetFromRule.referenceYearOffset")]
    [DataRow("workingDayInMonth.ordinal")]
    [DataRow("ordinalDayOfMonth.ordinal")]
    [DataRow("isoWeekDate.week")]
    [DataRow("dayOfYear.ordinal")]
    [DataRow("dailyInterval.intervalDays")]
    [DataRow("weekly.intervalWeeks")]
    [DataRow("monthlyDay.dayOfMonth")]
    [DataRow("monthlyDay.intervalMonths")]
    [DataRow("monthlyWeekday.intervalMonths")]
    [DataRow("patch.priority")]
    [DataRow("patch.durationDays")]
    public void LoadJson_WhenIntegerValueMalformed_ShouldThrowValueDiagnostic(string field)
    {
        string json = ScalarFieldJson(field, "\"high\"");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("high", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that an integer-valued JSON field holding a number that is not a 32-bit integer, a fraction or a value
    /// beyond the range, is rejected with the <c>BODU-CAL-VALUE</c> diagnostic rather than replaced by the field's
    /// default.
    /// </summary>
    /// <param name="value">The JSON number.</param>
    [TestMethod]
    [DataRow("1.5")]
    [DataRow("3000000000")]
    public void LoadJson_WhenIntegerValueIsNotInt32_ShouldThrowValueDiagnostic(string value)
    {
        string json = ScalarFieldJson("rule.priority", value);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains(value, StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that an item of a JSON year list that is not an integer is rejected with the <c>BODU-CAL-VALUE</c>
    /// diagnostic rather than dropped, which would widen the list's meaning.
    /// </summary>
    /// <param name="field">The placeholder of the year list holding the malformed item.</param>
    /// <param name="value">The malformed item, as a JSON literal.</param>
    /// <param name="text">The text of the malformed item the diagnostic names.</param>
    [TestMethod]
    [DataRow("scope.onlyYears", "\"next\"", "next")]
    [DataRow("scope.exceptYears", "\"next\"", "next")]
    [DataRow("applicability.onlyYears", "\"next\"", "next")]
    [DataRow("applicability.exceptYears", "2026.5", "2026.5")]
    public void LoadJson_WhenYearListItemMalformed_ShouldThrowValueDiagnostic(string field, string value, string text)
    {
        string json = ScalarFieldJson(field, value);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains(text, StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that a Boolean-valued JSON field holding text that is not a Boolean is rejected with the
    /// <c>BODU-CAL-VALUE</c> diagnostic rather than replaced by the field's default.
    /// </summary>
    /// <param name="field">The placeholder of the field holding the malformed value.</param>
    [TestMethod]
    [DataRow("action.skipWeekends")]
    [DataRow("action.skipNonWorkingDates")]
    [DataRow("emission.nonWorking")]
    [DataRow("use.nonWorking")]
    [DataRow("notableDate.defaultNonWorkingDay")]
    [DataRow("rule.nonWorking")]
    [DataRow("fixed.sweepCalendarYears")]
    [DataRow("fixed.skipLeapMonth")]
    [DataRow("patch.nonWorking")]
    public void LoadJson_WhenBooleanValueMalformed_ShouldThrowValueDiagnostic(string field)
    {
        string json = ScalarFieldJson(field, "\"yes\"");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("yes", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that a date-valued JSON field holding text that is not a <c>yyyy-MM-dd</c> date is rejected with the
    /// <c>BODU-CAL-VALUE</c> diagnostic rather than replaced by a default date or treated as absent.
    /// </summary>
    /// <param name="field">The placeholder of the field holding the malformed value.</param>
    [TestMethod]
    [DataRow("dailyInterval.anchorDate")]
    [DataRow("weekly.anchorDate")]
    [DataRow("monthlyDay.anchorDate")]
    [DataRow("monthlyWeekday.anchorDate")]
    public void LoadJson_WhenDateValueMalformed_ShouldThrowValueDiagnostic(string field)
    {
        string json = ScalarFieldJson(field, "\"2024-02-30\"");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("2024-02-30", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that an adjustment trigger's <c>month</c> holding neither a month name nor a number from 1 to 12 is
    /// rejected with the <c>BODU-CAL-VALUE</c> diagnostic rather than treated as absent, which leaves the trigger unable
    /// to fire.
    /// </summary>
    /// <param name="value">The malformed month, as a JSON literal.</param>
    /// <param name="text">The text of the malformed month the diagnostic names.</param>
    [TestMethod]
    [DataRow("\"Juneteenth\"", "Juneteenth")]
    [DataRow("13", "13")]
    public void LoadJson_WhenTriggerMonthMalformed_ShouldThrowValueDiagnostic(string value, string text)
    {
        string json = ScalarFieldJson("trigger.month", value);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains(text, StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Builds a JSON document holding every integer-, Boolean-, date- and month-valued field the loader reads, each set
    /// to its well-formed value except the one named, which holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The placeholder of the field to set, or <see langword="null" /> to set none.</param>
    /// <param name="literal">The JSON literal the named field holds.</param>
    /// <returns>The JSON document.</returns>
    private static string ScalarFieldJson(string? field, string? literal)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.scalars",
              "adjustmentPolicies": [ { "id": "policy", "priority": @policy.priority@,
                "scope": { "fromYear": @scope.fromYear@, "toYear": @scope.toYear@, "onlyYears": [ @scope.onlyYears@ ], "exceptYears": [ @scope.exceptYears@ ] },
                "trigger": { "type": "IfBeforeFixedDate", "month": @trigger.month@, "day": @trigger.day@ },
                "action": { "type": "AddDays", "days": @action.days@, "maxSearchDays": @action.maxSearchDays@,
                  "skipWeekends": @action.skipWeekends@, "skipNonWorkingDates": @action.skipNonWorkingDates@ },
                "emission": { "mode": "ObservedOnly", "nonWorking": @emission.nonWorking@ } } ],
              "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "nonWorking": @use.nonWorking@ } ] } ],
              "notableDates": [
                { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday",
                  "defaultNonWorkingDay": @notableDate.defaultNonWorkingDay@, "defaultDurationDays": @notableDate.defaultDurationDays@,
                  "rules": [ { "id": "r", "priority": @rule.priority@, "nonWorking": @rule.nonWorking@, "durationDays": @rule.durationDays@,
                    "applicability": { "fromYear": @applicability.fromYear@, "toYear": @applicability.toYear@,
                      "everyYears": @applicability.everyYears@, "anchorYear": @applicability.anchorYear@,
                      "onlyYears": [ @applicability.onlyYears@ ], "exceptYears": [ @applicability.exceptYears@ ] },
                    "strategy": { "fixed": { "month": 3, "day": @fixed.day@, "sweepCalendarYears": @fixed.sweepCalendarYears@, "skipLeapMonth": @fixed.skipLeapMonth@ } },
                    "adjustments": [ "policy" ] } ] },
                { "id": "near-date", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "weekdayNearDate": { "month": 5, "day": @weekdayNearDate.day@, "dayOfWeek": "Monday", "direction": "OnOrAfter" } } } ] },
                { "id": "offset", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "offsetFromRule": { "notableDateRef": "anchor", "ruleRef": "r", "offsetDays": @offsetFromRule.offsetDays@ } } } ] },
                { "id": "near-rule", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "weekdayNearRule": { "notableDateRef": "anchor", "ruleRef": "r",
                    "dayOfWeek": "Friday", "direction": "Before", "referenceYearOffset": @weekdayNearRule.referenceYearOffset@ } } } ] },
                { "id": "nth-from-rule", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "anchor", "ruleRef": "r",
                    "dayOfWeek": "Sunday", "ordinal": @nthWeekdayFromRule.ordinal@, "referenceYearOffset": @nthWeekdayFromRule.referenceYearOffset@ } } } ] },
                { "id": "working-day-offset", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "workingDayOffsetFromRule": { "notableDateRef": "anchor", "ruleRef": "r",
                    "offsetWorkingDays": @workingDayOffsetFromRule.offsetWorkingDays@, "referenceYearOffset": @workingDayOffsetFromRule.referenceYearOffset@ } } } ] },
                { "id": "working-day-in-month", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "workingDayInMonth": { "month": 6, "ordinal": @workingDayInMonth.ordinal@ } } } ] },
                { "id": "ordinal-day", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "ordinalDayOfMonth": { "month": 7, "ordinal": @ordinalDayOfMonth.ordinal@ } } } ] },
                { "id": "iso-week", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "isoWeekDate": { "week": @isoWeekDate.week@, "dayOfWeek": "Wednesday" } } } ] },
                { "id": "day-of-year", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "dayOfYear": { "ordinal": @dayOfYear.ordinal@ } } } ] },
                { "id": "daily", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "dailyInterval": { "anchorDate": @dailyInterval.anchorDate@, "intervalDays": @dailyInterval.intervalDays@ } } } ] },
                { "id": "weekly", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "weekly": { "days": [ "Thursday" ], "intervalWeeks": @weekly.intervalWeeks@, "anchorDate": @weekly.anchorDate@ } } } ] },
                { "id": "monthly-day", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "monthlyDay": { "dayOfMonth": @monthlyDay.dayOfMonth@, "intervalMonths": @monthlyDay.intervalMonths@,
                    "anchorDate": @monthlyDay.anchorDate@ } } } ] },
                { "id": "monthly-weekday", "displayName": "D", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "monthlyWeekday": { "dayOfWeek": "Friday", "weekOrdinal": "Second",
                    "intervalMonths": @monthlyWeekday.intervalMonths@, "anchorDate": @monthlyWeekday.anchorDate@ } } } ] } ],
              "overrides": [ { "operation": "PatchRule", "notableDateRef": "near-date", "ruleRef": "r",
                "priority": @patch.priority@, "nonWorking": @patch.nonWorking@, "durationDays": @patch.durationDays@ } ] }
            """;

        foreach ((string key, string wellFormed) in s_scalarFieldValues)
            json = json.Replace("@" + key + "@", key == field ? literal : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// The well-formed item, as a JSON literal, of every list-valued field the JSON document under the list tests holds,
    /// keyed by the placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_listFieldItems = new(StringComparer.Ordinal)
    {
        ["categoryPrecedence"] = "\"PublicHoliday\"",
        ["scope.territories"] = "\"AU\"",
        ["scope.calendars"] = "\"Gregorian\"",
        ["scope.categories"] = "\"PublicHoliday\"",
        ["scope.notableDateRefs"] = "\"anchor\"",
        ["scope.ruleRefs"] = "{ \"notableDateRef\": \"anchor\", \"ruleRef\": \"r\" }",
        ["trigger.weekdays"] = "\"Saturday\"",
        ["use.adjustments"] = "\"policy\"",
        ["notableDate.tags"] = "\"civic\"",
        ["applicability.territories"] = "\"AU\"",
        ["rule.tags"] = "\"civic\"",
        ["rule.adjustments"] = "\"policy\"",
        ["weekly.days"] = "\"Thursday\"",
        ["patch.tags"] = "\"civic\"",
        ["patch.adjustments"] = "\"policy\"",
    };

    /// <summary>
    /// Verifies that the list test document loads when every list-valued field holds a well-formed item, so each
    /// rejection the per-field tests observe is caused by the one item they replace.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryListItemWellFormed_ShouldLoad()
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(ListFieldJson(null, null), EnumImportResolver);

        Assert.AreEqual("test.lists", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an item of a JSON list of enumeration names that is a number, a Boolean or <c>null</c> is rejected
    /// with the <c>BODU-CAL-ENUM</c> diagnostic naming the item, rather than dropped, which would empty the list and
    /// widen or disable what it selects.
    /// </summary>
    /// <param name="field">The placeholder of the list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    [TestMethod]
    [DataRow("categoryPrecedence", "1")]
    [DataRow("scope.calendars", "0")]
    [DataRow("scope.categories", "1")]
    [DataRow("scope.categories", "null")]
    [DataRow("trigger.weekdays", "6")]
    [DataRow("trigger.weekdays", "true")]
    [DataRow("weekly.days", "4")]
    public void LoadJson_WhenNameListItemIsNotString_ShouldThrowEnumDiagnostic(string field, string item)
    {
        string json = ListFieldJson(field, item);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-ENUM" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("'" + item + "'", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-ENUM");
    }

    /// <summary>
    /// Verifies that an item of a JSON list of territory codes, references or tags that is not a string is rejected
    /// with the <c>BODU-CAL-VALUE</c> diagnostic naming the item's JSON kind, rather than dropped.
    /// </summary>
    /// <param name="field">The placeholder of the list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    [TestMethod]
    [DataRow("scope.territories", "36")]
    [DataRow("scope.notableDateRefs", "5")]
    [DataRow("scope.ruleRefs", "5")]
    [DataRow("use.adjustments", "5")]
    [DataRow("notableDate.tags", "5")]
    [DataRow("applicability.territories", "36")]
    [DataRow("rule.tags", "true")]
    [DataRow("rule.adjustments", "5")]
    [DataRow("patch.tags", "null")]
    [DataRow("patch.adjustments", "5")]
    public void LoadJson_WhenTextListItemIsNotString_ShouldThrowValueDiagnostic(string field, string item)
    {
        string json = ListFieldJson(field, item);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains(" list holds a JSON ", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that a JSON <c>fixed</c> strategy whose <c>day</c> lies outside 1 to 31 is rejected with the
    /// <c>BODU-CAL-STRATEGY</c> diagnostic rather than clamped into that range.
    /// </summary>
    /// <param name="day">The out-of-range day.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(32)]
    [DataRow(45)]
    public void LoadJson_WhenFixedDayOutOfRange_ShouldThrowStrategyDiagnostic(int day)
    {
        string json = DayStrategyJson($$"""{ "fixed": { "month": 1, "day": {{day}} } }""");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-STRATEGY" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-STRATEGY");
    }

    /// <summary>
    /// Verifies that a JSON <c>weekdayNearDate</c> strategy whose <c>day</c> lies outside 1 to 31 is rejected with the
    /// <c>BODU-CAL-STRATEGY</c> diagnostic rather than clamped into that range.
    /// </summary>
    /// <param name="day">The out-of-range day.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(32)]
    [DataRow(45)]
    public void LoadJson_WhenWeekdayNearDateDayOutOfRange_ShouldThrowStrategyDiagnostic(int day)
    {
        string json = DayStrategyJson($$"""{ "weekdayNearDate": { "month": 1, "day": {{day}}, "dayOfWeek": "Monday", "direction": "OnOrAfter" } }""");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-STRATEGY" && d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics, "BODU-CAL-STRATEGY");
    }

    /// <summary>
    /// Verifies that a JSON <c>fixed</c> strategy whose <c>day</c> is a bound of 1 to 31 loads as that day.
    /// </summary>
    /// <param name="day">The bounding day.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(31)]
    public void LoadJson_WhenFixedDayAtBound_ShouldLoadThatDay(int day)
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(DayStrategyJson($$"""{ "fixed": { "month": 1, "day": {{day}} } }"""));

        Assert.AreEqual(day, ((FixedDateStrategy)resource.NotableDates[0].Rules[0].Strategy!).Day);
    }

    /// <summary>
    /// Verifies that a JSON <c>weekdayNearDate</c> strategy whose <c>day</c> is a bound of 1 to 31 loads as that day.
    /// </summary>
    /// <param name="day">The bounding day.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(31)]
    public void LoadJson_WhenWeekdayNearDateDayAtBound_ShouldLoadThatDay(int day)
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(DayStrategyJson($$"""{ "weekdayNearDate": { "month": 1, "day": {{day}}, "dayOfWeek": "Monday", "direction": "OnOrAfter" } }"""));

        Assert.AreEqual(day, ((WeekdayNearDateStrategy)resource.NotableDates[0].Rules[0].Strategy!).Day);
    }

    /// <summary>
    /// Builds a JSON document whose one rule declares the supplied strategy.
    /// </summary>
    /// <param name="strategy">The JSON strategy object.</param>
    /// <returns>The JSON document.</returns>
    private static string DayStrategyJson(string strategy) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.day",
          "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
            "rules": [ { "id": "r", "strategy": __STRATEGY__ } ] } ] }
        """.Replace("__STRATEGY__", strategy, StringComparison.Ordinal);

    /// <summary>
    /// Builds a JSON document holding every list-valued field the loader reads, each holding one well-formed item except
    /// the one named, which holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The placeholder of the list to set, or <see langword="null" /> to set none.</param>
    /// <param name="item">The JSON literal the named list holds.</param>
    /// <returns>The JSON document.</returns>
    private static string ListFieldJson(string? field, string? item)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.lists",
              "resolutionPolicy": { "categoryPrecedence": [ @categoryPrecedence@ ] },
              "adjustmentPolicies": [ { "id": "policy",
                "scope": { "territories": [ @scope.territories@ ], "calendars": [ @scope.calendars@ ], "categories": [ @scope.categories@ ],
                  "notableDateRefs": [ @scope.notableDateRefs@ ], "ruleRefs": [ @scope.ruleRefs@ ] },
                "trigger": { "type": "IfDayOfWeek", "weekdays": [ @trigger.weekdays@ ] },
                "action": { "type": "MoveToNextWeekday", "dayOfWeek": "Monday" },
                "emission": { "mode": "ObservedOnly" } } ],
              "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "adjustments": [ @use.adjustments@ ] } ] } ],
              "notableDates": [
                { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday", "tags": [ @notableDate.tags@ ],
                  "rules": [ { "id": "r", "applicability": { "territories": [ @applicability.territories@ ] },
                    "strategy": { "fixed": { "month": 3, "day": 1 } },
                    "tags": [ @rule.tags@ ], "adjustments": [ @rule.adjustments@ ] } ] },
                { "id": "weekly", "displayName": "W", "category": "Observance",
                  "rules": [ { "id": "r", "recurrence": { "weekly": { "days": [ @weekly.days@ ] } } } ] },
                { "id": "other", "displayName": "O", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ],
              "overrides": [ { "operation": "PatchRule", "notableDateRef": "other", "ruleRef": "r",
                "tags": [ @patch.tags@ ], "adjustments": [ @patch.adjustments@ ] } ] }
            """;

        foreach ((string key, string wellFormed) in s_listFieldItems)
            json = json.Replace("@" + key + "@", key == field ? item : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Verifies that a JSON scalar field holding an object or an array, an integer, an enumeration name or a text value,
    /// is rejected with the <c>BODU-CAL-VALUE</c> diagnostic naming the field, rather than read as absent and replaced
    /// by the field's default.
    /// </summary>
    /// <param name="field">The JSON property holding the value.</param>
    /// <param name="literal">The object or array, as a JSON literal.</param>
    [TestMethod]
    [DataRow("priority", "{}")]
    [DataRow("priority", "[ 3 ]")]
    [DataRow("category", "{}")]
    [DataRow("category", "[ \"PublicHoliday\" ]")]
    [DataRow("displayName", "{ \"en\": \"X\" }")]
    [DataRow("displayName", "[]")]
    [DataRow("day", "{}")]
    public void LoadJson_WhenScalarValueIsObjectOrArray_ShouldThrowValueDiagnostic(string field, string literal)
    {
        string json = ScalarValueJson(field, literal);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("'" + field + "'", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that an optional JSON scalar field holding the literal <c>null</c> reads as absent, loading as the
    /// document that omits the field loads.
    /// </summary>
    /// <param name="field">The JSON property holding <c>null</c>.</param>
    [TestMethod]
    [DataRow("priority")]
    public void LoadJson_WhenScalarValueIsNull_ShouldReadItAsAbsent(string field)
    {
        NotableDateResource withNull = NotableDateResourceLoader.LoadJson(ScalarValueJson(field, "null"));
        NotableDateResource omitted = NotableDateResourceLoader.LoadJson(ScalarValueJson(field, null));

        NotableDateDefinition expected = omitted.NotableDates[0];
        NotableDateDefinition actual = withNull.NotableDates[0];
        Assert.AreEqual(expected.DisplayName, actual.DisplayName);
        Assert.AreEqual(expected.Category, actual.Category);
        Assert.AreEqual(expected.Rules[0].Priority, actual.Rules[0].Priority);
    }

    /// <summary>
    /// Builds a JSON document with one fixed concept whose named property, the rule's <c>priority</c>, the concept's
    /// <c>category</c> or <c>displayName</c>, or the fixed strategy's <c>day</c>, holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The property that holds the literal.</param>
    /// <param name="literal">The JSON literal, or <see langword="null" /> to omit the property.</param>
    /// <returns>The JSON document.</returns>
    private static string ScalarValueJson(string field, string? literal)
    {
        string Property(string name, string wellFormed) =>
            name != field ? $"\"{name}\": {wellFormed}, " : literal is null ? string.Empty : $"\"{name}\": {literal}, ";

        return $$"""
            { "schemaVersion": "1.0", "resourceId": "test.objects",
              "notableDates": [ { {{Property("displayName", "\"X\"")}}{{Property("category", "\"PublicHoliday\"")}}"id": "x",
                "rules": [ { {{Property("priority", "3")}}"id": "r", "strategy": { "fixed": { {{Property("day", "1")}}"month": 3 } } } ] } ] }
            """;
    }

    /// <summary>
    /// The well-formed value of every identifier, reference and territory code the JSON document under the identifier
    /// tests holds, keyed by the placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_identifierFieldValues = new(StringComparer.Ordinal)
    {
        ["resourceId"] = "test.identifiers",
        ["policy.id"] = "policy",
        ["action.notableDateRef"] = "offset",
        ["action.ruleRef"] = "r",
        ["scope.territories"] = "AU",
        ["scope.notableDateRefs"] = "anchor",
        ["scope.ruleRefs.notableDateRef"] = "anchor",
        ["scope.ruleRefs.ruleRef"] = "r",
        ["use.notableDateRef"] = "lib-day",
        ["use.as"] = "alias",
        ["use.territory"] = "AU-NSW",
        ["use.adjustments"] = "policy",
        ["notableDate.id"] = "anchor",
        ["rule.id"] = "r",
        ["applicability.territories"] = "AU",
        ["rule.adjustments"] = "policy",
        ["offsetFromRule.notableDateRef"] = "anchor",
        ["offsetFromRule.ruleRef"] = "r",
        ["untilDate.notableDateRef"] = "anchor",
        ["untilDate.ruleRef"] = "r",
        ["patch.notableDateRef"] = "anchor",
        ["patch.ruleRef"] = "r",
        ["patch.adjustments"] = "policy",
        ["remove.notableDateRef"] = "offset",
        ["remove.ruleRef"] = "gone",
        ["add.notableDateRef"] = "offset",
        ["add.rule.id"] = "added",
    };

    /// <summary>
    /// Verifies that the identifier test document loads when every identifier, reference and territory code matches
    /// the pattern the schema requires, so each rejection the per-field tests observe is caused by the one value they
    /// replace.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryIdentifierMatchesSchemaPattern_ShouldLoad()
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(IdentifierFieldJson(null, null), EnumImportResolver);

        Assert.AreEqual("test.identifiers", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that a JSON identifier, reference or territory code that breaks the pattern the schema requires of it is
    /// rejected with the <c>BODU-CAL-SCHEMA</c> diagnostic naming the value, as the same document in XML is rejected,
    /// rather than loaded.
    /// </summary>
    /// <param name="field">The placeholder of the field holding the value.</param>
    /// <param name="value">The value that breaks the field's pattern.</param>
    [TestMethod]
    [DataRow("resourceId", "Test.Identifiers")]
    [DataRow("resourceId", "test..identifiers")]
    [DataRow("policy.id", "Policy")]
    [DataRow("action.notableDateRef", "Offset")]
    [DataRow("action.ruleRef", "r_1")]
    [DataRow("scope.territories", "au")]
    [DataRow("scope.notableDateRefs", "Anchor")]
    [DataRow("scope.ruleRefs.notableDateRef", "anchor.day")]
    [DataRow("scope.ruleRefs.ruleRef", "R")]
    [DataRow("use.notableDateRef", "Lib-Day")]
    [DataRow("use.as", "1alias")]
    [DataRow("use.territory", "AU-NEWSW")]
    [DataRow("use.adjustments", "policy-")]
    [DataRow("notableDate.id", "New-Year")]
    [DataRow("notableDate.id", "new year")]
    [DataRow("rule.id", "R")]
    [DataRow("applicability.territories", "AUS")]
    [DataRow("rule.adjustments", "-policy")]
    [DataRow("offsetFromRule.notableDateRef", "Anchor")]
    [DataRow("offsetFromRule.ruleRef", "r--1")]
    [DataRow("untilDate.notableDateRef", "ANCHOR")]
    [DataRow("untilDate.ruleRef", "r.1")]
    [DataRow("patch.notableDateRef", "Anchor")]
    [DataRow("patch.ruleRef", "R")]
    [DataRow("patch.adjustments", "Policy")]
    [DataRow("remove.notableDateRef", "Offset")]
    [DataRow("remove.ruleRef", "Gone")]
    [DataRow("add.notableDateRef", "Offset")]
    [DataRow("add.rule.id", "Added")]
    public void LoadJson_WhenIdentifierBreaksSchemaPattern_ShouldThrowSchemaDiagnostic(string field, string value)
    {
        string json = IdentifierFieldJson(field, value);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-SCHEMA" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("'" + value + "'", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-SCHEMA");
    }

    /// <summary>
    /// Builds a JSON document holding every identifier, reference and territory code the loader reads, each set to its
    /// well-formed value except the one named, which holds the supplied value.
    /// </summary>
    /// <param name="field">The placeholder of the field to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named field holds.</param>
    /// <returns>The JSON document.</returns>
    private static string IdentifierFieldJson(string? field, string? value)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "@resourceId@",
              "adjustmentPolicies": [ { "id": "@policy.id@",
                "scope": { "territories": [ "@scope.territories@" ], "notableDateRefs": [ "@scope.notableDateRefs@" ],
                  "ruleRefs": [ { "notableDateRef": "@scope.ruleRefs.notableDateRef@", "ruleRef": "@scope.ruleRefs.ruleRef@" } ] },
                "trigger": { "type": "IfWeekend" },
                "action": { "type": "ReplaceWithRule", "notableDateRef": "@action.notableDateRef@", "ruleRef": "@action.ruleRef@" },
                "emission": { "mode": "ObservedOnly" } } ],
              "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "@use.notableDateRef@", "as": "@use.as@",
                "territory": "@use.territory@", "adjustments": [ "@use.adjustments@" ] } ] } ],
              "notableDates": [
                { "id": "@notableDate.id@", "displayName": "Anchor", "category": "PublicHoliday",
                  "rules": [ { "id": "@rule.id@", "applicability": { "territories": [ "@applicability.territories@" ] },
                    "strategy": { "fixed": { "month": 3, "day": 1 } }, "adjustments": [ "@rule.adjustments@" ] } ] },
                { "id": "offset", "displayName": "Offset", "category": "Observance",
                  "rules": [
                    { "id": "r", "strategy": { "offsetFromRule": { "notableDateRef": "@offsetFromRule.notableDateRef@",
                      "ruleRef": "@offsetFromRule.ruleRef@", "offsetDays": 1 } },
                      "duration": { "untilDate": { "strategy": { "offsetFromRule": { "notableDateRef": "@untilDate.notableDateRef@",
                        "ruleRef": "@untilDate.ruleRef@", "offsetDays": 3 } } } } },
                    { "id": "gone", "strategy": { "fixed": { "month": 5, "day": 1 } } } ] } ],
              "overrides": [
                { "operation": "PatchRule", "notableDateRef": "@patch.notableDateRef@", "ruleRef": "@patch.ruleRef@", "priority": 2,
                  "adjustments": [ "@patch.adjustments@" ] },
                { "operation": "RemoveRule", "notableDateRef": "@remove.notableDateRef@", "ruleRef": "@remove.ruleRef@" },
                { "operation": "AddRule", "notableDateRef": "@add.notableDateRef@",
                  "rule": { "id": "@add.rule.id@", "strategy": { "fixed": { "month": 6, "day": 1 } } } } ] }
            """;

        foreach ((string key, string wellFormed) in s_identifierFieldValues)
            json = json.Replace("@" + key + "@", key == field ? value : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// The well-formed value, as a JSON literal, of every object- and list-valued property the JSON document under the
    /// value-kind tests holds, keyed by the placeholder that stands for it, in the order the placeholders are replaced:
    /// a value that holds other placeholders precedes them.
    /// </summary>
    private static readonly (string Key, string WellFormed)[] s_kindFieldValues =
    [
        ("resolutionPolicy", """{ "categoryPrecedence": @categoryPrecedence@ }"""),
        ("categoryPrecedence", """[ "PublicHoliday" ]"""),
        ("adjustmentPolicies", """[ { "id": "policy", "scope": @scope@, "trigger": @trigger@, "action": @action@, "emission": @emission@, "parameters": @parameters@ } ]"""),
        ("scope", """{ "territories": @scope.territories@, "calendars": @scope.calendars@, "categories": @scope.categories@, "notableDateRefs": @scope.notableDateRefs@, "ruleRefs": @scope.ruleRefs@, "onlyYears": @scope.onlyYears@ }"""),
        ("scope.territories", """[ "AU" ]"""),
        ("scope.calendars", """[ "Gregorian" ]"""),
        ("scope.categories", """[ "PublicHoliday" ]"""),
        ("scope.notableDateRefs", """[ "anchor" ]"""),
        ("scope.ruleRefs", """[ { "notableDateRef": "anchor", "ruleRef": "r" } ]"""),
        ("scope.onlyYears", """[ 2026 ]"""),
        ("trigger", """{ "type": "IfDayOfWeek", "weekdays": @trigger.weekdays@ }"""),
        ("trigger.weekdays", """[ "Saturday" ]"""),
        ("action", """{ "type": "MoveToNextWeekday", "dayOfWeek": "Monday" }"""),
        ("emission", """{ "mode": "ObservedOnly" }"""),
        ("parameters", """{ "region": "north" }"""),
        ("imports", """[ { "resource": "lib", "uses": @uses@ } ]"""),
        ("uses", """[ { "notableDateRef": "lib-day", "adjustments": @use.adjustments@ } ]"""),
        ("use.adjustments", """[ "policy" ]"""),
        ("notableDates", """[ { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday", "tags": @notableDate.tags@, "rules": @rules@ }, @weekly.concept@, @other.concept@ ]"""),
        ("weekly.concept", """{ "id": "weekly", "displayName": "W", "category": "Observance", "rules": [ { "id": "r", "recurrence": @recurrence@ } ] }"""),
        ("other.concept", """{ "id": "other", "displayName": "O", "category": "Observance", "rules": [ { "id": "r", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] }"""),
        ("notableDate.tags", """[ "civic" ]"""),
        ("rules", """[ { "id": "r", "applicability": @applicability@, "strategy": @strategy@, "duration": @duration@, "tags": @rule.tags@, "adjustments": @rule.adjustments@ } ]"""),
        ("applicability", """{ "territories": @applicability.territories@, "exceptYears": @applicability.exceptYears@ }"""),
        ("applicability.territories", """[ "AU" ]"""),
        ("applicability.exceptYears", """[ 2027 ]"""),
        ("strategy", """{ "fixed": @fixed@ }"""),
        ("fixed", """{ "month": 3, "day": 1 }"""),
        ("duration", """{ "untilDate": @untilDate@ }"""),
        ("untilDate", """{ "strategy": @untilDate.strategy@ }"""),
        ("untilDate.strategy", """{ "fixed": { "month": 3, "day": 5 } }"""),
        ("rule.tags", """[ "civic" ]"""),
        ("rule.adjustments", """[ "policy" ]"""),
        ("recurrence", """{ "weekly": @weekly@ }"""),
        ("weekly", """{ "days": @weekly.days@ }"""),
        ("weekly.days", """[ "Thursday" ]"""),
        ("overrides", """[ { "operation": "PatchRule", "notableDateRef": "other", "ruleRef": "r", "applicability": @patch.applicability@, "tags": @patch.tags@, "adjustments": @patch.adjustments@ } ]"""),
        ("patch.applicability", """{ "territories": [ "AU" ] }"""),
        ("patch.tags", """[ "civic" ]"""),
        ("patch.adjustments", """[ "policy" ]"""),
    ];

    /// <summary>
    /// Verifies that the value-kind test document loads when every object- and list-valued property holds a value of
    /// its kind, so each rejection the per-property tests observe is caused by the one value they replace.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryObjectAndListValueIsOfItsKind_ShouldLoad()
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(KindFieldJson(null, null), EnumImportResolver);

        Assert.AreEqual("test.kinds", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that a JSON property that must hold an object or an array, holding a value of another kind, is rejected
    /// with the <c>BODU-CAL-VALUE</c> diagnostic naming the property, rather than read as absent.
    /// </summary>
    /// <param name="field">The placeholder of the property holding the value.</param>
    /// <param name="property">The JSON name of the property.</param>
    /// <param name="literal">The value of the wrong kind, as a JSON literal.</param>
    [TestMethod]
    [DataRow("resolutionPolicy", "resolutionPolicy", "5")]
    [DataRow("categoryPrecedence", "categoryPrecedence", "\"PublicHoliday\"")]
    [DataRow("adjustmentPolicies", "adjustmentPolicies", "{}")]
    [DataRow("scope", "scope", "[]")]
    [DataRow("scope.territories", "territories", "\"AU\"")]
    [DataRow("scope.calendars", "calendars", "\"Gregorian\"")]
    [DataRow("scope.categories", "categories", "\"PublicHoliday\"")]
    [DataRow("scope.notableDateRefs", "notableDateRefs", "\"anchor\"")]
    [DataRow("scope.ruleRefs", "ruleRefs", "{ \"notableDateRef\": \"anchor\", \"ruleRef\": \"r\" }")]
    [DataRow("scope.onlyYears", "onlyYears", "2026")]
    [DataRow("trigger", "trigger", "\"IfWeekend\"")]
    [DataRow("trigger.weekdays", "weekdays", "\"Saturday\"")]
    [DataRow("action", "action", "true")]
    [DataRow("emission", "emission", "[]")]
    [DataRow("parameters", "parameters", "[ \"north\" ]")]
    [DataRow("imports", "imports", "\"lib\"")]
    [DataRow("uses", "uses", "{}")]
    [DataRow("use.adjustments", "adjustments", "\"policy\"")]
    [DataRow("notableDates", "notableDates", "{}")]
    [DataRow("notableDate.tags", "tags", "\"civic\"")]
    [DataRow("rules", "rules", "{}")]
    [DataRow("applicability", "applicability", "5")]
    [DataRow("applicability.territories", "territories", "\"AU\"")]
    [DataRow("applicability.exceptYears", "exceptYears", "2027")]
    [DataRow("strategy", "strategy", "[]")]
    [DataRow("fixed", "fixed", "5")]
    [DataRow("duration", "duration", "\"P3D\"")]
    [DataRow("untilDate", "untilDate", "[]")]
    [DataRow("untilDate.strategy", "strategy", "\"fixed\"")]
    [DataRow("rule.tags", "tags", "\"civic\"")]
    [DataRow("rule.adjustments", "adjustments", "\"policy\"")]
    [DataRow("recurrence", "recurrence", "[]")]
    [DataRow("weekly", "weekly", "\"Thursday\"")]
    [DataRow("weekly.days", "days", "\"Thursday\"")]
    [DataRow("overrides", "overrides", "{}")]
    [DataRow("patch.applicability", "applicability", "[]")]
    [DataRow("patch.tags", "tags", "\"civic\"")]
    [DataRow("patch.adjustments", "adjustments", "\"policy\"")]
    public void LoadJson_WhenObjectOrListValueIsOfAnotherKind_ShouldThrowValueDiagnostic(string field, string property, string literal)
    {
        string json = KindFieldJson(field, literal);

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Severity == NotableDateValidationSeverity.Error && d.Message.Contains("'" + property + "'", StringComparison.Ordinal), ex.Diagnostics, "BODU-CAL-VALUE");
    }

    /// <summary>
    /// Verifies that an optional JSON object- or list-valued property holding the literal <c>null</c> reads as absent,
    /// without a diagnostic.
    /// </summary>
    /// <param name="field">The placeholder of the property holding <c>null</c>.</param>
    [TestMethod]
    [DataRow("resolutionPolicy")]
    [DataRow("categoryPrecedence")]
    [DataRow("scope")]
    [DataRow("scope.territories")]
    [DataRow("parameters")]
    [DataRow("notableDate.tags")]
    [DataRow("applicability")]
    [DataRow("applicability.exceptYears")]
    [DataRow("duration")]
    [DataRow("rule.tags")]
    [DataRow("rule.adjustments")]
    [DataRow("patch.applicability")]
    [DataRow("patch.tags")]
    [DataRow("overrides")]
    public void LoadJson_WhenObjectOrListValueIsNull_ShouldReadItAsAbsent(string field)
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(KindFieldJson(field, "null"), EnumImportResolver);

        Assert.AreEqual("test.kinds", resource.ResourceId);
    }

    /// <summary>
    /// Builds a JSON document holding every object- and list-valued property the loader reads, each holding a value of
    /// its kind except the one named, which holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The placeholder of the property to set, or <see langword="null" /> to set none.</param>
    /// <param name="literal">The JSON literal the named property holds.</param>
    /// <returns>The JSON document.</returns>
    private static string KindFieldJson(string? field, string? literal)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.kinds", "resolutionPolicy": @resolutionPolicy@,
              "adjustmentPolicies": @adjustmentPolicies@, "imports": @imports@, "notableDates": @notableDates@, "overrides": @overrides@ }
            """;

        foreach ((string key, string wellFormed) in s_kindFieldValues)
            json = json.Replace("@" + key + "@", key == field ? literal : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Verifies that a JSON override whose <c>operation</c> names no operation, or is absent, is rejected with a single
    /// error diagnostic, as the schema rejects the unknown element in XML, rather than dropped without a report.
    /// </summary>
    /// <param name="operation">The <c>operation</c> property, as a JSON member, or empty to omit it.</param>
    [TestMethod]
    [DataRow("\"operation\": \"Patch\",")]
    [DataRow("\"operation\": \"UpdateRule\",")]
    [DataRow("\"operation\": \"\",")]
    [DataRow("\"operation\": 5,")]
    [DataRow("")]
    public void LoadJson_WhenOverrideOperationIsUnrecognized_ShouldThrowValidationException(string operation)
    {
        string json = OverrideJson($$"""{ {{operation}} "notableDateRef": "x", "ruleRef": "r", "priority": 7 }""");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.ContainsSingle(d => d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a JSON <c>AddRule</c> override without a <c>rule</c>, or whose <c>rule</c> is the literal
    /// <c>null</c>, is rejected with a single error diagnostic, as the schema rejects it in XML, rather than dropped
    /// without a report.
    /// </summary>
    /// <param name="rule">The <c>rule</c> property, as a JSON member, or empty to omit it.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(", \"rule\": null")]
    public void LoadJson_WhenAddRuleHasNoRule_ShouldThrowValidationException(string rule)
    {
        string json = OverrideJson($$"""{ "operation": "AddRule", "notableDateRef": "x"{{rule}} }""");

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.ContainsSingle(d => d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a JSON <c>PatchRule</c> operation named in another case, which the loader matches ignoring case as
    /// it matches every other name, patches the rule rather than being dropped.
    /// </summary>
    /// <param name="operation">The operation's name.</param>
    [TestMethod]
    [DataRow("patchRule")]
    [DataRow("PATCHRULE")]
    public void LoadJson_WhenPatchRuleOperationDiffersInCase_ShouldPatchTheRule(string operation)
    {
        string json = OverrideJson($$"""{ "operation": "{{operation}}", "notableDateRef": "x", "ruleRef": "r", "priority": 7 }""");

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);

        Assert.AreEqual(7, resource.NotableDates.Single(d => d.Id == "x").Rules.Single(r => r.Id == "r").Priority);
    }

    /// <summary>
    /// Verifies that a JSON <c>RemoveRule</c> operation named in another case, which the loader matches ignoring case as
    /// it matches every other name, removes the rule rather than being dropped.
    /// </summary>
    /// <param name="operation">The operation's name.</param>
    [TestMethod]
    [DataRow("removeRule")]
    [DataRow("removerule")]
    public void LoadJson_WhenRemoveRuleOperationDiffersInCase_ShouldRemoveTheRule(string operation)
    {
        string json = OverrideJson($$"""{ "operation": "{{operation}}", "notableDateRef": "x", "ruleRef": "s" }""");

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);

        Assert.DoesNotContain(r => r.Id == "s", resource.NotableDates.Single(d => d.Id == "x").Rules);
    }

    /// <summary>
    /// Verifies that a JSON <c>AddRule</c> operation named in another case, which the loader matches ignoring case as it
    /// matches every other name, adds the rule rather than being dropped.
    /// </summary>
    /// <param name="operation">The operation's name.</param>
    [TestMethod]
    [DataRow("addRule")]
    [DataRow("ADDRULE")]
    public void LoadJson_WhenAddRuleOperationDiffersInCase_ShouldAddTheRule(string operation)
    {
        string json = OverrideJson($$"""{ "operation": "{{operation}}", "notableDateRef": "x", "rule": { "id": "t", "strategy": { "fixed": { "month": 5, "day": 1 } } } }""");

        NotableDateResource resource = NotableDateResourceLoader.LoadJson(json);

        Assert.Contains(r => r.Id == "t", resource.NotableDates.Single(d => d.Id == "x").Rules);
    }

    /// <summary>
    /// Builds a JSON document whose one concept, <c>x</c>, holds the rules <c>r</c> (1 March) and <c>s</c> (1 April),
    /// with the supplied override.
    /// </summary>
    /// <param name="operation">The override, as a JSON object.</param>
    /// <returns>The JSON document.</returns>
    private static string OverrideJson(string operation) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.overrides",
          "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance",
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } },
                       { "id": "s", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ],
          "overrides": [ @operation@ ] }
        """.Replace("@operation@", operation, StringComparison.Ordinal);

    /// <summary>
    /// Verifies that a JSON scalar field holding an object or an array is reported with a <c>BODU-CAL-VALUE</c> message
    /// that names the value's JSON kind rather than quoting its text, so the document builder, which no longer holds the
    /// text as written, can report it with the same message.
    /// </summary>
    /// <param name="field">The JSON property holding the value.</param>
    /// <param name="literal">The object or array, as a JSON literal.</param>
    /// <param name="kind">The value's JSON kind, as the message names it.</param>
    [TestMethod]
    [DataRow("priority", "{ }", "object")]
    [DataRow("category", "{ \"en\": \"X\" }", "object")]
    [DataRow("displayName", "[ 1, 2 ]", "array")]
    [DataRow("day", "{ \"value\": 1 }", "object")]
    public void LoadJson_WhenScalarValueIsObjectOrArray_ShouldNameItsKind(string field, string literal, string kind)
    {
        string json = ScalarValueJson(field, literal);
        string expected = $"The '{field}' property holds a JSON {kind}, which is not a string, number or Boolean.";

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Message == expected, ex.Diagnostics, expected);
    }

    /// <summary>
    /// Verifies that an item of a JSON list of territory codes, references or tags that is not a string is reported with
    /// a <c>BODU-CAL-VALUE</c> message that names the item's JSON kind rather than quoting its text.
    /// </summary>
    /// <param name="placeholder">The placeholder of the list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    /// <param name="field">The name of the list, as the message names it.</param>
    /// <param name="kind">The item's JSON kind, as the message names it.</param>
    [TestMethod]
    [DataRow("scope.territories", "36", "scope territories", "number")]
    [DataRow("scope.notableDateRefs", "{ }", "scope notableDateRefs", "object")]
    [DataRow("use.adjustments", "5", "import use adjustments", "number")]
    [DataRow("notableDate.tags", "{ \"a\": 1 }", "tags", "object")]
    [DataRow("applicability.territories", "[ ]", "applicability territories", "array")]
    [DataRow("rule.tags", "true", "rule tags", "Boolean")]
    [DataRow("rule.adjustments", "[ \"policy\" ]", "rule adjustments", "array")]
    [DataRow("patch.tags", "null", "patch tags", "null")]
    [DataRow("patch.adjustments", "false", "patch adjustments", "Boolean")]
    public void LoadJson_WhenTextListItemIsNotString_ShouldNameItsKind(string placeholder, string item, string field, string kind)
    {
        string json = ListFieldJson(placeholder, item);
        string expected = $"The {field} list holds a JSON {kind} item, which is not a string.";

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Message == expected, ex.Diagnostics, expected);
    }

    /// <summary>
    /// Verifies that an item of a JSON scope's <c>ruleRefs</c> that is neither a rule identifier nor a reference object
    /// is reported with a <c>BODU-CAL-VALUE</c> message that names the item's JSON kind rather than quoting its text.
    /// </summary>
    /// <param name="item">The item, as a JSON literal.</param>
    /// <param name="kind">The item's JSON kind, as the message names it.</param>
    [TestMethod]
    [DataRow("5", "number")]
    [DataRow("true", "Boolean")]
    [DataRow("null", "null")]
    [DataRow("[ \"r\" ]", "array")]
    public void LoadJson_WhenScopeRuleReferenceIsNeitherStringNorObject_ShouldNameItsKind(string item, string kind)
    {
        string json = ListFieldJson("scope.ruleRefs", item);
        string expected = $"The scope ruleRefs list holds a JSON {kind} item, which is neither a rule identifier nor a reference object.";

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Message == expected, ex.Diagnostics, expected);
    }

    /// <summary>
    /// Verifies that an item of a JSON list of enumeration names that is an object or an array, which no name is, is
    /// reported with a <c>BODU-CAL-VALUE</c> message that names the item's JSON kind, as a scalar field holding one is,
    /// rather than quoting its text as a name.
    /// </summary>
    /// <param name="placeholder">The placeholder of the list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    /// <param name="field">The name of the list, as the message names it.</param>
    /// <param name="kind">The item's JSON kind, as the message names it.</param>
    [TestMethod]
    [DataRow("categoryPrecedence", "{ }", "categoryPrecedence", "object")]
    [DataRow("scope.calendars", "[ \"Gregorian\" ]", "scope calendars", "array")]
    [DataRow("scope.categories", "{ \"a\": 1 }", "scope categories", "object")]
    [DataRow("trigger.weekdays", "[ ]", "trigger weekdays", "array")]
    [DataRow("weekly.days", "{}", "weekly days", "object")]
    public void LoadJson_WhenNameListItemIsObjectOrArray_ShouldNameItsKind(string placeholder, string item, string field, string kind)
    {
        string json = ListFieldJson(placeholder, item);
        string expected = $"The {field} list holds a JSON {kind} item, which is not a string, number or Boolean.";

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Message == expected, ex.Diagnostics, expected);
    }

    /// <summary>
    /// Verifies that an item of a JSON year list that is an object or an array, which no integer is, is reported with a
    /// <c>BODU-CAL-VALUE</c> message that names the item's JSON kind, as a scalar field holding one is, rather than
    /// quoting its text as an integer.
    /// </summary>
    /// <param name="placeholder">The placeholder of the year list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    /// <param name="field">The name of the list, as the message names it.</param>
    /// <param name="kind">The item's JSON kind, as the message names it.</param>
    [TestMethod]
    [DataRow("scope.onlyYears", "{ \"year\": 2026 }", "scope onlyYears", "object")]
    [DataRow("scope.exceptYears", "[ 2027 ]", "scope exceptYears", "array")]
    [DataRow("applicability.onlyYears", "[ ]", "applicability onlyYears", "array")]
    [DataRow("applicability.exceptYears", "{ }", "applicability exceptYears", "object")]
    public void LoadJson_WhenYearListItemIsObjectOrArray_ShouldNameItsKind(string placeholder, string item, string field, string kind)
    {
        string json = ScalarFieldJson(placeholder, item);
        string expected = $"The {field} list holds a JSON {kind} item, which is not a string, number or Boolean.";

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        Assert.Contains(d => d.Code == "BODU-CAL-VALUE" && d.Message == expected, ex.Diagnostics, expected);
    }

    /// <summary>
    /// Verifies that the object-list test document loads when every list of objects holds only objects, so each
    /// rejection the per-list tests observe is caused by the one item they add.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEveryObjectListHoldsOnlyObjects_ShouldLoad()
    {
        NotableDateResource resource = NotableDateResourceLoader.LoadJson(ObjectItemJson(null, null), EnumImportResolver);

        Assert.AreEqual("test.items", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an item of a JSON list of objects (concepts, rules, adjustment policies, imports, uses or
    /// overrides) that is not an object is rejected with a single <c>BODU-CAL-VALUE</c> error naming the property and
    /// the item's JSON kind, rather than read as an object with every property absent or dropped without a report.
    /// </summary>
    /// <param name="placeholder">The placeholder before which the item is added.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    /// <param name="property">The JSON name of the list.</param>
    /// <param name="kind">The item's JSON kind, as the message names it.</param>
    [TestMethod]
    [DataRow("notableDate.item", "5", "notableDates", "number")]
    [DataRow("notableDate.item", "[ ]", "notableDates", "array")]
    [DataRow("policy.item", "true", "adjustmentPolicies", "Boolean")]
    [DataRow("rule.item", "\"r\"", "rules", "string")]
    [DataRow("import.item", "null", "imports", "null")]
    [DataRow("use.item", "5", "uses", "number")]
    [DataRow("override.item", "5", "overrides", "number")]
    [DataRow("override.item", "\"PatchRule\"", "overrides", "string")]
    public void LoadJson_WhenObjectListHoldsNonObjectItem_ShouldThrowValueDiagnostic(string placeholder, string item, string property, string kind)
    {
        string json = ObjectItemJson(placeholder, item);
        string expected = $"The '{property}' property holds a JSON {kind} item, which is not an object.";

        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, EnumImportResolver);
        });

        NotableDateValidationDiagnostic diagnostic = Assert.ContainsSingle(d => d.Severity == NotableDateValidationSeverity.Error, ex.Diagnostics);
        Assert.AreEqual("BODU-CAL-VALUE", diagnostic.Code);
        Assert.AreEqual(expected, diagnostic.Message);
    }

    /// <summary>
    /// Builds a JSON document whose lists of concepts, rules, adjustment policies, imports, uses and overrides each
    /// hold one well-formed object, with the supplied item added before the object of the named list.
    /// </summary>
    /// <param name="placeholder">The placeholder of the list to add the item to, or <see langword="null" /> for none.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    /// <returns>The JSON document.</returns>
    private static string ObjectItemJson(string? placeholder, string? item)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.items",
              "adjustmentPolicies": [ @policy.item@{ "id": "policy", "trigger": { "type": "IfWeekend" },
                "action": { "type": "MoveToNextWorkingDay" }, "emission": { "mode": "ObservedOnly" } } ],
              "imports": [ @import.item@{ "resource": "lib", "uses": [ @use.item@{ "notableDateRef": "lib-day" } ] } ],
              "notableDates": [ @notableDate.item@{ "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday",
                "rules": [ @rule.item@{ "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } }, "adjustments": [ "policy" ] } ] } ],
              "overrides": [ @override.item@{ "operation": "PatchRule", "notableDateRef": "anchor", "ruleRef": "r", "priority": 2 } ] }
            """;

        foreach (string key in new[] { "policy.item", "import.item", "use.item", "notableDate.item", "rule.item", "override.item" })
            json = json.Replace("@" + key + "@", key == placeholder ? item + ", " : string.Empty, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// The JSON document under the schema tests: one of every object the loader reads, each holding every property the
    /// schema requires of it and every integer the schema bounds. <see cref="SchemaTemplateXml" /> is its XML twin.
    /// </summary>
    private const string SchemaTemplateJson = """
        { "schemaVersion": "1.0", "resourceId": "test.schema",
          "adjustmentPolicies": [ { "id": "policy", "priority": 1,
            "scope": { "fromYear": 2000, "toYear": 2100, "ruleRefs": [ { "notableDateRef": "anchor", "ruleRef": "r" } ],
              "onlyYears": [ 2026 ], "exceptYears": [ 2027 ] },
            "trigger": { "type": "IfBeforeFixedDate", "month": "June", "day": 15 },
            "action": { "type": "AddDays", "days": 1, "maxSearchDays": 7 },
            "emission": { "mode": "ObservedOnly" } } ],
          "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day" } ] } ],
          "notableDates": [
            { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday", "defaultDurationDays": 1,
              "rules": [ { "id": "r", "durationDays": 2,
                "applicability": { "fromYear": 2000, "toYear": 2100, "everyYears": 1, "anchorYear": 2000, "onlyYears": [ 2026 ], "exceptYears": [ 2027 ] },
                "strategy": { "fixed": { "month": 3, "day": 1 } }, "adjustments": [ "policy" ] } ] },
            { "id": "kinds", "displayName": "Kinds", "category": "Observance",
              "rules": [
                { "id": "day-of-week-in-month", "strategy": { "dayOfWeekInMonth": { "month": 5, "dayOfWeek": "Monday", "weekOrdinal": "First" } } },
                { "id": "weekday-near-date", "strategy": { "weekdayNearDate": { "month": 5, "day": 10, "dayOfWeek": "Monday", "direction": "OnOrAfter" } } },
                { "id": "relative", "strategy": { "relativeWeekdayInMonth": { "month": 11, "dayOfWeek": "Thursday", "weekOrdinal": "Fourth", "relativeDayOfWeek": "Friday", "direction": "After" } } },
                { "id": "offset", "strategy": { "offsetFromRule": { "notableDateRef": "anchor", "offsetDays": 3 } } },
                { "id": "algorithm", "strategy": { "algorithm": { "key": "western-easter" } } },
                { "id": "near-rule", "strategy": { "weekdayNearRule": { "notableDateRef": "anchor", "dayOfWeek": "Friday", "direction": "Before" } } },
                { "id": "nth-from-rule", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "anchor", "dayOfWeek": "Sunday", "ordinal": 2 } } },
                { "id": "working-offset", "strategy": { "workingDayOffsetFromRule": { "notableDateRef": "anchor", "offsetWorkingDays": -1 } } },
                { "id": "working-in-month", "strategy": { "workingDayInMonth": { "month": 6, "ordinal": 1 } } },
                { "id": "ordinal-day", "strategy": { "ordinalDayOfMonth": { "month": 7, "ordinal": 2 } } },
                { "id": "iso-week", "strategy": { "isoWeekDate": { "week": 20, "dayOfWeek": "Wednesday" } } },
                { "id": "day-of-year", "strategy": { "dayOfYear": { "ordinal": 100 } } },
                { "id": "span", "strategy": { "fixed": { "month": 8, "day": 1 } },
                  "duration": { "untilDate": { "strategy": { "fixed": { "month": 8, "day": 3 } } } } },
                { "id": "daily", "recurrence": { "dailyInterval": { "anchorDate": "2024-01-01", "intervalDays": 7 } } },
                { "id": "weekly", "recurrence": { "weekly": { "days": [ "Thursday" ], "intervalWeeks": 2, "anchorDate": "2024-01-04" } } },
                { "id": "monthly-day", "recurrence": { "monthlyDay": { "dayOfMonth": 15, "intervalMonths": 1 } } },
                { "id": "monthly-weekday", "recurrence": { "monthlyWeekday": { "dayOfWeek": "Friday", "weekOrdinal": "Second", "intervalMonths": 1 } } },
                { "id": "gone", "strategy": { "fixed": { "month": 10, "day": 1 } } } ] } ],
          "overrides": [
            { "operation": "PatchRule", "notableDateRef": "anchor", "ruleRef": "r", "durationDays": 1 },
            { "operation": "RemoveRule", "notableDateRef": "kinds", "ruleRef": "gone" },
            { "operation": "AddRule", "notableDateRef": "kinds", "rule": { "id": "added", "strategy": { "fixed": { "month": 9, "day": 1 } } } } ] }
        """;

    /// <summary>
    /// The XML twin of <see cref="SchemaTemplateJson" />, which loads into the same resource.
    /// </summary>
    private const string SchemaTemplateXml = """
        <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.schema">
          <AdjustmentPolicies>
            <AdjustmentPolicy id="policy" priority="1">
              <Scope fromYear="2000" toYear="2100">
                <Rule notableDateRef="anchor" ruleRef="r" />
                <OnlyYear value="2026" />
                <ExceptYear value="2027" />
              </Scope>
              <Trigger type="IfBeforeFixedDate" month="June" day="15" />
              <Action type="AddDays" days="1" maxSearchDays="7" />
              <Emission mode="ObservedOnly" />
            </AdjustmentPolicy>
          </AdjustmentPolicies>
          <Imports>
            <Import resource="lib"><Use notableDateRef="lib-day" /></Import>
          </Imports>
          <NotableDates>
            <NotableDate id="anchor" displayName="Anchor" category="PublicHoliday" defaultDurationDays="1">
              <Rules>
                <Rule id="r" durationDays="2">
                  <Applicability fromYear="2000" toYear="2100" everyYears="1" anchorYear="2000">
                    <OnlyYear value="2026" />
                    <ExceptYear value="2027" />
                  </Applicability>
                  <Strategy><Fixed month="3" day="1" /></Strategy>
                  <Adjustments><Adjustment policyRef="policy" /></Adjustments>
                </Rule>
              </Rules>
            </NotableDate>
            <NotableDate id="kinds" displayName="Kinds" category="Observance">
              <Rules>
                <Rule id="day-of-week-in-month"><Strategy><DayOfWeekInMonth month="5" dayOfWeek="Monday" weekOrdinal="First" /></Strategy></Rule>
                <Rule id="weekday-near-date"><Strategy><WeekdayNearDate month="5" day="10" dayOfWeek="Monday" direction="OnOrAfter" /></Strategy></Rule>
                <Rule id="relative"><Strategy><RelativeWeekdayInMonth month="11" dayOfWeek="Thursday" weekOrdinal="Fourth" relativeDayOfWeek="Friday" direction="After" /></Strategy></Rule>
                <Rule id="offset"><Strategy><OffsetFromRule notableDateRef="anchor" offsetDays="3" /></Strategy></Rule>
                <Rule id="algorithm"><Strategy><Algorithm key="western-easter" /></Strategy></Rule>
                <Rule id="near-rule"><Strategy><WeekdayNearRule notableDateRef="anchor" dayOfWeek="Friday" direction="Before" /></Strategy></Rule>
                <Rule id="nth-from-rule"><Strategy><NthWeekdayFromRule notableDateRef="anchor" dayOfWeek="Sunday" ordinal="2" /></Strategy></Rule>
                <Rule id="working-offset"><Strategy><WorkingDayOffsetFromRule notableDateRef="anchor" offsetWorkingDays="-1" /></Strategy></Rule>
                <Rule id="working-in-month"><Strategy><WorkingDayInMonth month="6" ordinal="1" /></Strategy></Rule>
                <Rule id="ordinal-day"><Strategy><OrdinalDayOfMonth month="7" ordinal="2" /></Strategy></Rule>
                <Rule id="iso-week"><Strategy><IsoWeekDate week="20" dayOfWeek="Wednesday" /></Strategy></Rule>
                <Rule id="day-of-year"><Strategy><DayOfYear ordinal="100" /></Strategy></Rule>
                <Rule id="span">
                  <Strategy><Fixed month="8" day="1" /></Strategy>
                  <Duration><UntilDate><Strategy><Fixed month="8" day="3" /></Strategy></UntilDate></Duration>
                </Rule>
                <Rule id="daily"><Recurrence><DailyInterval anchorDate="2024-01-01" intervalDays="7" /></Recurrence></Rule>
                <Rule id="weekly"><Recurrence><Weekly intervalWeeks="2" anchorDate="2024-01-04"><Day dayOfWeek="Thursday" /></Weekly></Recurrence></Rule>
                <Rule id="monthly-day"><Recurrence><MonthlyDay dayOfMonth="15" intervalMonths="1" /></Recurrence></Rule>
                <Rule id="monthly-weekday"><Recurrence><MonthlyWeekday dayOfWeek="Friday" weekOrdinal="Second" intervalMonths="1" /></Recurrence></Rule>
                <Rule id="gone"><Strategy><Fixed month="10" day="1" /></Strategy></Rule>
              </Rules>
            </NotableDate>
          </NotableDates>
          <Overrides>
            <PatchRule notableDateRef="anchor" ruleRef="r" durationDays="1" />
            <RemoveRule notableDateRef="kinds" ruleRef="gone" />
            <AddRule notableDateRef="kinds"><Rule id="added"><Strategy><Fixed month="9" day="1" /></Strategy></Rule></AddRule>
          </Overrides>
        </NotableDateResource>
        """;

    /// <summary>
    /// The location of each field the schema tests set or remove, keyed by the name that stands for it: a JSON pointer
    /// into <see cref="SchemaTemplateJson" /> and an XPath into <see cref="SchemaTemplateXml" />.
    /// </summary>
    private static readonly Dictionary<string, (string Json, string Xml)> s_schemaFields = new(StringComparer.Ordinal)
    {
        ["schemaVersion"] = ("/schemaVersion", "/nd:NotableDateResource/@schemaVersion"),
        ["resourceId"] = ("/resourceId", "/nd:NotableDateResource/@resourceId"),
        ["policy.id"] = ("/adjustmentPolicies/0/id", "//nd:AdjustmentPolicy/@id"),
        ["policy.trigger"] = ("/adjustmentPolicies/0/trigger", "//nd:Trigger"),
        ["policy.action"] = ("/adjustmentPolicies/0/action", "//nd:Action"),
        ["policy.emission"] = ("/adjustmentPolicies/0/emission", "//nd:Emission"),
        ["scope.fromYear"] = ("/adjustmentPolicies/0/scope/fromYear", "//nd:Scope/@fromYear"),
        ["scope.toYear"] = ("/adjustmentPolicies/0/scope/toYear", "//nd:Scope/@toYear"),
        ["scope.ruleRefs.notableDateRef"] = ("/adjustmentPolicies/0/scope/ruleRefs/0/notableDateRef", "//nd:Scope/nd:Rule/@notableDateRef"),
        ["scope.ruleRefs.ruleRef"] = ("/adjustmentPolicies/0/scope/ruleRefs/0/ruleRef", "//nd:Scope/nd:Rule/@ruleRef"),
        ["scope.onlyYears.item"] = ("/adjustmentPolicies/0/scope/onlyYears/0", "//nd:Scope/nd:OnlyYear/@value"),
        ["scope.exceptYears.item"] = ("/adjustmentPolicies/0/scope/exceptYears/0", "//nd:Scope/nd:ExceptYear/@value"),
        ["trigger.type"] = ("/adjustmentPolicies/0/trigger/type", "//nd:Trigger/@type"),
        ["trigger.day"] = ("/adjustmentPolicies/0/trigger/day", "//nd:Trigger/@day"),
        ["action.type"] = ("/adjustmentPolicies/0/action/type", "//nd:Action/@type"),
        ["action.maxSearchDays"] = ("/adjustmentPolicies/0/action/maxSearchDays", "//nd:Action/@maxSearchDays"),
        ["emission.mode"] = ("/adjustmentPolicies/0/emission/mode", "//nd:Emission/@mode"),
        ["import.resource"] = ("/imports/0/resource", "//nd:Import/@resource"),
        ["use.notableDateRef"] = ("/imports/0/uses/0/notableDateRef", "//nd:Use/@notableDateRef"),
        ["notableDate.id"] = ("/notableDates/0/id", "//nd:NotableDate[@id='anchor']/@id"),
        ["notableDate.displayName"] = ("/notableDates/0/displayName", "//nd:NotableDate[@id='anchor']/@displayName"),
        ["notableDate.category"] = ("/notableDates/0/category", "//nd:NotableDate[@id='anchor']/@category"),
        ["notableDate.defaultDurationDays"] = ("/notableDates/0/defaultDurationDays", "//nd:NotableDate[@id='anchor']/@defaultDurationDays"),
        ["notableDate.rules"] = ("/notableDates/1/rules", "//nd:NotableDate[@id='kinds']/nd:Rules"),
        ["notableDate.rules.items"] = ("/notableDates/1/rules", "//nd:NotableDate[@id='kinds']/nd:Rules/nd:Rule"),
        ["rule.id"] = ("/notableDates/1/rules/0/id", "//nd:Rule[@id='day-of-week-in-month']/@id"),
        ["rule.durationDays"] = ("/notableDates/0/rules/0/durationDays", "//nd:Rule[@id='r']/@durationDays"),
        ["applicability.fromYear"] = ("/notableDates/0/rules/0/applicability/fromYear", "//nd:Applicability/@fromYear"),
        ["applicability.toYear"] = ("/notableDates/0/rules/0/applicability/toYear", "//nd:Applicability/@toYear"),
        ["applicability.everyYears"] = ("/notableDates/0/rules/0/applicability/everyYears", "//nd:Applicability/@everyYears"),
        ["applicability.anchorYear"] = ("/notableDates/0/rules/0/applicability/anchorYear", "//nd:Applicability/@anchorYear"),
        ["applicability.onlyYears.item"] = ("/notableDates/0/rules/0/applicability/onlyYears/0", "//nd:Applicability/nd:OnlyYear/@value"),
        ["applicability.exceptYears.item"] = ("/notableDates/0/rules/0/applicability/exceptYears/0", "//nd:Applicability/nd:ExceptYear/@value"),
        ["fixed.month"] = ("/notableDates/0/rules/0/strategy/fixed/month", "//nd:Rule[@id='r']//nd:Fixed/@month"),
        ["fixed.day"] = ("/notableDates/0/rules/0/strategy/fixed/day", "//nd:Rule[@id='r']//nd:Fixed/@day"),
        ["dayOfWeekInMonth.month"] = ("/notableDates/1/rules/0/strategy/dayOfWeekInMonth/month", "//nd:DayOfWeekInMonth/@month"),
        ["dayOfWeekInMonth.dayOfWeek"] = ("/notableDates/1/rules/0/strategy/dayOfWeekInMonth/dayOfWeek", "//nd:DayOfWeekInMonth/@dayOfWeek"),
        ["dayOfWeekInMonth.weekOrdinal"] = ("/notableDates/1/rules/0/strategy/dayOfWeekInMonth/weekOrdinal", "//nd:DayOfWeekInMonth/@weekOrdinal"),
        ["weekdayNearDate.month"] = ("/notableDates/1/rules/1/strategy/weekdayNearDate/month", "//nd:WeekdayNearDate/@month"),
        ["weekdayNearDate.day"] = ("/notableDates/1/rules/1/strategy/weekdayNearDate/day", "//nd:WeekdayNearDate/@day"),
        ["weekdayNearDate.dayOfWeek"] = ("/notableDates/1/rules/1/strategy/weekdayNearDate/dayOfWeek", "//nd:WeekdayNearDate/@dayOfWeek"),
        ["weekdayNearDate.direction"] = ("/notableDates/1/rules/1/strategy/weekdayNearDate/direction", "//nd:WeekdayNearDate/@direction"),
        ["relativeWeekdayInMonth.month"] = ("/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/month", "//nd:RelativeWeekdayInMonth/@month"),
        ["relativeWeekdayInMonth.dayOfWeek"] = ("/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/dayOfWeek", "//nd:RelativeWeekdayInMonth/@dayOfWeek"),
        ["relativeWeekdayInMonth.weekOrdinal"] = ("/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/weekOrdinal", "//nd:RelativeWeekdayInMonth/@weekOrdinal"),
        ["relativeWeekdayInMonth.relativeDayOfWeek"] = ("/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/relativeDayOfWeek", "//nd:RelativeWeekdayInMonth/@relativeDayOfWeek"),
        ["relativeWeekdayInMonth.direction"] = ("/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/direction", "//nd:RelativeWeekdayInMonth/@direction"),
        ["offsetFromRule.notableDateRef"] = ("/notableDates/1/rules/3/strategy/offsetFromRule/notableDateRef", "//nd:OffsetFromRule/@notableDateRef"),
        ["offsetFromRule.offsetDays"] = ("/notableDates/1/rules/3/strategy/offsetFromRule/offsetDays", "//nd:OffsetFromRule/@offsetDays"),
        ["algorithm.key"] = ("/notableDates/1/rules/4/strategy/algorithm/key", "//nd:Algorithm/@key"),
        ["weekdayNearRule.notableDateRef"] = ("/notableDates/1/rules/5/strategy/weekdayNearRule/notableDateRef", "//nd:WeekdayNearRule/@notableDateRef"),
        ["weekdayNearRule.dayOfWeek"] = ("/notableDates/1/rules/5/strategy/weekdayNearRule/dayOfWeek", "//nd:WeekdayNearRule/@dayOfWeek"),
        ["weekdayNearRule.direction"] = ("/notableDates/1/rules/5/strategy/weekdayNearRule/direction", "//nd:WeekdayNearRule/@direction"),
        ["nthWeekdayFromRule.notableDateRef"] = ("/notableDates/1/rules/6/strategy/nthWeekdayFromRule/notableDateRef", "//nd:NthWeekdayFromRule/@notableDateRef"),
        ["nthWeekdayFromRule.dayOfWeek"] = ("/notableDates/1/rules/6/strategy/nthWeekdayFromRule/dayOfWeek", "//nd:NthWeekdayFromRule/@dayOfWeek"),
        ["nthWeekdayFromRule.ordinal"] = ("/notableDates/1/rules/6/strategy/nthWeekdayFromRule/ordinal", "//nd:NthWeekdayFromRule/@ordinal"),
        ["workingDayOffsetFromRule.notableDateRef"] = ("/notableDates/1/rules/7/strategy/workingDayOffsetFromRule/notableDateRef", "//nd:WorkingDayOffsetFromRule/@notableDateRef"),
        ["workingDayOffsetFromRule.offsetWorkingDays"] = ("/notableDates/1/rules/7/strategy/workingDayOffsetFromRule/offsetWorkingDays", "//nd:WorkingDayOffsetFromRule/@offsetWorkingDays"),
        ["workingDayInMonth.month"] = ("/notableDates/1/rules/8/strategy/workingDayInMonth/month", "//nd:WorkingDayInMonth/@month"),
        ["workingDayInMonth.ordinal"] = ("/notableDates/1/rules/8/strategy/workingDayInMonth/ordinal", "//nd:WorkingDayInMonth/@ordinal"),
        ["ordinalDayOfMonth.month"] = ("/notableDates/1/rules/9/strategy/ordinalDayOfMonth/month", "//nd:OrdinalDayOfMonth/@month"),
        ["ordinalDayOfMonth.ordinal"] = ("/notableDates/1/rules/9/strategy/ordinalDayOfMonth/ordinal", "//nd:OrdinalDayOfMonth/@ordinal"),
        ["isoWeekDate.week"] = ("/notableDates/1/rules/10/strategy/isoWeekDate/week", "//nd:IsoWeekDate/@week"),
        ["isoWeekDate.dayOfWeek"] = ("/notableDates/1/rules/10/strategy/isoWeekDate/dayOfWeek", "//nd:IsoWeekDate/@dayOfWeek"),
        ["dayOfYear.ordinal"] = ("/notableDates/1/rules/11/strategy/dayOfYear/ordinal", "//nd:DayOfYear/@ordinal"),
        ["duration.untilDate"] = ("/notableDates/1/rules/12/duration/untilDate", "//nd:Rule[@id='span']/nd:Duration/nd:UntilDate"),
        ["untilDate.fixed.month"] = ("/notableDates/1/rules/12/duration/untilDate/strategy/fixed/month", "//nd:UntilDate//nd:Fixed/@month"),
        ["untilDate.fixed.day"] = ("/notableDates/1/rules/12/duration/untilDate/strategy/fixed/day", "//nd:UntilDate//nd:Fixed/@day"),
        ["dailyInterval.anchorDate"] = ("/notableDates/1/rules/13/recurrence/dailyInterval/anchorDate", "//nd:DailyInterval/@anchorDate"),
        ["dailyInterval.intervalDays"] = ("/notableDates/1/rules/13/recurrence/dailyInterval/intervalDays", "//nd:DailyInterval/@intervalDays"),
        ["weekly.days"] = ("/notableDates/1/rules/14/recurrence/weekly/days", "//nd:Weekly/nd:Day"),
        ["weekly.intervalWeeks"] = ("/notableDates/1/rules/14/recurrence/weekly/intervalWeeks", "//nd:Weekly/@intervalWeeks"),
        ["monthlyDay.dayOfMonth"] = ("/notableDates/1/rules/15/recurrence/monthlyDay/dayOfMonth", "//nd:MonthlyDay/@dayOfMonth"),
        ["monthlyDay.intervalMonths"] = ("/notableDates/1/rules/15/recurrence/monthlyDay/intervalMonths", "//nd:MonthlyDay/@intervalMonths"),
        ["monthlyWeekday.dayOfWeek"] = ("/notableDates/1/rules/16/recurrence/monthlyWeekday/dayOfWeek", "//nd:MonthlyWeekday/@dayOfWeek"),
        ["monthlyWeekday.weekOrdinal"] = ("/notableDates/1/rules/16/recurrence/monthlyWeekday/weekOrdinal", "//nd:MonthlyWeekday/@weekOrdinal"),
        ["monthlyWeekday.intervalMonths"] = ("/notableDates/1/rules/16/recurrence/monthlyWeekday/intervalMonths", "//nd:MonthlyWeekday/@intervalMonths"),
        ["patch.notableDateRef"] = ("/overrides/0/notableDateRef", "//nd:PatchRule/@notableDateRef"),
        ["patch.ruleRef"] = ("/overrides/0/ruleRef", "//nd:PatchRule/@ruleRef"),
        ["patch.durationDays"] = ("/overrides/0/durationDays", "//nd:PatchRule/@durationDays"),
        ["remove.notableDateRef"] = ("/overrides/1/notableDateRef", "//nd:RemoveRule/@notableDateRef"),
        ["remove.ruleRef"] = ("/overrides/1/ruleRef", "//nd:RemoveRule/@ruleRef"),
        ["add.notableDateRef"] = ("/overrides/2/notableDateRef", "//nd:AddRule/@notableDateRef"),
        ["add.rule"] = ("/overrides/2/rule", "//nd:AddRule/nd:Rule"),
        ["add.rule.id"] = ("/overrides/2/rule/id", "//nd:AddRule/nd:Rule/@id"),
        ["add.fixed.day"] = ("/overrides/2/rule/strategy/fixed/day", "//nd:AddRule//nd:Fixed/@day"),
    };

    /// <summary>
    /// Verifies that the schema test document and its XML twin both load, into the same resource, so each rejection the
    /// per-field tests observe is caused by the one field they change.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenEverySchemaRequiredValueIsPresent_ShouldLoadWhatLoadLoads()
    {
        NotableDateResource fromJson = NotableDateResourceLoader.LoadJson(SchemaFieldJson(null, null), EnumImportResolver);
        NotableDateResource fromXml = NotableDateResourceLoader.Load(SchemaFieldXml(null, null), EnumImportResolver);

        CollectionAssert.AreEqual(SchemaPackBytes(fromXml), SchemaPackBytes(fromJson));
    }

    /// <summary>
    /// Verifies that a JSON document without a property the schema requires, which the same document in XML fails, is
    /// reported with <c>BODU-CAL-SCHEMA</c> and the other diagnostic codes the XML document reports, rather than read
    /// with a default in the property's place.
    /// </summary>
    /// <param name="field">The name of the required property removed from both documents.</param>
    [TestMethod]
    [DataRow("schemaVersion")]
    [DataRow("resourceId")]
    [DataRow("policy.id")]
    [DataRow("policy.trigger")]
    [DataRow("policy.action")]
    [DataRow("policy.emission")]
    [DataRow("scope.ruleRefs.notableDateRef")]
    [DataRow("scope.ruleRefs.ruleRef")]
    [DataRow("trigger.type")]
    [DataRow("action.type")]
    [DataRow("emission.mode")]
    [DataRow("import.resource")]
    [DataRow("use.notableDateRef")]
    [DataRow("notableDate.id")]
    [DataRow("notableDate.displayName")]
    [DataRow("notableDate.category")]
    [DataRow("notableDate.rules")]
    [DataRow("rule.id")]
    [DataRow("fixed.month")]
    [DataRow("fixed.day")]
    [DataRow("dayOfWeekInMonth.month")]
    [DataRow("dayOfWeekInMonth.dayOfWeek")]
    [DataRow("dayOfWeekInMonth.weekOrdinal")]
    [DataRow("weekdayNearDate.month")]
    [DataRow("weekdayNearDate.day")]
    [DataRow("weekdayNearDate.dayOfWeek")]
    [DataRow("weekdayNearDate.direction")]
    [DataRow("relativeWeekdayInMonth.month")]
    [DataRow("relativeWeekdayInMonth.dayOfWeek")]
    [DataRow("relativeWeekdayInMonth.weekOrdinal")]
    [DataRow("relativeWeekdayInMonth.relativeDayOfWeek")]
    [DataRow("relativeWeekdayInMonth.direction")]
    [DataRow("offsetFromRule.notableDateRef")]
    [DataRow("offsetFromRule.offsetDays")]
    [DataRow("algorithm.key")]
    [DataRow("weekdayNearRule.notableDateRef")]
    [DataRow("weekdayNearRule.dayOfWeek")]
    [DataRow("weekdayNearRule.direction")]
    [DataRow("nthWeekdayFromRule.notableDateRef")]
    [DataRow("nthWeekdayFromRule.dayOfWeek")]
    [DataRow("nthWeekdayFromRule.ordinal")]
    [DataRow("workingDayOffsetFromRule.notableDateRef")]
    [DataRow("workingDayOffsetFromRule.offsetWorkingDays")]
    [DataRow("workingDayInMonth.month")]
    [DataRow("workingDayInMonth.ordinal")]
    [DataRow("ordinalDayOfMonth.month")]
    [DataRow("ordinalDayOfMonth.ordinal")]
    [DataRow("isoWeekDate.week")]
    [DataRow("isoWeekDate.dayOfWeek")]
    [DataRow("dayOfYear.ordinal")]
    [DataRow("duration.untilDate")]
    [DataRow("untilDate.fixed.month")]
    [DataRow("untilDate.fixed.day")]
    [DataRow("dailyInterval.anchorDate")]
    [DataRow("weekly.days")]
    [DataRow("monthlyDay.dayOfMonth")]
    [DataRow("monthlyWeekday.dayOfWeek")]
    [DataRow("monthlyWeekday.weekOrdinal")]
    [DataRow("patch.notableDateRef")]
    [DataRow("patch.ruleRef")]
    [DataRow("remove.notableDateRef")]
    [DataRow("remove.ruleRef")]
    [DataRow("add.notableDateRef")]
    [DataRow("add.rule")]
    [DataRow("add.rule.id")]
    public void LoadJson_WhenSchemaRequiredPropertyIsAbsent_ShouldReportWhatLoadReports(string field)
    {
        AssertSchemaCodesMatch(SchemaFieldJson(field, null), SchemaFieldXml(field, null));
    }

    /// <summary>
    /// Verifies that a JSON property the schema requires, holding the literal <c>null</c>, which reads as absent, is
    /// reported as the absent property is: with <c>BODU-CAL-SCHEMA</c> and the other diagnostic codes the XML document
    /// without it reports.
    /// </summary>
    /// <param name="field">The name of the required property set to <c>null</c> in JSON and removed in XML.</param>
    [TestMethod]
    [DataRow("schemaVersion")]
    [DataRow("resourceId")]
    [DataRow("policy.id")]
    [DataRow("policy.trigger")]
    [DataRow("trigger.type")]
    [DataRow("scope.ruleRefs.notableDateRef")]
    [DataRow("import.resource")]
    [DataRow("notableDate.displayName")]
    [DataRow("notableDate.category")]
    [DataRow("notableDate.rules")]
    [DataRow("rule.id")]
    [DataRow("fixed.day")]
    [DataRow("nthWeekdayFromRule.ordinal")]
    [DataRow("duration.untilDate")]
    [DataRow("dailyInterval.anchorDate")]
    [DataRow("weekly.days")]
    [DataRow("monthlyDay.dayOfMonth")]
    [DataRow("patch.ruleRef")]
    [DataRow("add.rule")]
    public void LoadJson_WhenSchemaRequiredPropertyIsNull_ShouldReportWhatLoadReports(string field)
    {
        AssertSchemaCodesMatch(SchemaFieldJson(field, "null"), SchemaFieldXml(field, null));
    }

    /// <summary>
    /// Verifies that a JSON list the schema requires to hold an item, a concept's <c>rules</c> or a weekly recurrence's
    /// <c>days</c>, holding none, which the same document in XML fails, is reported with <c>BODU-CAL-SCHEMA</c> and the
    /// other diagnostic codes the XML document reports.
    /// </summary>
    /// <param name="field">The name of the list's items, emptied in JSON and removed in XML.</param>
    [TestMethod]
    [DataRow("notableDate.rules.items")]
    [DataRow("weekly.days")]
    public void LoadJson_WhenSchemaRequiredListIsEmpty_ShouldReportWhatLoadReports(string field)
    {
        AssertSchemaCodesMatch(SchemaFieldJson(field, "[]"), SchemaFieldXml(field, null));
    }

    /// <summary>
    /// Verifies that a JSON integer outside the range the schema allows for it, which the same document in XML fails, is
    /// reported with <c>BODU-CAL-SCHEMA</c> and the other diagnostic codes the XML document reports, rather than loaded
    /// or reported only where the value cannot be used.
    /// </summary>
    /// <param name="field">The name of the integer.</param>
    /// <param name="literal">The integer, as a JSON literal.</param>
    /// <param name="value">The integer, as an XML attribute value.</param>
    [TestMethod]
    [DataRow("scope.fromYear", "-1", "-1")]
    [DataRow("scope.toYear", "65536", "65536")]
    [DataRow("scope.onlyYears.item", "-5", "-5")]
    [DataRow("scope.exceptYears.item", "70000", "70000")]
    [DataRow("trigger.day", "0", "0")]
    [DataRow("trigger.day", "32", "32")]
    [DataRow("action.maxSearchDays", "0", "0")]
    [DataRow("notableDate.defaultDurationDays", "0", "0")]
    [DataRow("rule.durationDays", "0", "0")]
    [DataRow("rule.durationDays", "-3", "-3")]
    [DataRow("applicability.fromYear", "-1", "-1")]
    [DataRow("applicability.toYear", "65536", "65536")]
    [DataRow("applicability.everyYears", "0", "0")]
    [DataRow("applicability.anchorYear", "65536", "65536")]
    [DataRow("applicability.onlyYears.item", "-5", "-5")]
    [DataRow("applicability.exceptYears.item", "65536", "65536")]
    [DataRow("fixed.day", "0", "0")]
    [DataRow("fixed.day", "32", "32")]
    [DataRow("weekdayNearDate.day", "0", "0")]
    [DataRow("weekdayNearDate.day", "40", "40")]
    [DataRow("nthWeekdayFromRule.ordinal", "0", "0")]
    [DataRow("nthWeekdayFromRule.ordinal", "367", "367")]
    [DataRow("nthWeekdayFromRule.ordinal", "\"+5\"", "+5")]
    [DataRow("nthWeekdayFromRule.ordinal", "\"05\"", "05")]
    [DataRow("workingDayInMonth.ordinal", "0", "0")]
    [DataRow("ordinalDayOfMonth.ordinal", "-367", "-367")]
    [DataRow("dayOfYear.ordinal", "367", "367")]
    [DataRow("isoWeekDate.week", "0", "0")]
    [DataRow("isoWeekDate.week", "54", "54")]
    [DataRow("untilDate.fixed.day", "0", "0")]
    [DataRow("dailyInterval.intervalDays", "0", "0")]
    [DataRow("weekly.intervalWeeks", "0", "0")]
    [DataRow("monthlyDay.dayOfMonth", "0", "0")]
    [DataRow("monthlyDay.dayOfMonth", "32", "32")]
    [DataRow("monthlyDay.intervalMonths", "0", "0")]
    [DataRow("monthlyWeekday.intervalMonths", "-1", "-1")]
    [DataRow("patch.durationDays", "0", "0")]
    [DataRow("add.fixed.day", "32", "32")]
    public void LoadJson_WhenIntegerIsOutsideSchemaRange_ShouldReportWhatLoadReports(string field, string literal, string value)
    {
        AssertSchemaCodesMatch(SchemaFieldJson(field, literal), SchemaFieldXml(field, value));
    }

    /// <summary>
    /// Verifies that a JSON integer at a bound of the range the schema allows for it loads, as the same document in XML
    /// loads, into the same resource.
    /// </summary>
    /// <param name="field">The name of the integer.</param>
    /// <param name="value">The integer, as a JSON literal and an XML attribute value.</param>
    [TestMethod]
    [DataRow("scope.fromYear", "0")]
    [DataRow("scope.toYear", "65535")]
    [DataRow("scope.onlyYears.item", "0")]
    [DataRow("scope.exceptYears.item", "65535")]
    [DataRow("trigger.day", "1")]
    [DataRow("trigger.day", "31")]
    [DataRow("action.maxSearchDays", "1")]
    [DataRow("notableDate.defaultDurationDays", "1")]
    [DataRow("rule.durationDays", "1")]
    [DataRow("applicability.fromYear", "0")]
    [DataRow("applicability.toYear", "65535")]
    [DataRow("applicability.everyYears", "1")]
    [DataRow("applicability.anchorYear", "65535")]
    [DataRow("applicability.onlyYears.item", "65535")]
    [DataRow("fixed.day", "31")]
    [DataRow("weekdayNearDate.day", "31")]
    [DataRow("nthWeekdayFromRule.ordinal", "-366")]
    [DataRow("nthWeekdayFromRule.ordinal", "366")]
    [DataRow("dayOfYear.ordinal", "-1")]
    [DataRow("isoWeekDate.week", "1")]
    [DataRow("isoWeekDate.week", "53")]
    [DataRow("dailyInterval.intervalDays", "1")]
    [DataRow("monthlyDay.dayOfMonth", "31")]
    public void LoadJson_WhenIntegerIsAtSchemaRangeBound_ShouldLoadWhatLoadLoads(string field, string value)
    {
        NotableDateResource fromJson = NotableDateResourceLoader.LoadJson(SchemaFieldJson(field, value), EnumImportResolver);
        NotableDateResource fromXml = NotableDateResourceLoader.Load(SchemaFieldXml(field, value), EnumImportResolver);

        CollectionAssert.AreEqual(SchemaPackBytes(fromXml), SchemaPackBytes(fromJson), field + " " + value);
    }

    /// <summary>
    /// Asserts that a JSON document fails with <c>BODU-CAL-SCHEMA</c> among the same distinct error codes its XML twin
    /// fails with.
    /// </summary>
    /// <param name="json">The JSON document.</param>
    /// <param name="xml">The XML twin.</param>
    private static void AssertSchemaCodesMatch(string json, string xml)
    {
        bool xmlLoaded = NotableDateResourceLoader.TryLoad(xml, EnumImportResolver, out _, out IReadOnlyList<NotableDateValidationDiagnostic> xmlDiagnostics);
        bool jsonLoaded = NotableDateResourceLoader.TryLoadJson(json, EnumImportResolver, out _, out IReadOnlyList<NotableDateValidationDiagnostic> jsonDiagnostics);

        string[] expected = [.. xmlDiagnostics.Where(d => d.Severity == NotableDateValidationSeverity.Error).Select(d => d.Code).Distinct().Order(StringComparer.Ordinal)];
        string[] actual = [.. jsonDiagnostics.Where(d => d.Severity == NotableDateValidationSeverity.Error).Select(d => d.Code).Distinct().Order(StringComparer.Ordinal)];

        Assert.IsFalse(xmlLoaded, "The XML twin loaded.");
        Assert.IsFalse(jsonLoaded, "The JSON document loaded.");
        CollectionAssert.Contains(actual, "BODU-CAL-SCHEMA");
        CollectionAssert.AreEqual(expected, actual, string.Join(", ", actual) + " against " + string.Join(", ", expected));
    }

    /// <summary>
    /// Returns <see cref="SchemaTemplateJson" /> with one field removed or replaced.
    /// </summary>
    /// <param name="field">The name of the field, or <see langword="null" /> to change none.</param>
    /// <param name="literal">The JSON literal the field holds, or <see langword="null" /> to remove it.</param>
    /// <returns>The JSON document.</returns>
    private static string SchemaFieldJson(string? field, string? literal)
    {
        JsonNode root = JsonNode.Parse(SchemaTemplateJson)!;
        if (field is null)
            return root.ToJsonString();

        string[] segments = s_schemaFields[field].Json.Split('/', StringSplitOptions.RemoveEmptyEntries);
        JsonNode parent = root;
        foreach (string segment in segments[..^1])
            parent = parent is JsonArray array ? array[int.Parse(segment, CultureInfo.InvariantCulture)]! : parent[segment]!;

        string last = segments[^1];
        if (parent is JsonArray items)
            items[int.Parse(last, CultureInfo.InvariantCulture)] = JsonNode.Parse(literal!);
        else if (literal is null)
            _ = parent.AsObject().Remove(last);
        else
            parent[last] = JsonNode.Parse(literal);

        return root.ToJsonString();
    }

    /// <summary>
    /// Returns <see cref="SchemaTemplateXml" /> with one field removed or replaced.
    /// </summary>
    /// <param name="field">The name of the field, or <see langword="null" /> to change none.</param>
    /// <param name="value">
    /// The attribute value the field holds, or <see langword="null" /> to remove the attribute or the elements.
    /// </param>
    /// <returns>The XML document.</returns>
    private static string SchemaFieldXml(string? field, string? value)
    {
        XDocument document = XDocument.Parse(SchemaTemplateXml);
        if (field is not null)
        {
            XmlNamespaceManager namespaces = new(new NameTable());
            namespaces.AddNamespace("nd", "urn:bodu:globalization:calendar");

            foreach (XObject target in ((IEnumerable<object>)document.XPathEvaluate(s_schemaFields[field].Xml, namespaces)).Cast<XObject>().ToList())
            {
                switch (target)
                {
                    case XAttribute attribute when value is null:
                        attribute.Remove();
                        break;

                    case XAttribute attribute:
                        attribute.Value = value;
                        break;

                    case XElement element:
                        element.Remove();
                        break;
                }
            }
        }

        return document.ToString();
    }

    /// <summary>
    /// Writes a resource as a binary rule pack, the byte-for-byte form two resources are compared in.
    /// </summary>
    /// <param name="resource">The resource to write.</param>
    /// <returns>The pack bytes.</returns>
    private static byte[] SchemaPackBytes(NotableDateResource resource)
    {
        using MemoryStream stream = new();
        NotableDateBinaryResource.Write(resource, stream);
        return stream.ToArray();
    }
}
