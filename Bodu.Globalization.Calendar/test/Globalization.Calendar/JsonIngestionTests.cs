// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonIngestionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that <see cref="NotableDateResourceLoader.LoadJson(string)" /> ingests the JSON representation of a
/// notable-date document, producing the same resolution behaviour as the XML form across fixed dates, nth-weekday and
/// offset strategies, an algorithm, a non-Gregorian calendar, an adjustment policy, and an override.
/// </summary>
[TestClass]
public sealed class JsonIngestionTests
{
    /// <summary>
    /// A representative notable-date document expressed in JSON.
    /// </summary>
    private const string Json = """
    {
      "schemaVersion": "1.0",
      "resourceId": "data.json",
      "resolutionPolicy": { "duplicatePolicy": "Error", "priorityDirection": "HigherWins" },
      "adjustmentPolicies": [
        {
          "id": "sunday-to-monday",
          "priority": 100,
          "trigger": { "type": "IfDayOfWeek", "weekdays": [ "Sunday" ] },
          "action": { "type": "MoveToNextWeekday", "dayOfWeek": "Monday" },
          "emission": { "mode": "ObservedOnly", "reason": "Observed Monday" }
        }
      ],
      "notableDates": [
        {
          "id": "new-years-day", "displayName": "New Year's Day", "category": "PublicHoliday", "defaultNonWorkingDay": true,
          "rules": [ { "id": "us", "strategy": { "fixed": { "month": 1, "day": 1 } }, "adjustments": [ "sunday-to-monday" ] } ]
        },
        {
          "id": "mlk-day", "displayName": "Martin Luther King, Jr. Day", "category": "PublicHoliday", "defaultNonWorkingDay": true,
          "rules": [ { "id": "us", "strategy": { "dayOfWeekInMonth": { "month": "January", "dayOfWeek": "Monday", "weekOrdinal": "Third" } } } ]
        },
        {
          "id": "easter-sunday", "displayName": "Easter Sunday", "category": "Religious", "defaultNonWorkingDay": false,
          "rules": [ { "id": "us", "applicability": { "fromYear": 1583 }, "strategy": { "algorithm": { "key": "western-easter" } } } ]
        },
        {
          "id": "good-friday", "displayName": "Good Friday", "category": "Religious", "defaultNonWorkingDay": false,
          "rules": [ { "id": "us", "applicability": { "fromYear": 1583 }, "strategy": { "offsetFromRule": { "notableDateRef": "easter-sunday", "ruleRef": "us", "offsetDays": -2 } } } ]
        },
        {
          "id": "islamic-new-year", "displayName": "Islamic New Year", "category": "Religious", "defaultNonWorkingDay": true,
          "rules": [ { "id": "sa", "applicability": { "calendar": "Hijri" }, "strategy": { "fixed": { "month": 1, "day": 1, "sweepCalendarYears": true } } } ]
        },
        {
          "id": "constitution-day", "displayName": "Constitution Day", "category": "Observance", "defaultNonWorkingDay": false,
          "rules": [ { "id": "pr", "strategy": { "fixed": { "month": 7, "day": 25 } } } ]
        }
      ],
      "overrides": [
        { "operation": "RemoveRule", "notableDateRef": "constitution-day", "ruleRef": "pr" }
      ]
    }
    """;

    /// <summary>
    /// Builds a service over the JSON document.
    /// </summary>
    /// <returns>A service for the JSON document.</returns>
    private static NotableDateService CreateService() =>
        new(NotableDateResourceLoader.LoadJson(Json));

    /// <summary>
    /// Resolves the single occurrence with the supplied identifier for a year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The matching occurrence, or <see langword="null" /> when none.</returns>
    private static NotableDate? Single(int year, string notableDateId) =>
        CreateService()
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), "XX")
            .FirstOrDefault(r => r.NotableDateId == notableDateId);

    /// <summary>
    /// Verifies that each JSON strategy resolves to its known date.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expected">The expected emitted date in ISO format.</param>
    [TestMethod]
    [DataRow(2023, "new-years-day", "2023-01-02")]
    [DataRow(2026, "mlk-day", "2026-01-19")]
    [DataRow(2024, "easter-sunday", "2024-03-31")]
    [DataRow(2024, "good-friday", "2024-03-29")]
    [DataRow(2024, "islamic-new-year", "2024-07-07")]
    public void LoadJson_Strategy_ResolvesToKnownAnswer(int year, string notableDateId, string expected)
    {
        NotableDate? match = Single(year, notableDateId);

        Assert.IsNotNull(match);
        Assert.AreEqual(DateOnly.Parse(expected, CultureInfo.InvariantCulture), match.Date);
    }

    /// <summary>
    /// Verifies that the New Year's Day Sunday substitute is emitted as an observed occurrence.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenAdjustmentFires_EmitsObservedOccurrence()
    {
        NotableDate? match = Single(2023, "new-years-day");

        Assert.IsNotNull(match);
        Assert.AreEqual(
            (true, (DateOnly?)new DateOnly(2023, 1, 1)),
            (match.IsObserved, match.ActualDate));
    }

    /// <summary>
    /// Verifies that the JSON override removes the targeted rule, so Constitution Day no longer resolves.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenRemoveRuleOverride_DropsTheTargetedRule()
    {
        Assert.IsNull(Single(2024, "constitution-day"));
    }

    /// <summary>
    /// Verifies that malformed JSON throws a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void LoadJson_WhenJsonIsMalformed_ThrowsFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = NotableDateResourceLoader.LoadJson("{ not valid json ");
        });
    }
}
