// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CanadaTerritoryAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources for the
/// Canada rule catalogue shipped in the Americas data pack, focused on Victoria Day — the Monday immediately preceding
/// 25 May, modelled with the <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy (the Monday on or before
/// 24 May). Rows span multiple years to exercise the strategy's nuances, including the year in which 24 May is itself a
/// Monday (the on-the-day case).
/// </summary>
public static class CanadaTerritoryAnswers
{
    private static Func<INotableDateRuleProvider> CaProvider =>
        AmericasCalendarData.CreateCanadaProvider;

    /// <summary>
    /// Provides the Victoria Day occurrence expectations across a span of years.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="TerritoryNotableDateKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> VictoriaDayOccurrences()
    {
        // Victoria Day = the Monday on or before 24 May. 24 May 2021 is itself a Monday (the on-the-day case);
        // every other year resolves to the Monday earlier in the window.
        yield return Row(2021, new DateTime(2021, 5, 24), "24 May is itself a Monday");
        yield return Row(2022, new DateTime(2022, 5, 23));
        yield return Row(2023, new DateTime(2023, 5, 22));
        yield return Row(2024, new DateTime(2024, 5, 20));
        yield return Row(2025, new DateTime(2025, 5, 19));
        yield return Row(2026, new DateTime(2026, 5, 18));
    }

    private static object[] Row(int year, DateTime expectedDate, string? note = null) =>
        [
            new TerritoryNotableDateKnownAnswer
            {
                ProviderFactory = CaProvider,
                Territory = "CA",
                Year = year,
                Name = "Victoria Day",
                ExpectedDate = expectedDate,
                ExpectedDayOfWeek = DayOfWeek.Monday,
                Note = note,
            },
        ];
}
