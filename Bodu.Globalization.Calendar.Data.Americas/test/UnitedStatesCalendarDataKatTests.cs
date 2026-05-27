// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnitedStatesCalendarDataKatTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Drives <see cref="CalendarDataKat" /> rows against the public Americas calendar service surface,
/// asserting the actual date the bundled rule provider hands back for canonical (territory, calendar,
/// year, notable-date) queries. Bespoke override and cherry-pick coverage stays in
/// <see cref="UnitedStatesNotableDatesTests" />.
/// </summary>
[TestClass]
public sealed class UnitedStatesCalendarDataKatTests
{
    private static IReadOnlyList<CalendarDataKat> Kats { get; } =
    [
        // Independence Day 2025 is Friday — no weekend substitute applies.
        new(
            Name: "US Independence Day 2025 (Friday, no substitute)",
            Territory: "US",
            Calendar: "Gregorian",
            Year: 2025,
            NotableDateName: "Independence Day",
            ExpectedDate: new DateOnly(2025, 7, 4)),

        // Independence Day 2026 is Saturday — US data pack rolls to next non-working day
        // (Monday July 6 2026). Documents the weekend-substitute path.
        new(
            Name: "US Independence Day 2026 (Saturday → observed Monday)",
            Territory: "US",
            Calendar: "Gregorian",
            Year: 2026,
            NotableDateName: "Independence Day",
            ExpectedDate: new DateOnly(2026, 7, 6)),
    ];

    /// <summary>
    /// Verifies that each <see cref="CalendarDataKat" /> row drives the public US data provider to its
    /// expected actual date via <see cref="NotableDateService.GetNotableDates(int, string?, Type?)" />.
    /// Validates the Americas data package through the public calendar service rather than peeking at
    /// low-level provider internals.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenKatIsKnownForUnitedStates_ShouldYieldExpectedDate()
    {
        using NotableDateService service = new(
            [AmericasCalendarData.CreateUnitedStatesProvider()],
            WorkingDaysOfWeek.MondayToFriday);

        foreach (CalendarDataKat kat in Kats)
        {
            IReadOnlyList<NotableDate> results = service.GetNotableDates(kat.Year, kat.Territory);
            NotableDate? match = results.FirstOrDefault(r =>
                string.Equals(r.Name, kat.NotableDateName, StringComparison.Ordinal));

            Assert.IsNotNull(match, $"Data KAT '{kat.Name}': service returned no entry for '{kat.NotableDateName}'.");
            Assert.AreEqual(
                kat.ExpectedDate,
                DateOnly.FromDateTime(match.Date),
                $"Data KAT '{kat.Name}': actual date differs from expected.");
        }
    }
}
