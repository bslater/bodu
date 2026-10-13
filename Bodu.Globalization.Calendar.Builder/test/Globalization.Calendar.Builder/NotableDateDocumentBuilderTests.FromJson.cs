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
}
