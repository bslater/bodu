// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.FromJson.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
