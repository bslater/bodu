// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalJewishCatalogueKnownAnswerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the year-by-year known-answer table for the Hebrew observances authored in the bundled <c>global-jewish</c>
/// catalogue, including the observances added in the latest pass (Tu BiShvat, Tisha B'Av, Lag BaOmer, Shemini Atzeret
/// and both Simchat Torah variants). The anchors are <see cref="FixedDateStrategy" /> rules in the
/// <see cref="CalendarSystem.Hebrew" /> calendar with a calendar-year sweep; the remaining observances are invariant
/// <see cref="OffsetFromRuleStrategy" /> offsets from those anchors. Expected Gregorian dates are the published Hebcal
/// dates (<see href="https://www.hebcal.com" />), whose arithmetic matches the base class library
/// <see cref="System.Globalization.HebrewCalendar" />.
/// </summary>
[TestClass]
public sealed class GlobalJewishCatalogueKnownAnswerTests
{
    /// <summary>
    /// Builds a service over the bundled <c>global-jewish</c> catalogue.
    /// </summary>
    /// <returns>A service for the catalogue.</returns>
    private static NotableDateService CreateService() =>
        CommonCatalogues.Service("global-jewish");

    /// <summary>
    /// Verifies that every observance declared in <c>global-jewish</c> resolves for a representative Gregorian year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_WhenLoadingJewish_ResolvesEveryObservance()
    {
        NotableDateService service = CreateService();

        var ids = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), CommonCatalogues.NeutralTerritory)
            .Select(r => r.NotableDateId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in new[]
        {
            "tu-bishvat", "purim", "passover", "lag-baomer", "shavuot", "tisha-bav", "rosh-hashanah", "yom-kippur",
            "sukkot", "shemini-atzeret", "simchat-torah-israel", "simchat-torah-diaspora", "hanukkah",
        })
        {
            Assert.Contains(id, ids);
        }
    }

    /// <summary>
    /// Verifies that each Hebrew observance resolves to the published Hebcal date across Gregorian years 2023-2026.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="month">The expected Gregorian month.</param>
    /// <param name="day">The expected Gregorian day.</param>
    [TestMethod]
    [TestCategory("Regression")]

    // 2023 (Hebrew 5783-5784)
    [DataRow(2023, "tu-bishvat", 2, 6)]
    [DataRow(2023, "purim", 3, 7)]
    [DataRow(2023, "passover", 4, 6)]
    [DataRow(2023, "lag-baomer", 5, 9)]          // Passover + 33
    [DataRow(2023, "shavuot", 5, 26)]            // Passover + 50
    [DataRow(2023, "tisha-bav", 7, 27)]
    [DataRow(2023, "rosh-hashanah", 9, 16)]
    [DataRow(2023, "yom-kippur", 9, 25)]         // Rosh Hashanah + 9
    [DataRow(2023, "sukkot", 9, 30)]             // Rosh Hashanah + 14
    [DataRow(2023, "shemini-atzeret", 10, 7)]    // Rosh Hashanah + 21
    [DataRow(2023, "simchat-torah-israel", 10, 7)]
    [DataRow(2023, "simchat-torah-diaspora", 10, 8)]
    [DataRow(2023, "hanukkah", 12, 8)]

    // 2024 (Hebrew 5784-5785)
    [DataRow(2024, "tu-bishvat", 1, 25)]
    [DataRow(2024, "purim", 3, 24)]
    [DataRow(2024, "passover", 4, 23)]
    [DataRow(2024, "lag-baomer", 5, 26)]
    [DataRow(2024, "shavuot", 6, 12)]
    [DataRow(2024, "tisha-bav", 8, 13)]
    [DataRow(2024, "rosh-hashanah", 10, 3)]
    [DataRow(2024, "yom-kippur", 10, 12)]
    [DataRow(2024, "sukkot", 10, 17)]
    [DataRow(2024, "shemini-atzeret", 10, 24)]
    [DataRow(2024, "simchat-torah-israel", 10, 24)]
    [DataRow(2024, "simchat-torah-diaspora", 10, 25)]
    [DataRow(2024, "hanukkah", 12, 26)]

    // 2025 (Hebrew 5785-5786)
    [DataRow(2025, "tu-bishvat", 2, 13)]
    [DataRow(2025, "purim", 3, 14)]
    [DataRow(2025, "passover", 4, 13)]
    [DataRow(2025, "lag-baomer", 5, 16)]
    [DataRow(2025, "shavuot", 6, 2)]
    [DataRow(2025, "tisha-bav", 8, 3)]
    [DataRow(2025, "rosh-hashanah", 9, 23)]
    [DataRow(2025, "yom-kippur", 10, 2)]
    [DataRow(2025, "sukkot", 10, 7)]
    [DataRow(2025, "shemini-atzeret", 10, 14)]
    [DataRow(2025, "simchat-torah-israel", 10, 14)]
    [DataRow(2025, "simchat-torah-diaspora", 10, 15)]
    [DataRow(2025, "hanukkah", 12, 15)]

    // 2026 (Hebrew 5786-5787)
    [DataRow(2026, "tu-bishvat", 2, 2)]
    [DataRow(2026, "purim", 3, 3)]
    [DataRow(2026, "passover", 4, 2)]
    [DataRow(2026, "lag-baomer", 5, 5)]
    [DataRow(2026, "shavuot", 5, 22)]
    [DataRow(2026, "tisha-bav", 7, 23)]
    [DataRow(2026, "rosh-hashanah", 9, 12)]
    [DataRow(2026, "yom-kippur", 9, 21)]
    [DataRow(2026, "sukkot", 9, 26)]
    [DataRow(2026, "shemini-atzeret", 10, 3)]
    [DataRow(2026, "simchat-torah-israel", 10, 3)]
    [DataRow(2026, "simchat-torah-diaspora", 10, 4)]
    [DataRow(2026, "hanukkah", 12, 5)]
    public void Resolve_JewishObservance_MatchesHebcalReferenceDate(int year, string notableDateId, int month, int day)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, year);

        Assert.AreEqual(new DateOnly(year, month, day), observance.Date, $"{notableDateId} {year}");
    }

    /// <summary>
    /// Verifies that the multi-day Hebrew observances preserve their authored duration across the resolution pipeline.
    /// </summary>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="expectedDuration">The expected authored duration in days.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("rosh-hashanah", 2)]
    [DataRow("passover", 7)]
    [DataRow("sukkot", 7)]
    [DataRow("hanukkah", 8)]
    [DataRow("yom-kippur", 1)]
    [DataRow("shemini-atzeret", 1)]
    public void Resolve_JewishObservance_PreservesAuthoredDurationDays(string notableDateId, int expectedDuration)
    {
        NotableDate observance = CommonCatalogues.ResolveSingle(CreateService(), notableDateId, 2024);

        Assert.AreEqual(expectedDuration, observance.DurationDays, notableDateId);
    }
}
