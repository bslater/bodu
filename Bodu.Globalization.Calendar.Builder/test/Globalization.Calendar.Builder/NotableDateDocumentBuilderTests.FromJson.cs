// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.FromJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json.Nodes;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// The recognized value of every enumeration-valued property the JSON enumeration test document holds, keyed by the
    /// placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonEnumFieldValues = new(StringComparer.Ordinal)
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
        ["action.type"] = "MoveToNextWeekday",
        ["action.dayOfWeek"] = "Monday",
        ["emission.mode"] = "ObservedOnly",
        ["notableDate.category"] = "PublicHoliday",
        ["rule.category"] = "Observance",
        ["applicability.calendar"] = "Gregorian",
        ["untilDate.startBoundary"] = "Inclusive",
        ["untilDate.endBoundary"] = "Exclusive",
        ["untilDate.selection"] = "FirstAfterStart",
        ["patch.category"] = "Civic",
    };

    /// <summary>
    /// Verifies that the JSON enumeration test document reads and builds when every enumeration-valued property holds a
    /// recognized value, so each rejection the per-property test observes is caused by the one value it replaces.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenEveryEnumValueRecognized_ShouldReadAndBuild()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromJson(EnumFieldJson(null, null)).Build();

        Assert.AreEqual("test.enums", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an enumeration-valued JSON property holding a value that names no member is rejected with a
    /// <see cref="FormatException" /> naming the value, as the loader rejects it, rather than read as the enumeration's
    /// default or dropped.
    /// </summary>
    /// <param name="field">The placeholder of the property holding the unrecognized value.</param>
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
    [DataRow("action.type")]
    [DataRow("action.dayOfWeek")]
    [DataRow("emission.mode")]
    [DataRow("notableDate.category")]
    [DataRow("rule.category")]
    [DataRow("applicability.calendar")]
    [DataRow("untilDate.startBoundary")]
    [DataRow("untilDate.endBoundary")]
    [DataRow("untilDate.selection")]
    [DataRow("patch.category")]
    public void FromJson_WhenEnumValueUnrecognized_ShouldThrowFormatException(string field)
    {
        string json = EnumFieldJson(field, "Bogus");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains("Bogus", ex.Message);
    }

    /// <summary>
    /// Verifies that an enumeration-valued JSON property holding a number as text, whether or not it is a member's
    /// value, is rejected with a <see cref="FormatException" />, as the loader accepts only member names.
    /// </summary>
    /// <param name="value">The numeric text.</param>
    [TestMethod]
    [DataRow("1")]
    [DataRow("99")]
    public void FromJson_WhenEnumValueIsNumericText_ShouldThrowFormatException(string value)
    {
        string json = EnumFieldJson("notableDate.category", value);

        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });
    }

    /// <summary>
    /// Verifies that an enumeration-valued JSON property naming a member in a different case reads as that member, as
    /// the loader reads member names ignoring case.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenEnumValueDiffersInCase_ShouldReadTheNamedMember()
    {
        string json = EnumFieldJson("sameDayCollisionPolicy", "highestpriorityonly");

        NotableDateResource resource = NotableDateDocumentBuilder.FromJson(json).Build();

        Assert.AreEqual(CollisionPolicy.HighestPriorityOnly, resource.ResolutionPolicy.SameDayCollisionPolicy);
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is a letter mask, which <see cref="DayOfWeekSet" />
    /// reads but the document format does not define, is rejected with a <see cref="FormatException" />, as the loader
    /// rejects it, rather than read as that working week.
    /// </summary>
    /// <param name="workingDays">The letter mask.</param>
    [TestMethod]
    [DataRow("_MTWTF_")]
    [DataRow("MTWTF__")]
    [DataRow("-MTWTF-")]
    public void FromJson_WhenWorkingDaysIsLetterMask_ShouldThrowFormatException(string workingDays)
    {
        string json = WorkingDaysJson(workingDays);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains(workingDays, ex.Message);
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is not a seven-character working-week pattern is
    /// rejected with a <see cref="FormatException" />, as the loader rejects it, rather than dropped in favour of the
    /// default working week.
    /// </summary>
    /// <param name="workingDays">The malformed working-week pattern.</param>
    [TestMethod]
    [DataRow("MTWTF")]
    [DataRow("01111100")]
    [DataRow("0111112")]
    public void FromJson_WhenWorkingDaysMalformed_ShouldThrowFormatException(string workingDays)
    {
        string json = WorkingDaysJson(workingDays);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains(workingDays, ex.Message);
    }

    /// <summary>
    /// Verifies that a resolution policy whose <c>workingDays</c> is the documented Sunday-first binary pattern reads as
    /// that working week.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenWorkingDaysIsBinary_ShouldReadTheWorkingWeek()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromJson(WorkingDaysJson("1111100")).Build();

        Assert.AreEqual(DayOfWeekSet.SundayToThursday, resource.ResolutionPolicy.WorkingWeek);
    }

    /// <summary>
    /// Builds a JSON document holding every enumeration-valued property the builder reads, each set to its recognized
    /// value except the one named, which holds the supplied value.
    /// </summary>
    /// <param name="field">The placeholder of the property to set, or <see langword="null" /> to set none.</param>
    /// <param name="value">The value the named property holds.</param>
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
                "trigger": { "type": "@trigger.type@", "weekdays": [ "@trigger.weekdays@" ] },
                "action": { "type": "@action.type@", "dayOfWeek": "@action.dayOfWeek@" },
                "emission": { "mode": "@emission.mode@" } } ],
              "notableDates": [
                { "id": "anchor", "displayName": "Anchor", "category": "@notableDate.category@",
                  "rules": [ { "id": "r", "category": "@rule.category@", "applicability": { "calendar": "@applicability.calendar@" },
                    "strategy": { "fixed": { "month": 3, "day": 1 } },
                    "duration": { "untilDate": { "startBoundary": "@untilDate.startBoundary@", "endBoundary": "@untilDate.endBoundary@",
                      "selection": "@untilDate.selection@", "strategy": { "fixed": { "month": 3, "day": 5 } } } },
                    "adjustments": [ "policy" ] } ] } ],
              "overrides": [ { "operation": "PatchRule", "notableDateRef": "anchor", "ruleRef": "r", "category": "@patch.category@" } ] }
            """;

        foreach ((string key, string recognized) in s_jsonEnumFieldValues)
            json = json.Replace("@" + key + "@", key == field ? value : recognized, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Builds a JSON document with one fixed concept whose resolution policy declares the supplied working week.
    /// </summary>
    /// <param name="workingDays">The <c>workingDays</c> value.</param>
    /// <returns>The JSON document.</returns>
    private static string WorkingDaysJson(string workingDays) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.working-days",
          "resolutionPolicy": { "workingDays": "@workingDays@" },
          "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ] }
        """.Replace("@workingDays@", workingDays, StringComparison.Ordinal);

    /// <summary>
    /// The well-formed value, as a JSON literal, of every integer- and Boolean-valued property the JSON scalar test
    /// document holds, keyed by the placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonScalarFieldValues = new(StringComparer.Ordinal)
    {
        ["policy.priority"] = "5",
        ["action.days"] = "1",
        ["action.maxSearchDays"] = "7",
        ["action.skipWeekends"] = "true",
        ["action.skipNonWorkingDates"] = "false",
        ["emission.nonWorking"] = "true",
        ["notableDate.defaultDurationDays"] = "1",
        ["notableDate.defaultNonWorkingDay"] = "true",
        ["rule.priority"] = "3",
        ["rule.nonWorking"] = "true",
        ["rule.durationDays"] = "2",
        ["applicability.fromYear"] = "2000",
        ["applicability.toYear"] = "2100",
        ["applicability.everyYears"] = "1",
        ["applicability.anchorYear"] = "2000",
        ["applicability.onlyYears"] = "2026",
        ["applicability.exceptYears"] = "2027",
        ["patch.priority"] = "4",
        ["patch.nonWorking"] = "false",
        ["patch.durationDays"] = "1",
    };

    /// <summary>
    /// Verifies that the JSON scalar test document reads and builds when every integer- and Boolean-valued property is
    /// well-formed, so each rejection the per-property tests observe is caused by the one value they replace.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenEveryScalarValueWellFormed_ShouldReadAndBuild()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromJson(ScalarFieldJson(null, null)).Build();

        Assert.AreEqual("test.scalars", resource.ResourceId);
    }

    /// <summary>
    /// Verifies that an integer-valued JSON property holding text that is not an integer is rejected with a
    /// <see cref="FormatException" /> naming the value, as the loader rejects it.
    /// </summary>
    /// <param name="field">The placeholder of the property holding the malformed value.</param>
    [TestMethod]
    [DataRow("policy.priority")]
    [DataRow("action.days")]
    [DataRow("action.maxSearchDays")]
    [DataRow("notableDate.defaultDurationDays")]
    [DataRow("rule.priority")]
    [DataRow("rule.durationDays")]
    [DataRow("applicability.fromYear")]
    [DataRow("applicability.toYear")]
    [DataRow("applicability.everyYears")]
    [DataRow("applicability.anchorYear")]
    [DataRow("applicability.onlyYears")]
    [DataRow("applicability.exceptYears")]
    [DataRow("patch.priority")]
    [DataRow("patch.durationDays")]
    public void FromJson_WhenIntegerValueMalformed_ShouldThrowFormatException(string field)
    {
        string json = ScalarFieldJson(field, "\"high\"");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains("high", ex.Message);
    }

    /// <summary>
    /// Verifies that an integer-valued JSON property holding a number that is not a 32-bit integer, a fraction or a
    /// value beyond the range, is rejected with a <see cref="FormatException" /> naming the number, as the loader
    /// rejects it.
    /// </summary>
    /// <param name="value">The JSON number.</param>
    [TestMethod]
    [DataRow("1.5")]
    [DataRow("3000000000")]
    public void FromJson_WhenIntegerValueIsNotInt32_ShouldThrowFormatException(string value)
    {
        string json = ScalarFieldJson("rule.priority", value);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains(value, ex.Message);
    }

    /// <summary>
    /// Verifies that a Boolean-valued JSON property holding text that is not a Boolean is rejected with a
    /// <see cref="FormatException" /> naming the value, as the loader rejects it.
    /// </summary>
    /// <param name="field">The placeholder of the property holding the malformed value.</param>
    [TestMethod]
    [DataRow("action.skipWeekends")]
    [DataRow("action.skipNonWorkingDates")]
    [DataRow("emission.nonWorking")]
    [DataRow("notableDate.defaultNonWorkingDay")]
    [DataRow("rule.nonWorking")]
    [DataRow("patch.nonWorking")]
    public void FromJson_WhenBooleanValueMalformed_ShouldThrowFormatException(string field)
    {
        string json = ScalarFieldJson(field, "\"yes\"");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains("yes", ex.Message);
    }

    /// <summary>
    /// Verifies that an integer-valued JSON property holding a number in quotes reads as that number, as the loader
    /// reads it.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenIntegerValueIsNumericText_ShouldReadTheNumber()
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromJson(ScalarFieldJson("rule.priority", "\"7\"")).Build();

        Assert.AreEqual(7, resource.NotableDates.Single(d => d.Id == "anchor").Rules[0].Priority);
    }

    /// <summary>
    /// Verifies that a Boolean-valued JSON property holding a schema Boolean as text, or as the number 0 or 1, reads as
    /// that Boolean, as the loader reads it.
    /// </summary>
    /// <param name="value">The Boolean, as a JSON literal.</param>
    /// <param name="expected">The Boolean the value stands for.</param>
    [TestMethod]
    [DataRow("\"false\"", false)]
    [DataRow("0", false)]
    [DataRow("1", true)]
    public void FromJson_WhenBooleanValueIsTextOrNumber_ShouldReadTheBoolean(string value, bool expected)
    {
        NotableDateResource resource = NotableDateDocumentBuilder.FromJson(ScalarFieldJson("rule.nonWorking", value)).Build();

        Assert.AreEqual(expected, resource.NotableDates.Single(d => d.Id == "anchor").Rules[0].NonWorking);
    }

    /// <summary>
    /// Builds a JSON document holding every integer- and Boolean-valued property the builder reads, each set to its
    /// well-formed value except the one named, which holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The placeholder of the property to set, or <see langword="null" /> to set none.</param>
    /// <param name="literal">The JSON literal the named property holds.</param>
    /// <returns>The JSON document.</returns>
    private static string ScalarFieldJson(string? field, string? literal)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.scalars",
              "adjustmentPolicies": [ { "id": "policy", "priority": @policy.priority@,
                "trigger": { "type": "Always" },
                "action": { "type": "AddDays", "days": @action.days@, "maxSearchDays": @action.maxSearchDays@,
                  "skipWeekends": @action.skipWeekends@, "skipNonWorkingDates": @action.skipNonWorkingDates@ },
                "emission": { "mode": "ObservedOnly", "nonWorking": @emission.nonWorking@ } } ],
              "notableDates": [
                { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday",
                  "defaultNonWorkingDay": @notableDate.defaultNonWorkingDay@, "defaultDurationDays": @notableDate.defaultDurationDays@,
                  "rules": [ { "id": "r", "priority": @rule.priority@, "nonWorking": @rule.nonWorking@, "durationDays": @rule.durationDays@,
                    "applicability": { "fromYear": @applicability.fromYear@, "toYear": @applicability.toYear@,
                      "everyYears": @applicability.everyYears@, "anchorYear": @applicability.anchorYear@,
                      "onlyYears": [ @applicability.onlyYears@ ], "exceptYears": [ @applicability.exceptYears@ ] },
                    "strategy": { "fixed": { "month": 3, "day": 1 } },
                    "adjustments": [ "policy" ] } ] },
                { "id": "other", "displayName": "Other", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ],
              "overrides": [ { "operation": "PatchRule", "notableDateRef": "other", "ruleRef": "r",
                "priority": @patch.priority@, "nonWorking": @patch.nonWorking@, "durationDays": @patch.durationDays@ } ] }
            """;

        foreach ((string key, string wellFormed) in s_jsonScalarFieldValues)
            json = json.Replace("@" + key + "@", key == field ? literal : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>The adjustment policy the field test documents declare unless a scenario replaces it.</summary>
    private const string DefaultFieldPolicyJson =
        """{ "id": "policy", "trigger": { "type": "IfWeekend" }, "action": { "type": "MoveToNextWorkingDay" }, "emission": { "mode": "ObservedOnly" } }""";

    /// <summary>
    /// The field test scenarios, each naming the adjustment policy, the root <c>imports</c> property, and the extra
    /// <c>PatchRule</c> properties its document declares.
    /// </summary>
    private static readonly Dictionary<string, (string Policy, string Imports, string Patch)> s_jsonFieldScenarios = new(StringComparer.Ordinal)
    {
        ["import with a use"] = (DefaultFieldPolicyJson, """ "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "as": "library-day", "territory": "AU", "category": "Cultural", "nonWorking": true, "adjustments": [ "policy" ] } ] } ], """, string.Empty),
        ["import of a whole resource"] = (DefaultFieldPolicyJson, """ "imports": [ { "resource": "lib" } ], """, string.Empty),
        ["trigger month and day"] = ("""{ "id": "policy", "trigger": { "type": "IfBeforeFixedDate", "month": "June", "day": 15 }, "action": { "type": "AddDays", "days": 1 }, "emission": { "mode": "ObservedOnly" } }""", string.Empty, string.Empty),
        ["trigger numeric month"] = ("""{ "id": "policy", "trigger": { "type": "IfAfterFixedDate", "month": 6, "day": 15 }, "action": { "type": "AddDays", "days": 1 }, "emission": { "mode": "ObservedOnly" } }""", string.Empty, string.Empty),
        ["trigger weekOrdinal"] = ("""{ "id": "policy", "trigger": { "type": "IfNthOccurrenceInMonth", "weekOrdinal": "Second", "weekdays": [ "Monday" ] }, "action": { "type": "AddDays", "days": 1 }, "emission": { "mode": "ObservedOnly" } }""", string.Empty, string.Empty),
        ["trigger handlerKey"] = ("""{ "id": "policy", "trigger": { "type": "Custom", "handlerKey": "my-trigger" }, "action": { "type": "AddDays", "days": 1 }, "emission": { "mode": "ObservedOnly" } }""", string.Empty, string.Empty),
        ["action replacement rule"] = ("""{ "id": "policy", "trigger": { "type": "IfWeekend" }, "action": { "type": "ReplaceWithRule", "notableDateRef": "other", "ruleRef": "r" }, "emission": { "mode": "ObservedOnly" } }""", string.Empty, string.Empty),
        ["action handlerKey and parameters"] = ("""{ "id": "policy", "trigger": { "type": "IfWeekend" }, "action": { "type": "Custom", "handlerKey": "my-action" }, "emission": { "mode": "ObservedOnly" }, "parameters": { "offset": "2", "mode": "fast" } }""", string.Empty, string.Empty),
        ["scope rule references and years"] = ("""{ "id": "policy", "scope": { "ruleRefs": [ { "notableDateRef": "anchor", "ruleRef": "r" } ], "fromYear": 2000, "toYear": 2100, "onlyYears": [ 2026 ], "exceptYears": [ 2027 ] }, "trigger": { "type": "IfWeekend" }, "action": { "type": "MoveToNextWorkingDay" }, "emission": { "mode": "ObservedOnly" } }""", string.Empty, string.Empty),
        ["patch strategy"] = (DefaultFieldPolicyJson, string.Empty, """, "strategy": { "fixed": { "month": 5, "day": 2 } }"""),
        ["patch recurrence"] = (DefaultFieldPolicyJson, string.Empty, """, "recurrence": { "monthlyDay": { "dayOfMonth": 15 } }"""),
        ["patch duration"] = (DefaultFieldPolicyJson, string.Empty, """, "duration": { "untilDate": { "strategy": { "fixed": { "month": 4, "day": 3 } } } }"""),
        ["patch applicability"] = (DefaultFieldPolicyJson, string.Empty, """, "applicability": { "calendar": "Gregorian", "territories": [ "NZ" ], "fromYear": 2000, "onlyYears": [ 2026 ] }"""),
        ["patch empty applicability"] = (DefaultFieldPolicyJson, string.Empty, """, "applicability": { }"""),
        ["patch tags"] = (DefaultFieldPolicyJson, string.Empty, """, "tags": [ "civic" ]"""),
        ["patch adjustments"] = (DefaultFieldPolicyJson, string.Empty, """, "adjustments": [ "policy" ]"""),
        ["every field"] = (
            """{ "id": "policy", "scope": { "ruleRefs": [ { "notableDateRef": "anchor", "ruleRef": "r" } ], "fromYear": 2000, "toYear": 2100, "onlyYears": [ 2026 ], "exceptYears": [ 2027 ] }, "trigger": { "type": "IfBeforeFixedDate", "month": "June", "day": 15 }, "action": { "type": "ReplaceWithRule", "notableDateRef": "other", "ruleRef": "r" }, "emission": { "mode": "ObservedOnly" }, "parameters": { "offset": "2" } }""",
            """ "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "as": "library-day", "territory": "AU", "category": "Cultural", "nonWorking": true, "adjustments": [ "policy" ] } ] } ], """,
            """, "strategy": { "fixed": { "month": 5, "day": 2 } }, "duration": { "untilDate": { "strategy": { "fixed": { "month": 5, "day": 4 } } } }, "applicability": { "territories": [ "NZ" ] }, "tags": [ "civic" ], "adjustments": [ "policy" ]"""),
        ["patch empty tags"] = (DefaultFieldPolicyJson, string.Empty, """, "tags": [ ]"""),
        ["patch empty adjustments"] = (DefaultFieldPolicyJson, string.Empty, """, "adjustments": [ ]"""),
        ["import use empty adjustments"] = (DefaultFieldPolicyJson, """ "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "adjustments": [ ] } ] } ], """, string.Empty),
        ["unqualified scope rule reference"] = ("""{ "id": "policy", "scope": { "ruleRefs": [ "r" ] }, "trigger": { "type": "IfWeekend" }, "action": { "type": "MoveToNextWorkingDay" }, "emission": { "mode": "ObservedOnly" } }""", string.Empty, string.Empty),
        ["handler parameter that is an object"] = ("""{ "id": "policy", "trigger": { "type": "IfWeekend" }, "action": { "type": "Custom", "handlerKey": "my-action" }, "emission": { "mode": "ObservedOnly" }, "parameters": { "options": { "fast": true } } }""", string.Empty, string.Empty),
    };

    /// <summary>
    /// Verifies that a JSON document declaring a field <see cref="NotableDateResourceLoader" /> reads builds, through
    /// the builder, into the same resource the loader loads, compared byte for byte as binary rule packs, rather than
    /// into one the builder dropped the field from.
    /// </summary>
    /// <param name="scenario">The name of the field scenario.</param>
    [TestMethod]
    [DataRow("import with a use")]
    [DataRow("import of a whole resource")]
    [DataRow("trigger month and day")]
    [DataRow("trigger numeric month")]
    [DataRow("trigger weekOrdinal")]
    [DataRow("trigger handlerKey")]
    [DataRow("action replacement rule")]
    [DataRow("action handlerKey and parameters")]
    [DataRow("scope rule references and years")]
    [DataRow("patch strategy")]
    [DataRow("patch recurrence")]
    [DataRow("patch duration")]
    [DataRow("patch applicability")]
    [DataRow("patch empty applicability")]
    [DataRow("patch tags")]
    [DataRow("patch adjustments")]
    [DataRow("every field")]
    public void FromJson_WhenDocumentDeclaresField_ShouldBuildWhatLoadJsonLoads(string scenario)
    {
        string json = FieldScenarioJson(scenario);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, BuilderEnumImportResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(BuilderEnumImportResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), scenario);
    }

    /// <summary>
    /// Verifies that the valid JSON documents the builder tests use build, through the builder, into the same resource
    /// the loader loads, compared byte for byte as binary rule packs.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenTestDocumentIsValid_ShouldBuildWhatLoadJsonLoads()
    {
        foreach (string json in new[] { EnumFieldJson(null, null), ScalarFieldJson(null, null), WorkingDaysJson("1111100") })
        {
            NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, BuilderEnumImportResolver);
            NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(BuilderEnumImportResolver);

            CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), loaded.ResourceId);
        }
    }

    /// <summary>
    /// Verifies that a JSON field the builder's document model cannot represent, an explicitly empty list that clears
    /// what a patch or an import would otherwise keep, a scope rule reference that is a bare rule identifier, or a
    /// handler parameter whose value is not a scalar, is rejected with a <see cref="NotSupportedException" /> rather
    /// than dropped or read differently.
    /// </summary>
    /// <param name="scenario">The name of the field scenario.</param>
    [TestMethod]
    [DataRow("patch empty tags")]
    [DataRow("patch empty adjustments")]
    [DataRow("import use empty adjustments")]
    [DataRow("unqualified scope rule reference")]
    [DataRow("handler parameter that is an object")]
    public void FromJson_WhenFieldCannotBeRepresented_ShouldThrowNotSupportedException(string scenario)
    {
        string json = FieldScenarioJson(scenario);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });
    }

    /// <summary>
    /// Writes a resource as a binary rule pack, the byte-for-byte form two resources are compared in.
    /// </summary>
    /// <param name="resource">The resource to write.</param>
    /// <returns>The pack bytes.</returns>
    private static byte[] PackBytes(NotableDateResource resource)
    {
        using MemoryStream stream = new();
        NotableDateBinaryResource.Write(resource, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Builds the JSON document of a field scenario: two concepts, a patch of the second concept's rule, and the
    /// scenario's adjustment policy, imports, and extra patch properties.
    /// </summary>
    /// <param name="scenario">The name of the field scenario.</param>
    /// <returns>The JSON document.</returns>
    private static string FieldScenarioJson(string scenario)
    {
        (string policy, string imports, string patch) = s_jsonFieldScenarios[scenario];

        return """
            { "schemaVersion": "1.0", "resourceId": "test.fields",
              "adjustmentPolicies": [ @policy@ ],
              @imports@
              "notableDates": [
                { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } }, "adjustments": [ "policy" ] } ] },
                { "id": "other", "displayName": "Other", "category": "Observance",
                  "rules": [ { "id": "r", "applicability": { "territories": [ "AU" ] }, "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ],
              "overrides": [ { "operation": "PatchRule", "notableDateRef": "other", "ruleRef": "r", "priority": 2 @patch@ } ] }
            """
            .Replace("@policy@", policy, StringComparison.Ordinal)
            .Replace("@imports@", imports, StringComparison.Ordinal)
            .Replace("@patch@", patch, StringComparison.Ordinal);
    }

    /// <summary>
    /// The well-formed value, as a JSON literal, of every text field the JSON text test document holds, keyed by the
    /// placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonTextFieldValues = new(StringComparer.Ordinal)
    {
        ["schemaVersion"] = "\"1.0\"",
        ["resourceId"] = "\"test.text\"",
        ["metadata.name"] = "\"Name\"",
        ["metadata.description"] = "\"Description\"",
        ["metadata.sources"] = "\"source\"",
        ["policy.id"] = "\"policy\"",
        ["policy.description"] = "\"Policy\"",
        ["trigger.handlerKey"] = "\"my-trigger\"",
        ["action.handlerKey"] = "\"my-action\"",
        ["emission.reason"] = "\"Reason\"",
        ["import.resource"] = "\"lib\"",
        ["use.as"] = "\"library-day\"",
        ["notableDate.id"] = "\"anchor\"",
        ["notableDate.displayName"] = "\"Anchor\"",
        ["rule.id"] = "\"r\"",
        ["rule.comment"] = "\"Comment\"",
    };

    /// <summary>
    /// The well-formed item, as a JSON literal, of every list the JSON list test document holds, keyed by the
    /// placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonListFieldItems = new(StringComparer.Ordinal)
    {
        ["categoryPrecedence"] = "\"PublicHoliday\"",
        ["scope.territories"] = "\"AU\"",
        ["scope.calendars"] = "\"Gregorian\"",
        ["scope.categories"] = "\"PublicHoliday\"",
        ["scope.notableDateRefs"] = "\"anchor\"",
        ["trigger.weekdays"] = "\"Saturday\"",
        ["use.adjustments"] = "\"policy\"",
        ["notableDate.tags"] = "\"civic\"",
        ["applicability.territories"] = "\"AU\"",
        ["rule.tags"] = "\"civic\"",
        ["rule.adjustments"] = "\"policy\"",
        ["patch.tags"] = "\"civic\"",
        ["patch.adjustments"] = "\"policy\"",
    };

    /// <summary>
    /// Verifies that a JSON text field holding a number or a Boolean, which the loader reads as its text, builds through
    /// the builder into the same resource the loader loads, compared byte for byte as binary rule packs, rather than
    /// making the builder throw.
    /// </summary>
    /// <param name="field">The placeholder of the text field.</param>
    /// <param name="literal">The number or Boolean, as a JSON literal.</param>
    [TestMethod]
    [DataRow("schemaVersion", "5")]
    [DataRow("schemaVersion", "true")]
    [DataRow("resourceId", "true")]
    [DataRow("metadata.name", "5")]
    [DataRow("metadata.name", "true")]
    [DataRow("metadata.description", "5")]
    [DataRow("metadata.sources", "5")]
    [DataRow("metadata.sources", "true")]
    [DataRow("policy.id", "true")]
    [DataRow("policy.description", "5")]
    [DataRow("trigger.handlerKey", "5")]
    [DataRow("trigger.handlerKey", "true")]
    [DataRow("action.handlerKey", "5")]
    [DataRow("action.handlerKey", "false")]
    [DataRow("emission.reason", "5")]
    [DataRow("emission.reason", "true")]
    [DataRow("import.resource", "5")]
    [DataRow("import.resource", "true")]
    [DataRow("use.as", "true")]
    [DataRow("notableDate.id", "true")]
    [DataRow("notableDate.displayName", "5")]
    [DataRow("notableDate.displayName", "true")]
    [DataRow("rule.id", "true")]
    [DataRow("rule.comment", "5")]
    [DataRow("rule.comment", "true")]
    public void FromJson_WhenTextFieldIsNumberOrBoolean_ShouldBuildWhatLoadJsonLoads(string field, string literal)
    {
        string json = TextFieldJson(field, literal);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, AnyLibraryResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(AnyLibraryResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), field + " " + literal);
    }

    /// <summary>
    /// Verifies that an item of a JSON list of territory codes, references or tags that is a number or a Boolean, which
    /// the loader rejects with <c>BODU-CAL-VALUE</c>, is rejected with a <see cref="FormatException" /> carrying the
    /// loader's message, rather than an <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="field">The placeholder of the list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    [TestMethod]
    [DataRow("notableDate.tags", "5")]
    [DataRow("rule.tags", "true")]
    [DataRow("rule.adjustments", "5")]
    [DataRow("applicability.territories", "5")]
    [DataRow("scope.territories", "true")]
    [DataRow("scope.notableDateRefs", "5")]
    [DataRow("patch.tags", "5")]
    [DataRow("patch.adjustments", "true")]
    [DataRow("use.adjustments", "5")]
    public void FromJson_WhenTextListItemIsNumberOrBoolean_ShouldThrowFormatException(string field, string item)
    {
        string json = ListFieldJson(field, item);
        string expected = LoaderErrorMessage(json, AnyLibraryResolver);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an item of a JSON list of enumeration names that is a number or a Boolean, which the loader
    /// rejects with <c>BODU-CAL-ENUM</c>, is rejected with a <see cref="FormatException" /> naming the item, rather
    /// than an <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="field">The placeholder of the list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    [TestMethod]
    [DataRow("categoryPrecedence", "1")]
    [DataRow("trigger.weekdays", "6")]
    [DataRow("scope.calendars", "0")]
    [DataRow("scope.categories", "true")]
    public void FromJson_WhenNameListItemIsNumberOrBoolean_ShouldThrowFormatException(string field, string item)
    {
        string json = ListFieldJson(field, item);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains("'" + item + "'", ex.Message);
    }

    /// <summary>
    /// Verifies that an enumeration-valued JSON property holding a number, which the loader reads as its digits and
    /// rejects with <c>BODU-CAL-ENUM</c>, is rejected with a <see cref="FormatException" /> naming the number, rather
    /// than an <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="property">The JSON fragment holding the number.</param>
    [TestMethod]
    [DataRow("""{ "schemaVersion": "1.0", "resourceId": "test.enum", "resolutionPolicy": { "duplicatePolicy": 1 }, "notableDates": [] }""")]
    [DataRow("""{ "schemaVersion": "1.0", "resourceId": "test.enum", "notableDates": [ { "id": "x", "displayName": "X", "category": 1, "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ] }""")]
    [DataRow("""{ "schemaVersion": "1.0", "resourceId": "test.enum", "adjustmentPolicies": [ { "id": "p", "trigger": { "type": 1 }, "action": { "type": "None" }, "emission": { "mode": "ActualOnly" } } ], "notableDates": [] }""")]
    public void FromJson_WhenEnumValueIsJsonNumber_ShouldThrowFormatException(string property)
    {
        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(property);
        });

        Assert.Contains("'1'", ex.Message);
    }

    /// <summary>
    /// Verifies that a text field the loader does not read, a metadata name, description or source, an adjustment
    /// policy's description or a rule's comment, holding an object or an array, which the builder's document model holds
    /// only as text, is rejected with a <see cref="NotSupportedException" /> rather than an
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="field">The placeholder of the text field.</param>
    /// <param name="literal">The object or array, as a JSON literal.</param>
    [TestMethod]
    [DataRow("metadata.name", "{}")]
    [DataRow("metadata.description", "[]")]
    [DataRow("metadata.sources", "{}")]
    [DataRow("policy.description", "{ \"en\": \"Policy\" }")]
    [DataRow("rule.comment", "[]")]
    public void FromJson_WhenUnreadTextFieldIsObjectOrArray_ShouldThrowNotSupportedException(string field, string literal)
    {
        string json = TextFieldJson(field, literal);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });
    }

    /// <summary>
    /// Resolves every imported resource name to the one library resource the text test document imports.
    /// </summary>
    /// <param name="name">The imported resource's name.</param>
    /// <returns>The library resource's JSON.</returns>
    private static string? AnyLibraryResolver(string name) =>
        BuilderEnumImportResolver("lib");

    /// <summary>
    /// Builds a JSON document holding every text field the builder reads, each set to its well-formed value except the
    /// one named, which holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The placeholder of the field to set.</param>
    /// <param name="literal">The JSON literal the named field holds.</param>
    /// <returns>The JSON document.</returns>
    private static string TextFieldJson(string field, string literal)
    {
        string json = """
            { "schemaVersion": @schemaVersion@, "resourceId": @resourceId@,
              "metadata": { "name": @metadata.name@, "description": @metadata.description@, "sources": [ @metadata.sources@ ] },
              "adjustmentPolicies": [ { "id": @policy.id@, "description": @policy.description@,
                "trigger": { "type": "Custom", "handlerKey": @trigger.handlerKey@ },
                "action": { "type": "Custom", "handlerKey": @action.handlerKey@ },
                "emission": { "mode": "ObservedOnly", "reason": @emission.reason@ } } ],
              "imports": [ { "resource": @import.resource@, "uses": [ { "notableDateRef": "lib-day", "as": @use.as@ } ] } ],
              "notableDates": [
                { "id": @notableDate.id@, "displayName": @notableDate.displayName@, "category": "PublicHoliday",
                  "rules": [ { "id": @rule.id@, "comment": @rule.comment@, "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ] }
            """;

        foreach ((string key, string wellFormed) in s_jsonTextFieldValues)
            json = json.Replace("@" + key + "@", key == field ? literal : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Builds a JSON document holding every list the builder reads, each holding one well-formed item except the one
    /// named, which holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The placeholder of the list to set.</param>
    /// <param name="item">The JSON literal the named list holds.</param>
    /// <returns>The JSON document.</returns>
    private static string ListFieldJson(string field, string item)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.lists",
              "resolutionPolicy": { "categoryPrecedence": [ @categoryPrecedence@ ] },
              "adjustmentPolicies": [ { "id": "policy",
                "scope": { "territories": [ @scope.territories@ ], "calendars": [ @scope.calendars@ ], "categories": [ @scope.categories@ ],
                  "notableDateRefs": [ @scope.notableDateRefs@ ] },
                "trigger": { "type": "IfDayOfWeek", "weekdays": [ @trigger.weekdays@ ] },
                "action": { "type": "MoveToNextWeekday", "dayOfWeek": "Monday" },
                "emission": { "mode": "ObservedOnly" } } ],
              "imports": [ { "resource": "lib", "uses": [ { "notableDateRef": "lib-day", "adjustments": [ @use.adjustments@ ] } ] } ],
              "notableDates": [
                { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday", "tags": [ @notableDate.tags@ ],
                  "rules": [ { "id": "r", "applicability": { "territories": [ @applicability.territories@ ] },
                    "strategy": { "fixed": { "month": 3, "day": 1 } },
                    "tags": [ @rule.tags@ ], "adjustments": [ @rule.adjustments@ ] } ] },
                { "id": "other", "displayName": "O", "category": "Observance",
                  "rules": [ { "id": "r", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ],
              "overrides": [ { "operation": "PatchRule", "notableDateRef": "other", "ruleRef": "r",
                "tags": [ @patch.tags@ ], "adjustments": [ @patch.adjustments@ ] } ] }
            """;

        foreach ((string key, string wellFormed) in s_jsonListFieldItems)
            json = json.Replace("@" + key + "@", key == field ? item : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Verifies that a JSON scalar field holding an object or an array, which the loader rejects with
    /// <c>BODU-CAL-VALUE</c> naming the field, is rejected with a <see cref="FormatException" /> naming the field, rather
    /// than read as absent or failing with an <see cref="InvalidOperationException" />.
    /// </summary>
    /// <param name="field">The JSON property holding the value.</param>
    /// <param name="literal">The object or array, as a JSON literal.</param>
    [TestMethod]
    [DataRow("priority", "{}")]
    [DataRow("priority", "[]")]
    [DataRow("category", "{}")]
    [DataRow("category", "[ \"PublicHoliday\" ]")]
    [DataRow("displayName", "{}")]
    [DataRow("displayName", "[]")]
    [DataRow("day", "{}")]
    public void FromJson_WhenScalarValueIsObjectOrArray_ShouldThrowFormatException(string field, string literal)
    {
        string json = ObjectValueJson(field, literal);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.Contains(field, ex.Message);
    }

    /// <summary>
    /// Verifies that an optional JSON scalar field holding the literal <c>null</c> reads as absent, as the loader reads
    /// it, so the field keeps its default.
    /// </summary>
    /// <param name="field">The JSON property holding <c>null</c>.</param>
    [TestMethod]
    [DataRow("priority")]
    public void FromJson_WhenScalarValueIsNull_ShouldBuildWhatLoadJsonLoads(string field)
    {
        string json = ObjectValueJson(field, "null");

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build();

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), field);
    }

    /// <summary>
    /// Builds a JSON document with one fixed concept whose named property, the rule's <c>priority</c>, the concept's
    /// <c>category</c> or <c>displayName</c>, or the fixed strategy's <c>day</c>, holds the supplied JSON literal.
    /// </summary>
    /// <param name="field">The property that holds the literal.</param>
    /// <param name="literal">The JSON literal.</param>
    /// <returns>The JSON document.</returns>
    private static string ObjectValueJson(string field, string literal)
    {
        string priority = field == "priority" ? literal : "3";
        string category = field == "category" ? literal : "\"PublicHoliday\"";
        string displayName = field == "displayName" ? literal : "\"X\"";
        string day = field == "day" ? literal : "1";

        return $$"""
            { "schemaVersion": "1.0", "resourceId": "test.objects",
              "notableDates": [ { "id": "x", "displayName": {{displayName}}, "category": {{category}},
                "rules": [ { "id": "r", "priority": {{priority}}, "strategy": { "fixed": { "month": 3, "day": {{day}} } } } ] } ] }
            """;
    }

    /// <summary>
    /// Verifies that a <c>PatchRule</c> override whose <c>strategy</c> or <c>recurrence</c> names no recognized kind,
    /// which the loader rejects with <c>BODU-CAL-RULE-SOURCE</c>, is rejected with a <see cref="FormatException" />
    /// carrying the loader's message, rather than built without the occurrence source.
    /// </summary>
    /// <param name="patch">The extra <c>PatchRule</c> properties.</param>
    [TestMethod]
    [DataRow(""", "strategy": { }""")]
    [DataRow(""", "strategy": { "unknownKind": { } }""")]
    [DataRow(""", "recurrence": { }""")]
    [DataRow(""", "recurrence": { "unknownKind": { } }""")]
    public void FromJson_WhenPatchOccurrenceSourceNamesNoKind_ShouldThrowFormatException(string patch)
    {
        string json = PatchSourceJson(patch);
        string expected = LoaderRuleSourceMessage(json);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that a <c>PatchRule</c> override naming two occurrence sources, two strategy kinds or a strategy and a
    /// recurrence, which the loader rejects with <c>BODU-CAL-RULE-SOURCE</c>, is rejected with a
    /// <see cref="FormatException" /> carrying the loader's message, rather than built with the first kind alone or left
    /// for <see cref="NotableDateDocumentBuilder.Build()" /> to reject.
    /// </summary>
    /// <param name="patch">The extra <c>PatchRule</c> properties.</param>
    [TestMethod]
    [DataRow(""", "strategy": { "fixed": { "month": 5, "day": 2 }, "algorithm": { "key": "western-easter" } }""")]
    [DataRow(""", "strategy": { "fixed": { "month": 5, "day": 2 } }, "recurrence": { "monthlyDay": { "dayOfMonth": 15 } }""")]
    public void FromJson_WhenPatchOccurrenceSourceNamesTwoKinds_ShouldThrowFormatException(string patch)
    {
        string json = PatchSourceJson(patch);
        string expected = LoaderRuleSourceMessage(json);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that a <c>PatchRule</c> override whose occurrence source the loader accepts, a kind named in another
    /// case or an empty <c>strategy</c> beside a recognized <c>recurrence</c>, builds through the builder into the same
    /// resource the loader loads.
    /// </summary>
    /// <param name="patch">The extra <c>PatchRule</c> properties.</param>
    [TestMethod]
    [DataRow(""", "strategy": { "Fixed": { "month": 5, "day": 2 } }""")]
    [DataRow(""", "strategy": { }, "recurrence": { "monthlyDay": { "dayOfMonth": 15 } }""")]
    public void FromJson_WhenPatchOccurrenceSourceIsRecognized_ShouldBuildWhatLoadJsonLoads(string patch)
    {
        string json = PatchSourceJson(patch);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build();

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), patch);
    }

    /// <summary>
    /// Returns the message of the <c>BODU-CAL-RULE-SOURCE</c> diagnostic the loader reports for a JSON document.
    /// </summary>
    /// <param name="json">The JSON document.</param>
    /// <returns>The diagnostic's message.</returns>
    private static string LoaderRuleSourceMessage(string json)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        return ex.Diagnostics.Single(d => d.Code == "BODU-CAL-RULE-SOURCE").Message;
    }

    /// <summary>
    /// Builds a JSON document whose one rule, a fixed 1 April, is patched by a <c>PatchRule</c> override carrying the
    /// supplied extra properties.
    /// </summary>
    /// <param name="patch">The extra <c>PatchRule</c> properties, each preceded by a comma.</param>
    /// <returns>The JSON document.</returns>
    private static string PatchSourceJson(string patch) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.patch-source",
          "notableDates": [ { "id": "other", "displayName": "Other", "category": "Observance",
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ],
          "overrides": [ { "operation": "PatchRule", "notableDateRef": "other", "ruleRef": "r", "priority": 2 @patch@ } ] }
        """.Replace("@patch@", patch, StringComparison.Ordinal);

    /// <summary>
    /// The JSON documents whose property names differ in case from the canonical names, keyed by scenario, each of
    /// which the loader reads as it reads the canonical names.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonCaseScenarios = new(StringComparer.Ordinal)
    {
        ["root"] = """
            { "SchemaVersion": "1.0", "ResourceId": "test.case",
              "NotableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 7, "day": 4 } } } ] } ] }
            """,
        ["concept and rule"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "notableDates": [ { "Id": "x", "DisplayName": "X", "Category": "PublicHoliday", "Tags": [ "civic" ],
                "Rules": [ { "Id": "r", "Priority": 3, "Strategy": { "fixed": { "month": 7, "day": 4 } } } ] } ] }
            """,
        ["strategy body"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "Month": 7, "DAY": 4 } } } ] } ] }
            """,
        ["recurrence body"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance",
                "rules": [ { "id": "r", "Recurrence": { "MonthlyDay": { "DayOfMonth": 15, "IntervalMonths": 2, "AnchorDate": "2026-01-15" } } } ] } ] }
            """,
        ["weekly days"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance",
                "rules": [ { "id": "r", "recurrence": { "weekly": { "Days": [ "Monday", "Friday" ], "IntervalWeeks": 2, "AnchorDate": "2026-01-05" } } } ] } ] }
            """,
        ["applicability and until date"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance",
                "rules": [ { "id": "r", "Applicability": { "Territories": [ "AU" ], "FromYear": 2020 },
                  "strategy": { "fixed": { "month": 3, "day": 1 } },
                  "Duration": { "UntilDate": { "Strategy": { "Fixed": { "Month": 3, "Day": 5 } }, "EndBoundary": "Exclusive" } } } ] } ] }
            """,
        ["adjustment policy"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "AdjustmentPolicies": [ { "Id": "p", "Trigger": { "Type": "IfWeekend" },
                "Action": { "Type": "MoveToNextWorkingDay", "SkipWeekends": true }, "Emission": { "Mode": "ObservedOnly", "Reason": "Moved" } } ],
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 7, "day": 4 } }, "Adjustments": [ "p" ] } ] } ] }
            """,
        ["override"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 7, "day": 4 } } } ] } ],
              "Overrides": [ { "Operation": "PatchRule", "NotableDateRef": "x", "RuleRef": "r", "Priority": 4 } ] }
            """,
        ["import"] = """
            { "schemaVersion": "1.0", "resourceId": "test.case",
              "Imports": [ { "Resource": "lib", "Uses": [ { "NotableDateRef": "lib-day", "As": "alias", "NonWorking": true } ] } ],
              "notableDates": [ { "id": "x", "displayName": "X", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 7, "day": 4 } } } ] } ] }
            """,
    };

    /// <summary>
    /// The JSON documents whose strategy or recurrence body holds a property the loader does not read, keyed by
    /// scenario.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonUnreadBodyPropertyScenarios = new(StringComparer.Ordinal)
    {
        ["fixed note"] = """{ "strategy": { "fixed": { "month": 3, "day": 1, "note": "ignored" } } }""",
        ["fixed object"] = """{ "strategy": { "fixed": { "month": 3, "day": 1, "meta": { "source": "x" } } } }""",
        ["monthlyDay extra"] = """{ "recurrence": { "monthlyDay": { "dayOfMonth": 15, "extra": 5 } } }""",
        ["weekly label"] = """{ "recurrence": { "weekly": { "days": [ "Monday" ], "label": "Mondays" } } }""",
    };

    /// <summary>
    /// Verifies that a JSON document whose property names differ in case from the canonical names, which the loader
    /// matches ignoring case, builds through the builder into the same resource the loader loads, compared byte for byte
    /// as binary packs.
    /// </summary>
    /// <param name="scenario">The name of the scenario.</param>
    [TestMethod]
    [DataRow("root")]
    [DataRow("concept and rule")]
    [DataRow("strategy body")]
    [DataRow("recurrence body")]
    [DataRow("weekly days")]
    [DataRow("applicability and until date")]
    [DataRow("adjustment policy")]
    [DataRow("override")]
    [DataRow("import")]
    public void FromJson_WhenPropertyNameDiffersInCase_ShouldBuildWhatLoadJsonLoads(string scenario)
    {
        string json = s_jsonCaseScenarios[scenario];

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, AnyLibraryResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(AnyLibraryResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), scenario);
    }

    /// <summary>
    /// Verifies that a strategy or recurrence body holding a property the loader does not read, which the loader
    /// ignores, builds through the builder into the same resource the loader loads, rather than reaching the document as
    /// an attribute the schema rejects.
    /// </summary>
    /// <param name="scenario">The name of the scenario.</param>
    [TestMethod]
    [DataRow("fixed note")]
    [DataRow("fixed object")]
    [DataRow("monthlyDay extra")]
    [DataRow("weekly label")]
    public void FromJson_WhenOccurrenceSourceHoldsPropertyTheLoaderIgnores_ShouldBuildWhatLoadJsonLoads(string scenario)
    {
        string json = $$"""
            { "schemaVersion": "1.0", "resourceId": "test.unread-body",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance",
                "rules": [ { "id": "r", {{s_jsonUnreadBodyPropertyScenarios[scenario][1..^1]}} } ] } ] }
            """;

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build();

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), scenario);
    }

    /// <summary>
    /// Verifies that a rule whose strategy and recurrence name no recognized kind between them, which the loader rejects
    /// with <c>BODU-CAL-RULE-SOURCE</c>, is rejected with a <see cref="FormatException" /> carrying the loader's message,
    /// rather than built without an occurrence source.
    /// </summary>
    /// <param name="rule">The rule, as a JSON object.</param>
    [TestMethod]
    [DataRow("""{ "id": "r" }""")]
    [DataRow("""{ "id": "r", "strategy": { } }""")]
    [DataRow("""{ "id": "r", "strategy": { "fixd": { "month": 3, "day": 1 } } }""")]
    [DataRow("""{ "id": "r", "recurrence": { } }""")]
    public void FromJson_WhenRuleOccurrenceSourceNamesNoKind_ShouldThrowFormatException(string rule)
    {
        string json = RuleJson(rule);
        string expected = LoaderDiagnosticMessage(json, "BODU-CAL-RULE-SOURCE");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that a rule whose strategy and recurrence name more than one kind between them, which the loader rejects
    /// with <c>BODU-CAL-RULE-SOURCE</c>, is rejected with a <see cref="FormatException" /> carrying the loader's message,
    /// rather than built with the first kind alone.
    /// </summary>
    /// <param name="rule">The rule, as a JSON object.</param>
    [TestMethod]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 }, "algorithm": { "key": "western-easter" } } }""")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } }, "recurrence": { "weekly": { "days": [ "Monday" ] } } }""")]
    [DataRow("""{ "id": "r", "recurrence": { "weekly": { "days": [ "Monday" ] }, "monthlyDay": { "dayOfMonth": 1 } } }""")]
    public void FromJson_WhenRuleOccurrenceSourceNamesTwoKinds_ShouldThrowFormatException(string rule)
    {
        string json = RuleJson(rule);
        string expected = LoaderDiagnosticMessage(json, "BODU-CAL-RULE-SOURCE");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an <c>AddRule</c> override whose rule names two strategy kinds, which the loader rejects with
    /// <c>BODU-CAL-RULE-SOURCE</c>, is rejected with a <see cref="FormatException" /> carrying the loader's message.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenAddedRuleNamesTwoKinds_ShouldThrowFormatException()
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.add-rule-source",
              "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] } ],
              "overrides": [ { "operation": "AddRule", "notableDateRef": "x",
                "rule": { "id": "added", "strategy": { "fixed": { "month": 4, "day": 1 }, "dayOfYear": { "ordinal": 100 } } } } ] }
            """;
        string expected = LoaderDiagnosticMessage(json, "BODU-CAL-RULE-SOURCE");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an <c>untilDate</c> whose end strategy is absent or names no recognized kind, which the loader
    /// rejects with <c>BODU-CAL-DURATION-NOEND</c>, is rejected with a <see cref="FormatException" /> carrying the
    /// loader's message.
    /// </summary>
    /// <param name="untilDate">The <c>untilDate</c>, as a JSON object.</param>
    [TestMethod]
    [DataRow("""{ "endBoundary": "Exclusive" }""")]
    [DataRow("""{ "strategy": { } }""")]
    [DataRow("""{ "strategy": { "fixd": { "month": 3, "day": 5 } } }""")]
    public void FromJson_WhenUntilDateStrategyNamesNoKind_ShouldThrowFormatException(string untilDate)
    {
        string json = UntilDateJson(untilDate);
        string expected = LoaderDiagnosticMessage(json, "BODU-CAL-DURATION-NOEND");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an <c>untilDate</c> whose end strategy names two kinds, which the loader rejects with
    /// <c>BODU-CAL-DURATION-CONFLICT</c>, is rejected with a <see cref="FormatException" /> carrying the loader's message,
    /// rather than built with the first kind alone.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenUntilDateStrategyNamesTwoKinds_ShouldThrowFormatException()
    {
        string json = UntilDateJson("""{ "strategy": { "fixed": { "month": 3, "day": 5 }, "dayOfYear": { "ordinal": 100 } } }""");
        string expected = LoaderDiagnosticMessage(json, "BODU-CAL-DURATION-CONFLICT");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that a <c>PatchRule</c> override whose <c>untilDate</c> end strategy is absent, names no recognized kind
    /// or names two, which the loader rejects with the diagnostic named, is rejected with a
    /// <see cref="FormatException" /> carrying the loader's message.
    /// </summary>
    /// <param name="patch">The extra <c>PatchRule</c> properties.</param>
    /// <param name="code">The code of the diagnostic the loader reports.</param>
    [TestMethod]
    [DataRow(""", "duration": { "untilDate": { "endBoundary": "Exclusive" } }""", "BODU-CAL-DURATION-NOEND")]
    [DataRow(""", "duration": { "untilDate": { "strategy": { "fixd": { "month": 4, "day": 5 } } } }""", "BODU-CAL-DURATION-NOEND")]
    [DataRow(""", "duration": { "untilDate": { "strategy": { "fixed": { "month": 4, "day": 5 }, "dayOfYear": { "ordinal": 100 } } } }""", "BODU-CAL-DURATION-CONFLICT")]
    public void FromJson_WhenPatchUntilDateStrategyIsNotOneKind_ShouldThrowFormatException(string patch, string code)
    {
        string json = PatchSourceJson(patch);
        string expected = LoaderDiagnosticMessage(json, code);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Returns the message of the one diagnostic with the supplied code that the loader reports for a JSON document.
    /// </summary>
    /// <param name="json">The JSON document.</param>
    /// <param name="code">The diagnostic code.</param>
    /// <returns>The diagnostic's message.</returns>
    private static string LoaderDiagnosticMessage(string json, string code)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        return ex.Diagnostics.Single(d => d.Code == code).Message;
    }

    /// <summary>
    /// Builds a JSON document whose one concept, <c>x</c>, holds the supplied rule.
    /// </summary>
    /// <param name="rule">The rule, as a JSON object.</param>
    /// <returns>The JSON document.</returns>
    private static string RuleJson(string rule) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.rule-source",
          "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance", "rules": [ @rule@ ] } ] }
        """.Replace("@rule@", rule, StringComparison.Ordinal);

    /// <summary>
    /// Builds a JSON document whose one rule, a fixed 1 March, lasts until the supplied <c>untilDate</c>.
    /// </summary>
    /// <param name="untilDate">The <c>untilDate</c>, as a JSON object.</param>
    /// <returns>The JSON document.</returns>
    private static string UntilDateJson(string untilDate) =>
        RuleJson("""{ "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } }, "duration": { "untilDate": @until@ } }""".Replace("@until@", untilDate, StringComparison.Ordinal));

    /// <summary>
    /// The well-formed value, as a JSON literal, of every object- and list-valued property the JSON document under the
    /// value-kind tests holds, keyed by the placeholder that stands for it, in the order the placeholders are replaced:
    /// a value that holds other placeholders precedes them.
    /// </summary>
    private static readonly (string Key, string WellFormed)[] s_jsonKindFieldValues =
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
    /// Verifies that the value-kind test document, every object- and list-valued property holding a value of its kind,
    /// builds through the builder into the same resource the loader loads, so each rejection the per-property tests
    /// observe is caused by the one value they replace.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenEveryObjectAndListValueIsOfItsKind_ShouldBuildWhatLoadJsonLoads()
    {
        string json = KindFieldJson(null, null);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, AnyLibraryResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(AnyLibraryResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built));
    }

    /// <summary>
    /// Verifies that a JSON property that must hold an object or an array, holding a value of another kind, which the
    /// loader rejects with <c>BODU-CAL-VALUE</c>, is rejected with a <see cref="FormatException" /> carrying the
    /// loader's message, rather than read as absent.
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
    public void FromJson_WhenObjectOrListValueIsOfAnotherKind_ShouldThrowFormatException(string field, string property, string literal)
    {
        string json = KindFieldJson(field, literal);

        NotableDateValidationException loaderException = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, AnyLibraryResolver);
        });

        string expected = loaderException.Diagnostics.Single(d => d.Code == "BODU-CAL-VALUE").Message;
        Assert.Contains("'" + property + "'", expected);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an optional JSON object- or list-valued property holding the literal <c>null</c>, which the
    /// loader reads as absent, builds through the builder into the same resource the loader loads.
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
    public void FromJson_WhenObjectOrListValueIsNull_ShouldBuildWhatLoadJsonLoads(string field)
    {
        string json = KindFieldJson(field, "null");

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, AnyLibraryResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(AnyLibraryResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), field);
    }

    /// <summary>
    /// Builds a JSON document holding every object- and list-valued property the builder reads, each holding a value of
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

        foreach ((string key, string wellFormed) in s_jsonKindFieldValues)
            json = json.Replace("@" + key + "@", key == field ? literal : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Verifies that an override whose <c>operation</c> names no operation, or is absent, which the loader rejects, is
    /// rejected with a <see cref="FormatException" /> carrying the loader's message, rather than read as a
    /// <c>PatchRule</c>.
    /// </summary>
    /// <param name="operation">The <c>operation</c> property, as a JSON member, or empty to omit it.</param>
    [TestMethod]
    [DataRow("\"operation\": \"Patch\",")]
    [DataRow("\"operation\": \"UpdateRule\",")]
    [DataRow("\"operation\": \"\",")]
    [DataRow("\"operation\": 5,")]
    [DataRow("")]
    public void FromJson_WhenOverrideOperationIsUnrecognized_ShouldThrowFormatException(string operation)
    {
        string json = OverrideOperationJson($$"""{ {{operation}} "notableDateRef": "x", "ruleRef": "r", "priority": 7 }""");
        string expected = LoaderErrorMessage(json);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an <c>AddRule</c> override without a <c>rule</c>, or whose <c>rule</c> is the literal <c>null</c>,
    /// which the loader rejects, is rejected with a <see cref="FormatException" /> carrying the loader's message,
    /// rather than read as a rule with an empty identifier.
    /// </summary>
    /// <param name="rule">The <c>rule</c> property, as a JSON member, or empty to omit it.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow(", \"rule\": null")]
    public void FromJson_WhenAddRuleHasNoRule_ShouldThrowFormatException(string rule)
    {
        string json = OverrideOperationJson($$"""{ "operation": "AddRule", "notableDateRef": "x"{{rule}} }""");
        string expected = LoaderErrorMessage(json);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an override whose operation is named in another case, which the loader matches ignoring case,
    /// builds through the builder into the same resource the loader loads.
    /// </summary>
    /// <param name="operation">The override, as a JSON object.</param>
    [TestMethod]
    [DataRow("""{ "operation": "patchRule", "notableDateRef": "x", "ruleRef": "r", "priority": 7 }""")]
    [DataRow("""{ "operation": "REMOVERULE", "notableDateRef": "x", "ruleRef": "s" }""")]
    [DataRow("""{ "operation": "addrule", "notableDateRef": "x", "rule": { "id": "t", "strategy": { "fixed": { "month": 5, "day": 1 } } } }""")]
    public void FromJson_WhenOverrideOperationDiffersInCase_ShouldBuildWhatLoadJsonLoads(string operation)
    {
        string json = OverrideOperationJson(operation);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build();

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), operation);
    }

    /// <summary>
    /// Returns the message of the one error diagnostic the loader reports for a JSON document.
    /// </summary>
    /// <param name="json">The JSON document.</param>
    /// <returns>The diagnostic's message.</returns>
    private static string LoaderErrorMessage(string json)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json);
        });

        return ex.Diagnostics.Single(d => d.Severity == NotableDateValidationSeverity.Error).Message;
    }

    /// <summary>
    /// Builds a JSON document whose one concept, <c>x</c>, holds the rules <c>r</c> (1 March) and <c>s</c> (1 April),
    /// with the supplied override.
    /// </summary>
    /// <param name="operation">The override, as a JSON object.</param>
    /// <returns>The JSON document.</returns>
    private static string OverrideOperationJson(string operation) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.overrides",
          "notableDates": [ { "id": "x", "displayName": "X", "category": "Observance",
            "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } } },
                       { "id": "s", "strategy": { "fixed": { "month": 4, "day": 1 } } } ] } ],
          "overrides": [ @operation@ ] }
        """.Replace("@operation@", operation, StringComparison.Ordinal);

    /// <summary>
    /// Verifies that a strategy, recurrence or end-strategy attribute value the loader rejects, on a rule, an
    /// <c>AddRule</c> override's rule or a <c>PatchRule</c> override, is rejected with a <see cref="FormatException" />
    /// carrying the loader's message, rather than written into the document for the schema to reject at
    /// <c>Build</c> or failing with the framework's own message.
    /// </summary>
    /// <param name="rule">The rule of the concept <c>x</c>, as a JSON object.</param>
    /// <param name="overrides">The document's overrides, as a JSON member, or empty for none.</param>
    [TestMethod]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1, "day": 1.5 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1, "day": "high" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1, "day": true } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1, "day": 1e1 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1, "day": 1, "sweepCalendarYears": "maybe" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1, "day": 1, "skipLeapMonth": 2 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1, "day": 1, "skipLeapMonth": "TRUE" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 1.5, "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": "Smarch", "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "Hebrew" }, "strategy": { "fixed": { "month": "nisan", "day": 15 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "Hijri" }, "strategy": { "fixed": { "month": "Nisan", "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "Persian" }, "strategy": { "fixed": { "month": 13, "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "ChineseLunisolar" }, "strategy": { "fixed": { "month": 13, "day": 1, "skipLeapMonth": true } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "ChineseLunisolar" }, "strategy": { "fixed": { "skipLeapMonth": true, "month": 13, "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "Hebrew" }, "strategy": { "fixed": { "month": 14, "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "dayOfWeekInMonth": { "month": 5, "dayOfWeek": "Funday", "weekOrdinal": "First" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "dayOfWeekInMonth": { "month": 5, "dayOfWeek": "Monday", "weekOrdinal": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "weekdayNearDate": { "month": 5, "day": 1, "dayOfWeek": "Monday", "direction": "Sideways" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "weekdayNearDate": { "month": 5, "day": 1, "dayOfWeek": "Monday ", "direction": "After" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "relativeWeekdayInMonth": { "month": 5, "dayOfWeek": "Monday", "weekOrdinal": "First", "relativeDayOfWeek": "Someday", "direction": "After" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "offsetFromRule": { "notableDateRef": "anchor", "offsetDays": "soon" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "weekdayNearRule": { "notableDateRef": "anchor", "dayOfWeek": "Monday", "direction": "After", "referenceYearOffset": 0.5 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "anchor", "dayOfWeek": "Monday", "ordinal": "first" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "workingDayOffsetFromRule": { "notableDateRef": "anchor", "offsetWorkingDays": true } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "workingDayInMonth": { "month": 5, "ordinal": 2.5 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "ordinalDayOfMonth": { "month": "Smarch", "ordinal": 2 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "isoWeekDate": { "week": "x", "dayOfWeek": "Monday" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "dayOfYear": { "ordinal": "last" } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "dailyInterval": { "anchorDate": "2024-13-01" } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "dailyInterval": { "anchorDate": "2024-01-01", "intervalDays": "daily" } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "weekly": { "days": [ "Monday" ], "intervalWeeks": "2x", "anchorDate": "2024-01-01" } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "weekly": { "days": [ "Monday" ], "anchorDate": " 2024-01-01" } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "monthlyDay": { "dayOfMonth": 2.5 } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "monthlyDay": { "dayOfMonth": 2, "invalidDayBehavior": "Nope" } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "monthlyWeekday": { "dayOfWeek": "Monday", "weekOrdinal": "Penultimate" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } }, "duration": { "untilDate": { "strategy": { "fixed": { "month": 5, "day": "x" } } } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } } }""", """, "overrides": [ { "operation": "AddRule", "notableDateRef": "x", "rule": { "id": "t", "strategy": { "fixed": { "month": 6, "day": "x" } } } } ]""")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } } }""", """, "overrides": [ { "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "strategy": { "dayOfWeekInMonth": { "month": 5, "dayOfWeek": "Monday", "weekOrdinal": "Tenth" } } } ]""")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } } }""", """, "overrides": [ { "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "recurrence": { "monthlyDay": { "dayOfMonth": "x" } } } ]""")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } } }""", """, "overrides": [ { "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "duration": { "untilDate": { "strategy": { "fixed": { "month": 5, "day": false } } } } } ]""")]
    public void FromJson_WhenStrategyAttributeValueIsMalformed_ShouldThrowFormatException(string rule, string overrides)
    {
        string json = AttributeValueJson(rule, overrides);
        string expected = LoaderErrorMessage(json);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that a strategy, recurrence or end-strategy attribute value the loader reads, in a form the schema does
    /// not accept as written (a name in another case, or a numeric month on a rule in another calendar), builds through
    /// the builder into the same resource the loader loads, compared byte for byte as binary rule packs, rather than
    /// being written as given and failing at <c>Build</c>.
    /// </summary>
    /// <param name="rule">The rule of the concept <c>x</c>, as a JSON object.</param>
    /// <param name="overrides">The document's overrides, as a JSON member, or empty for none.</param>
    [TestMethod]
    [DataRow("""{ "id": "r", "strategy": { "dayOfWeekInMonth": { "month": 5, "dayOfWeek": "monday", "weekOrdinal": "LAST" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "weekdayNearDate": { "month": 5, "day": 24, "dayOfWeek": "MONDAY", "direction": "onorbefore" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "relativeWeekdayInMonth": { "month": 11, "dayOfWeek": "thursday", "weekOrdinal": "fourth", "relativeDayOfWeek": "FRIDAY", "direction": "after" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "weekdayNearRule": { "notableDateRef": "anchor", "dayOfWeek": "sunday", "direction": "nearest" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "anchor", "dayOfWeek": "tuesday", "ordinal": 2 } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "isoWeekDate": { "week": 10, "dayOfWeek": "wednesday" } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "Hebrew" }, "strategy": { "fixed": { "month": 7, "day": 15 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "Hijri" }, "strategy": { "fixed": { "month": "9", "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "ChineseLunisolar" }, "strategy": { "fixed": { "month": 13, "day": 1 } } }""", "")]
    [DataRow("""{ "id": "r", "applicability": { "calendar": "Hebrew" }, "strategy": { "fixed": { "month": "LastAdar", "day": 14, "sweepCalendarYears": true } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "monthlyDay": { "dayOfMonth": 31, "invalidDayBehavior": "uselastdayofmonth" } } }""", "")]
    [DataRow("""{ "id": "r", "recurrence": { "monthlyWeekday": { "dayOfWeek": "friday", "weekOrdinal": "last" } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } }, "duration": { "untilDate": { "strategy": { "weekdayNearDate": { "month": 5, "day": 3, "dayOfWeek": "friday", "direction": "onorafter" } } } } }""", "")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } } }""", """, "overrides": [ { "operation": "PatchRule", "notableDateRef": "x", "ruleRef": "r", "strategy": { "dayOfWeekInMonth": { "month": 6, "dayOfWeek": "saturday", "weekOrdinal": "second" } } } ]""")]
    [DataRow("""{ "id": "r", "strategy": { "fixed": { "month": 5, "day": 1 } } }""", """, "overrides": [ { "operation": "AddRule", "notableDateRef": "x", "rule": { "id": "t", "applicability": { "calendar": "Hebrew" }, "strategy": { "fixed": { "month": 1, "day": 1 } } } } ]""")]
    public void FromJson_WhenStrategyAttributeValueIsLoaderForm_ShouldBuildWhatLoadJsonLoads(string rule, string overrides)
    {
        string json = AttributeValueJson(rule, overrides);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build();

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), rule + overrides);
    }

    /// <summary>
    /// Builds a JSON document whose concept <c>anchor</c> holds the rule <c>a</c> (1 March) and whose concept
    /// <c>x</c> holds the supplied rule, with the supplied overrides.
    /// </summary>
    /// <param name="rule">The rule of the concept <c>x</c>, as a JSON object.</param>
    /// <param name="overrides">The document's overrides, as a JSON member, or empty for none.</param>
    /// <returns>The JSON document.</returns>
    private static string AttributeValueJson(string rule, string overrides) =>
        """
        { "schemaVersion": "1.0", "resourceId": "test.attributes",
          "notableDates": [
            { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday",
              "rules": [ { "id": "a", "strategy": { "fixed": { "month": 3, "day": 1 } } } ] },
            { "id": "x", "displayName": "X", "category": "Observance", "rules": [ @rule@ ] } ]
          @overrides@ }
        """
        .Replace("@rule@", rule, StringComparison.Ordinal)
        .Replace("@overrides@", overrides, StringComparison.Ordinal);

    /// <summary>
    /// Verifies that a JSON scalar field holding an object or an array written with white space, which the loader
    /// rejects with <c>BODU-CAL-VALUE</c>, is rejected with a <see cref="FormatException" /> carrying the loader's
    /// message, rather than one quoting the value as the builder re-serializes it.
    /// </summary>
    /// <param name="field">The JSON property holding the value.</param>
    /// <param name="literal">The object or array, as a JSON literal.</param>
    [TestMethod]
    [DataRow("priority", "{ }")]
    [DataRow("category", "{ \"en\": \"X\" }")]
    [DataRow("displayName", "[ 1, 2 ]")]
    [DataRow("day", "{ \"value\": 1 }")]
    public void FromJson_WhenScalarValueHoldsSpacedObjectOrArray_ShouldThrowLoadersMessage(string field, string literal)
    {
        string json = ObjectValueJson(field, literal);
        string expected = LoaderErrorMessage(json);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an item of a JSON list of territory codes, references, tags, enumeration names or years that is an
    /// object or an array written with white space, which the loader rejects with a single error, is rejected with a
    /// <see cref="FormatException" /> carrying the loader's message, rather than one quoting the item as the builder
    /// re-serializes it.
    /// </summary>
    /// <param name="document">The test document: <c>list</c> or <c>scalar</c>.</param>
    /// <param name="field">The placeholder of the list holding the item.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    [TestMethod]
    [DataRow("list", "notableDate.tags", "{ \"a\": 1 }")]
    [DataRow("list", "rule.adjustments", "[ \"policy\" ]")]
    [DataRow("list", "applicability.territories", "[ ]")]
    [DataRow("list", "scope.notableDateRefs", "{ }")]
    [DataRow("list", "patch.tags", "[ 1, 2 ]")]
    [DataRow("list", "use.adjustments", "{ \"ref\": \"policy\" }")]
    [DataRow("list", "categoryPrecedence", "{ }")]
    [DataRow("list", "scope.calendars", "[ \"Gregorian\" ]")]
    [DataRow("list", "trigger.weekdays", "[ ]")]
    [DataRow("scalar", "applicability.onlyYears", "{ \"year\": 2026 }")]
    [DataRow("scalar", "applicability.exceptYears", "[ 2027 ]")]
    public void FromJson_WhenListItemIsSpacedObjectOrArray_ShouldThrowLoadersMessage(string document, string field, string item)
    {
        string json = document == "list" ? ListFieldJson(field, item) : ScalarFieldJson(field, item);
        string expected = LoaderErrorMessage(json, AnyLibraryResolver);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Verifies that an item of a JSON scope's <c>ruleRefs</c> that is an array written with white space, which the
    /// loader rejects with a single error, is rejected with a <see cref="FormatException" /> carrying the loader's
    /// message.
    /// </summary>
    /// <param name="item">The item, as a JSON literal.</param>
    [TestMethod]
    [DataRow("[ \"r\" ]")]
    [DataRow("[ ]")]
    public void FromJson_WhenScopeRuleReferenceIsSpacedArray_ShouldThrowLoadersMessage(string item)
    {
        string json = """
            { "schemaVersion": "1.0", "resourceId": "test.rulerefs",
              "adjustmentPolicies": [ { "id": "policy", "scope": { "ruleRefs": [ @item@ ] },
                "trigger": { "type": "IfWeekend" }, "action": { "type": "MoveToNextWorkingDay" }, "emission": { "mode": "ObservedOnly" } } ],
              "notableDates": [ { "id": "anchor", "displayName": "Anchor", "category": "PublicHoliday",
                "rules": [ { "id": "r", "strategy": { "fixed": { "month": 3, "day": 1 } }, "adjustments": [ "policy" ] } ] } ] }
            """.Replace("@item@", item, StringComparison.Ordinal);
        string expected = LoaderErrorMessage(json);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Returns the message of the one error diagnostic the loader reports for a JSON document whose imports a resolver
    /// supplies.
    /// </summary>
    /// <param name="json">The JSON document.</param>
    /// <param name="resolver">The resolver that supplies the document's imports.</param>
    /// <returns>The diagnostic's message.</returns>
    private static string LoaderErrorMessage(string json, Func<string, string?> resolver)
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson(json, resolver);
        });

        return ex.Diagnostics.Single(d => d.Severity == NotableDateValidationSeverity.Error).Message;
    }

    /// <summary>
    /// Verifies that an item of a JSON list of objects (concepts, rules, adjustment policies, imports, uses or
    /// overrides) that is not an object, which the loader rejects with <c>BODU-CAL-VALUE</c>, is rejected with a
    /// <see cref="FormatException" /> carrying the loader's message, rather than skipped.
    /// </summary>
    /// <param name="placeholder">The placeholder before which the item is added.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    [TestMethod]
    [DataRow("notableDate.item", "5")]
    [DataRow("notableDate.item", "[ ]")]
    [DataRow("policy.item", "true")]
    [DataRow("rule.item", "\"r\"")]
    [DataRow("import.item", "null")]
    [DataRow("use.item", "5")]
    [DataRow("override.item", "5")]
    [DataRow("override.item", "\"PatchRule\"")]
    public void FromJson_WhenObjectListHoldsNonObjectItem_ShouldThrowFormatException(string placeholder, string item)
    {
        string json = ObjectItemJson(placeholder, item);
        string expected = LoaderErrorMessage(json, BuilderEnumImportResolver);

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected, ex.Message);
    }

    /// <summary>
    /// Builds a JSON document whose lists of concepts, rules, adjustment policies, imports, uses and overrides each
    /// hold one well-formed object, with the supplied item added before the object of the named list.
    /// </summary>
    /// <param name="placeholder">The placeholder of the list to add the item to.</param>
    /// <param name="item">The item, as a JSON literal.</param>
    /// <returns>The JSON document.</returns>
    private static string ObjectItemJson(string placeholder, string item)
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
    /// schema requires of it and every integer the schema bounds.
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
    /// The JSON pointer into <see cref="SchemaTemplateJson" /> of each field the schema tests set or remove, keyed by
    /// the name that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_schemaFieldPointers = new(StringComparer.Ordinal)
    {
        ["schemaVersion"] = "/schemaVersion",
        ["resourceId"] = "/resourceId",
        ["policy.id"] = "/adjustmentPolicies/0/id",
        ["policy.trigger"] = "/adjustmentPolicies/0/trigger",
        ["policy.action"] = "/adjustmentPolicies/0/action",
        ["policy.emission"] = "/adjustmentPolicies/0/emission",
        ["scope.fromYear"] = "/adjustmentPolicies/0/scope/fromYear",
        ["scope.toYear"] = "/adjustmentPolicies/0/scope/toYear",
        ["scope.ruleRefs.notableDateRef"] = "/adjustmentPolicies/0/scope/ruleRefs/0/notableDateRef",
        ["scope.ruleRefs.ruleRef"] = "/adjustmentPolicies/0/scope/ruleRefs/0/ruleRef",
        ["scope.onlyYears.item"] = "/adjustmentPolicies/0/scope/onlyYears/0",
        ["scope.exceptYears.item"] = "/adjustmentPolicies/0/scope/exceptYears/0",
        ["trigger.type"] = "/adjustmentPolicies/0/trigger/type",
        ["trigger.day"] = "/adjustmentPolicies/0/trigger/day",
        ["action.type"] = "/adjustmentPolicies/0/action/type",
        ["action.maxSearchDays"] = "/adjustmentPolicies/0/action/maxSearchDays",
        ["emission.mode"] = "/adjustmentPolicies/0/emission/mode",
        ["import.resource"] = "/imports/0/resource",
        ["use.notableDateRef"] = "/imports/0/uses/0/notableDateRef",
        ["notableDate.id"] = "/notableDates/0/id",
        ["notableDate.displayName"] = "/notableDates/0/displayName",
        ["notableDate.category"] = "/notableDates/0/category",
        ["notableDate.defaultDurationDays"] = "/notableDates/0/defaultDurationDays",
        ["notableDate.rules"] = "/notableDates/1/rules",
        ["rule.id"] = "/notableDates/1/rules/0/id",
        ["rule.durationDays"] = "/notableDates/0/rules/0/durationDays",
        ["applicability.fromYear"] = "/notableDates/0/rules/0/applicability/fromYear",
        ["applicability.toYear"] = "/notableDates/0/rules/0/applicability/toYear",
        ["applicability.everyYears"] = "/notableDates/0/rules/0/applicability/everyYears",
        ["applicability.anchorYear"] = "/notableDates/0/rules/0/applicability/anchorYear",
        ["applicability.onlyYears.item"] = "/notableDates/0/rules/0/applicability/onlyYears/0",
        ["applicability.exceptYears.item"] = "/notableDates/0/rules/0/applicability/exceptYears/0",
        ["fixed.month"] = "/notableDates/0/rules/0/strategy/fixed/month",
        ["fixed.day"] = "/notableDates/0/rules/0/strategy/fixed/day",
        ["dayOfWeekInMonth.month"] = "/notableDates/1/rules/0/strategy/dayOfWeekInMonth/month",
        ["dayOfWeekInMonth.dayOfWeek"] = "/notableDates/1/rules/0/strategy/dayOfWeekInMonth/dayOfWeek",
        ["dayOfWeekInMonth.weekOrdinal"] = "/notableDates/1/rules/0/strategy/dayOfWeekInMonth/weekOrdinal",
        ["weekdayNearDate.month"] = "/notableDates/1/rules/1/strategy/weekdayNearDate/month",
        ["weekdayNearDate.day"] = "/notableDates/1/rules/1/strategy/weekdayNearDate/day",
        ["weekdayNearDate.dayOfWeek"] = "/notableDates/1/rules/1/strategy/weekdayNearDate/dayOfWeek",
        ["weekdayNearDate.direction"] = "/notableDates/1/rules/1/strategy/weekdayNearDate/direction",
        ["relativeWeekdayInMonth.month"] = "/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/month",
        ["relativeWeekdayInMonth.dayOfWeek"] = "/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/dayOfWeek",
        ["relativeWeekdayInMonth.weekOrdinal"] = "/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/weekOrdinal",
        ["relativeWeekdayInMonth.relativeDayOfWeek"] = "/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/relativeDayOfWeek",
        ["relativeWeekdayInMonth.direction"] = "/notableDates/1/rules/2/strategy/relativeWeekdayInMonth/direction",
        ["offsetFromRule.notableDateRef"] = "/notableDates/1/rules/3/strategy/offsetFromRule/notableDateRef",
        ["offsetFromRule.offsetDays"] = "/notableDates/1/rules/3/strategy/offsetFromRule/offsetDays",
        ["algorithm.key"] = "/notableDates/1/rules/4/strategy/algorithm/key",
        ["weekdayNearRule.notableDateRef"] = "/notableDates/1/rules/5/strategy/weekdayNearRule/notableDateRef",
        ["weekdayNearRule.dayOfWeek"] = "/notableDates/1/rules/5/strategy/weekdayNearRule/dayOfWeek",
        ["weekdayNearRule.direction"] = "/notableDates/1/rules/5/strategy/weekdayNearRule/direction",
        ["nthWeekdayFromRule.notableDateRef"] = "/notableDates/1/rules/6/strategy/nthWeekdayFromRule/notableDateRef",
        ["nthWeekdayFromRule.dayOfWeek"] = "/notableDates/1/rules/6/strategy/nthWeekdayFromRule/dayOfWeek",
        ["nthWeekdayFromRule.ordinal"] = "/notableDates/1/rules/6/strategy/nthWeekdayFromRule/ordinal",
        ["workingDayOffsetFromRule.notableDateRef"] = "/notableDates/1/rules/7/strategy/workingDayOffsetFromRule/notableDateRef",
        ["workingDayOffsetFromRule.offsetWorkingDays"] = "/notableDates/1/rules/7/strategy/workingDayOffsetFromRule/offsetWorkingDays",
        ["workingDayInMonth.month"] = "/notableDates/1/rules/8/strategy/workingDayInMonth/month",
        ["workingDayInMonth.ordinal"] = "/notableDates/1/rules/8/strategy/workingDayInMonth/ordinal",
        ["ordinalDayOfMonth.month"] = "/notableDates/1/rules/9/strategy/ordinalDayOfMonth/month",
        ["ordinalDayOfMonth.ordinal"] = "/notableDates/1/rules/9/strategy/ordinalDayOfMonth/ordinal",
        ["isoWeekDate.week"] = "/notableDates/1/rules/10/strategy/isoWeekDate/week",
        ["isoWeekDate.dayOfWeek"] = "/notableDates/1/rules/10/strategy/isoWeekDate/dayOfWeek",
        ["dayOfYear.ordinal"] = "/notableDates/1/rules/11/strategy/dayOfYear/ordinal",
        ["duration.untilDate"] = "/notableDates/1/rules/12/duration/untilDate",
        ["untilDate.fixed.month"] = "/notableDates/1/rules/12/duration/untilDate/strategy/fixed/month",
        ["untilDate.fixed.day"] = "/notableDates/1/rules/12/duration/untilDate/strategy/fixed/day",
        ["dailyInterval.anchorDate"] = "/notableDates/1/rules/13/recurrence/dailyInterval/anchorDate",
        ["dailyInterval.intervalDays"] = "/notableDates/1/rules/13/recurrence/dailyInterval/intervalDays",
        ["weekly.days"] = "/notableDates/1/rules/14/recurrence/weekly/days",
        ["weekly.intervalWeeks"] = "/notableDates/1/rules/14/recurrence/weekly/intervalWeeks",
        ["monthlyDay.dayOfMonth"] = "/notableDates/1/rules/15/recurrence/monthlyDay/dayOfMonth",
        ["monthlyDay.intervalMonths"] = "/notableDates/1/rules/15/recurrence/monthlyDay/intervalMonths",
        ["monthlyWeekday.dayOfWeek"] = "/notableDates/1/rules/16/recurrence/monthlyWeekday/dayOfWeek",
        ["monthlyWeekday.weekOrdinal"] = "/notableDates/1/rules/16/recurrence/monthlyWeekday/weekOrdinal",
        ["monthlyWeekday.intervalMonths"] = "/notableDates/1/rules/16/recurrence/monthlyWeekday/intervalMonths",
        ["patch.notableDateRef"] = "/overrides/0/notableDateRef",
        ["patch.ruleRef"] = "/overrides/0/ruleRef",
        ["patch.durationDays"] = "/overrides/0/durationDays",
        ["remove.notableDateRef"] = "/overrides/1/notableDateRef",
        ["remove.ruleRef"] = "/overrides/1/ruleRef",
        ["add.notableDateRef"] = "/overrides/2/notableDateRef",
        ["add.rule"] = "/overrides/2/rule",
        ["add.rule.id"] = "/overrides/2/rule/id",
        ["add.fixed.day"] = "/overrides/2/rule/strategy/fixed/day",
    };

    /// <summary>
    /// Verifies that the schema test document builds, through the builder, into the same resource the loader loads,
    /// compared byte for byte as binary rule packs, so each rejection the per-field tests observe is caused by the one
    /// field they change.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenEverySchemaRequiredValueIsPresent_ShouldBuildWhatLoadJsonLoads()
    {
        string json = SchemaFieldJson(null, null);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, BuilderEnumImportResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(BuilderEnumImportResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built));
    }

    /// <summary>
    /// Verifies that a JSON document without a property the schema requires, which the loader reports with
    /// <c>BODU-CAL-SCHEMA</c>, is rejected with a <see cref="FormatException" /> carrying the loader's message, rather
    /// than read with a default in the property's place.
    /// </summary>
    /// <param name="field">The name of the required property removed from the document.</param>
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
    public void FromJson_WhenSchemaRequiredPropertyIsAbsent_ShouldThrowLoadersMessage(string field)
    {
        AssertThrowsLoaderSchemaMessage(SchemaFieldJson(field, null));
    }

    /// <summary>
    /// Verifies that a JSON property the schema requires, holding the literal <c>null</c>, which the loader reads as
    /// absent and reports with <c>BODU-CAL-SCHEMA</c>, is rejected with a <see cref="FormatException" /> carrying the
    /// loader's message.
    /// </summary>
    /// <param name="field">The name of the required property set to <c>null</c>.</param>
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
    public void FromJson_WhenSchemaRequiredPropertyIsNull_ShouldThrowLoadersMessage(string field)
    {
        AssertThrowsLoaderSchemaMessage(SchemaFieldJson(field, "null"));
    }

    /// <summary>
    /// Verifies that a JSON list the schema requires to hold an item, a concept's <c>rules</c> or a weekly recurrence's
    /// <c>days</c>, holding none, which the loader reports with <c>BODU-CAL-SCHEMA</c>, is rejected with a
    /// <see cref="FormatException" /> carrying the loader's message.
    /// </summary>
    /// <param name="field">The name of the list emptied in the document.</param>
    [TestMethod]
    [DataRow("notableDate.rules")]
    [DataRow("weekly.days")]
    public void FromJson_WhenSchemaRequiredListIsEmpty_ShouldThrowLoadersMessage(string field)
    {
        AssertThrowsLoaderSchemaMessage(SchemaFieldJson(field, "[]"));
    }

    /// <summary>
    /// Verifies that a JSON integer outside the range the schema allows for it, which the loader reports with
    /// <c>BODU-CAL-SCHEMA</c>, is rejected with a <see cref="FormatException" /> carrying the loader's message, rather
    /// than written into the document for the schema to reject at <c>Build</c>.
    /// </summary>
    /// <param name="field">The name of the integer.</param>
    /// <param name="literal">The integer, as a JSON literal.</param>
    [TestMethod]
    [DataRow("scope.fromYear", "-1")]
    [DataRow("scope.toYear", "65536")]
    [DataRow("scope.onlyYears.item", "-5")]
    [DataRow("scope.exceptYears.item", "70000")]
    [DataRow("trigger.day", "0")]
    [DataRow("trigger.day", "32")]
    [DataRow("action.maxSearchDays", "0")]
    [DataRow("notableDate.defaultDurationDays", "0")]
    [DataRow("rule.durationDays", "0")]
    [DataRow("rule.durationDays", "-3")]
    [DataRow("applicability.fromYear", "-1")]
    [DataRow("applicability.toYear", "65536")]
    [DataRow("applicability.everyYears", "0")]
    [DataRow("applicability.anchorYear", "65536")]
    [DataRow("applicability.onlyYears.item", "-5")]
    [DataRow("applicability.exceptYears.item", "65536")]
    [DataRow("fixed.day", "0")]
    [DataRow("fixed.day", "32")]
    [DataRow("weekdayNearDate.day", "0")]
    [DataRow("weekdayNearDate.day", "40")]
    [DataRow("nthWeekdayFromRule.ordinal", "0")]
    [DataRow("nthWeekdayFromRule.ordinal", "367")]
    [DataRow("nthWeekdayFromRule.ordinal", "\"+5\"")]
    [DataRow("nthWeekdayFromRule.ordinal", "\"05\"")]
    [DataRow("workingDayInMonth.ordinal", "0")]
    [DataRow("ordinalDayOfMonth.ordinal", "-367")]
    [DataRow("dayOfYear.ordinal", "367")]
    [DataRow("isoWeekDate.week", "0")]
    [DataRow("isoWeekDate.week", "54")]
    [DataRow("untilDate.fixed.day", "0")]
    [DataRow("dailyInterval.intervalDays", "0")]
    [DataRow("weekly.intervalWeeks", "0")]
    [DataRow("monthlyDay.dayOfMonth", "0")]
    [DataRow("monthlyDay.dayOfMonth", "32")]
    [DataRow("monthlyDay.intervalMonths", "0")]
    [DataRow("monthlyWeekday.intervalMonths", "-1")]
    [DataRow("patch.durationDays", "0")]
    [DataRow("add.fixed.day", "32")]
    public void FromJson_WhenIntegerIsOutsideSchemaRange_ShouldThrowLoadersMessage(string field, string literal)
    {
        AssertThrowsLoaderSchemaMessage(SchemaFieldJson(field, literal));
    }

    /// <summary>
    /// Verifies that a JSON integer at a bound of the range the schema allows for it builds, through the builder, into
    /// the same resource the loader loads, compared byte for byte as binary rule packs.
    /// </summary>
    /// <param name="field">The name of the integer.</param>
    /// <param name="literal">The integer, as a JSON literal.</param>
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
    public void FromJson_WhenIntegerIsAtSchemaRangeBound_ShouldBuildWhatLoadJsonLoads(string field, string literal)
    {
        string json = SchemaFieldJson(field, literal);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, BuilderEnumImportResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(BuilderEnumImportResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built), field + " " + literal);
    }

    /// <summary>
    /// The well-formed value of every identifier, reference and territory code the JSON document under the pattern tests
    /// holds, keyed by the placeholder that stands for it.
    /// </summary>
    private static readonly Dictionary<string, string> s_jsonIdentifierFieldValues = new(StringComparer.Ordinal)
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
        ["weekdayNearRule.notableDateRef"] = "anchor",
        ["weekdayNearRule.ruleRef"] = "r",
        ["nthWeekdayFromRule.notableDateRef"] = "anchor",
        ["nthWeekdayFromRule.ruleRef"] = "r",
        ["workingDayOffsetFromRule.notableDateRef"] = "anchor",
        ["workingDayOffsetFromRule.ruleRef"] = "r",
        ["untilDate.notableDateRef"] = "anchor",
        ["untilDate.ruleRef"] = "r",
        ["patch.notableDateRef"] = "anchor",
        ["patch.ruleRef"] = "r",
        ["patch.territories"] = "NZ",
        ["patch.adjustments"] = "policy",
        ["remove.notableDateRef"] = "offset",
        ["remove.ruleRef"] = "gone",
        ["add.notableDateRef"] = "offset",
        ["add.rule.id"] = "added",
    };

    /// <summary>
    /// Verifies that the pattern test document builds, through the builder, into the same resource the loader loads,
    /// compared byte for byte as binary rule packs, so each rejection the per-field tests observe is caused by the one
    /// value they replace.
    /// </summary>
    [TestMethod]
    public void FromJson_WhenEveryIdentifierMatchesSchemaPattern_ShouldBuildWhatLoadJsonLoads()
    {
        string json = IdentifierFieldJson(null, null);

        NotableDateResource loaded = NotableDateResourceLoader.LoadJson(json, BuilderEnumImportResolver);
        NotableDateResource built = NotableDateDocumentBuilder.FromJson(json).Build(BuilderEnumImportResolver);

        CollectionAssert.AreEqual(PackBytes(loaded), PackBytes(built));
    }

    /// <summary>
    /// Verifies that a JSON identifier, reference or territory code that breaks the pattern the schema requires of it,
    /// which the loader rejects with <c>BODU-CAL-SCHEMA</c>, is rejected with a <see cref="FormatException" /> carrying
    /// the loader's message, rather than read and left for <c>Build</c> to reject.
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
    [DataRow("weekdayNearRule.notableDateRef", "anchor_day")]
    [DataRow("weekdayNearRule.ruleRef", "R")]
    [DataRow("nthWeekdayFromRule.notableDateRef", "1anchor")]
    [DataRow("nthWeekdayFromRule.ruleRef", "r-")]
    [DataRow("workingDayOffsetFromRule.notableDateRef", "Anchor")]
    [DataRow("workingDayOffsetFromRule.ruleRef", "r 1")]
    [DataRow("untilDate.notableDateRef", "ANCHOR")]
    [DataRow("untilDate.ruleRef", "r.1")]
    [DataRow("patch.notableDateRef", "Anchor")]
    [DataRow("patch.ruleRef", "R")]
    [DataRow("patch.territories", "nz")]
    [DataRow("patch.adjustments", "Policy")]
    [DataRow("remove.notableDateRef", "Offset")]
    [DataRow("remove.ruleRef", "Gone")]
    [DataRow("add.notableDateRef", "Offset")]
    [DataRow("add.rule.id", "Added")]
    public void FromJson_WhenIdentifierBreaksSchemaPattern_ShouldThrowLoadersMessage(string field, string value)
    {
        AssertThrowsLoaderSchemaMessage(IdentifierFieldJson(field, value));
    }

    /// <summary>
    /// Builds a JSON document holding every identifier, reference and territory code the loader holds to a pattern, each
    /// set to its well-formed value except the one named, which holds the supplied value.
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
                    { "id": "near", "strategy": { "weekdayNearRule": { "notableDateRef": "@weekdayNearRule.notableDateRef@",
                      "ruleRef": "@weekdayNearRule.ruleRef@", "dayOfWeek": "Friday", "direction": "Before" } } },
                    { "id": "nth", "strategy": { "nthWeekdayFromRule": { "notableDateRef": "@nthWeekdayFromRule.notableDateRef@",
                      "ruleRef": "@nthWeekdayFromRule.ruleRef@", "dayOfWeek": "Sunday", "ordinal": 2 } } },
                    { "id": "working", "strategy": { "workingDayOffsetFromRule": { "notableDateRef": "@workingDayOffsetFromRule.notableDateRef@",
                      "ruleRef": "@workingDayOffsetFromRule.ruleRef@", "offsetWorkingDays": 1 } } },
                    { "id": "gone", "strategy": { "fixed": { "month": 5, "day": 1 } } } ] } ],
              "overrides": [
                { "operation": "PatchRule", "notableDateRef": "@patch.notableDateRef@", "ruleRef": "@patch.ruleRef@", "priority": 2,
                  "applicability": { "territories": [ "@patch.territories@" ] }, "adjustments": [ "@patch.adjustments@" ] },
                { "operation": "RemoveRule", "notableDateRef": "@remove.notableDateRef@", "ruleRef": "@remove.ruleRef@" },
                { "operation": "AddRule", "notableDateRef": "@add.notableDateRef@",
                  "rule": { "id": "@add.rule.id@", "strategy": { "fixed": { "month": 6, "day": 1 } } } } ] }
            """;

        foreach ((string key, string wellFormed) in s_jsonIdentifierFieldValues)
            json = json.Replace("@" + key + "@", key == field ? value : wellFormed, StringComparison.Ordinal);

        return json;
    }

    /// <summary>
    /// Asserts that the builder rejects a JSON document with a <see cref="FormatException" /> carrying the message of the
    /// first <c>BODU-CAL-SCHEMA</c> error the loader reports for it.
    /// </summary>
    /// <param name="json">The JSON document.</param>
    private static void AssertThrowsLoaderSchemaMessage(string json)
    {
        _ = NotableDateResourceLoader.TryLoadJson(json, BuilderEnumImportResolver, out _, out IReadOnlyList<NotableDateValidationDiagnostic> diagnostics);
        NotableDateValidationDiagnostic? expected = diagnostics.FirstOrDefault(d => d.Code == "BODU-CAL-SCHEMA" && d.Severity == NotableDateValidationSeverity.Error);
        Assert.IsNotNull(expected, "The loader reported no BODU-CAL-SCHEMA error.");

        FormatException ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateDocumentBuilder.FromJson(json);
        });

        Assert.AreEqual(expected.Message, ex.Message);
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

        string[] segments = s_schemaFieldPointers[field].Split('/', StringSplitOptions.RemoveEmptyEntries);
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
}
