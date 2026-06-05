// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayNearDateAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data sources that exercise
/// the <see cref="DateResolutionStrategy.WeekdayNearDate" /> strategy end to end through the Europe data pack: the
/// Finnish and Swedish Saturday-window holidays (Midsummer Day, All Saints' Day) and the German Repentance Day
/// (Buß- und Bettag, the Wednesday before 23 November in Saxony).
/// </summary>
public static class WeekdayNearDateAnswers
{
    private static Func<INotableDateRuleProvider> FiProvider =>
        EuropeCalendarData.CreateFinlandProvider;

    private static Func<INotableDateRuleProvider> SeProvider =>
        EuropeCalendarData.CreateSwedenProvider;

    private static Func<INotableDateRuleProvider> DeProvider =>
        EuropeCalendarData.CreateGermanyProvider;

    /// <summary>
    /// Provides the expected resolved occurrences of the weekday-near-date holidays for representative years.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="TerritoryNotableDateKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> Occurrences()
    {
        // Finland — Midsummer Day: the Saturday on or after 20 June.
        yield return Row(FiProvider, "FI", 2024, "Midsummer Day", new DateTime(2024, 6, 22), DayOfWeek.Saturday);
        yield return Row(FiProvider, "FI", 2025, "Midsummer Day", new DateTime(2025, 6, 21), DayOfWeek.Saturday);

        // Finland — All Saints' Day: the Saturday on or after 31 October.
        yield return Row(FiProvider, "FI", 2024, "All Saints' Day", new DateTime(2024, 11, 2), DayOfWeek.Saturday);
        yield return Row(FiProvider, "FI", 2025, "All Saints' Day", new DateTime(2025, 11, 1), DayOfWeek.Saturday);

        // Sweden — Midsummer Day and All Saints' Day share the same windows.
        yield return Row(SeProvider, "SE", 2024, "Midsummer Day", new DateTime(2024, 6, 22), DayOfWeek.Saturday);
        yield return Row(SeProvider, "SE", 2025, "All Saints' Day", new DateTime(2025, 11, 1), DayOfWeek.Saturday);

        // Germany (Saxony) — Repentance Day: the Wednesday on or before 22 November.
        yield return Row(DeProvider, "DE-SN", 2024, "Repentance Day", new DateTime(2024, 11, 20), DayOfWeek.Wednesday);
        yield return Row(DeProvider, "DE-SN", 2025, "Repentance Day", new DateTime(2025, 11, 19), DayOfWeek.Wednesday);

        // --- Additional years exercising the on-the-day and month-boundary nuances ---

        // Midsummer Day: 20 June is itself a Saturday in 2020 and 2026 (on-the-day), a Sunday in 2021
        // (rolls to the 26th), and a Tuesday in 2023 (rolls to the 24th).
        yield return Row(FiProvider, "FI", 2020, "Midsummer Day", new DateTime(2020, 6, 20), DayOfWeek.Saturday);
        yield return Row(SeProvider, "SE", 2020, "Midsummer Day", new DateTime(2020, 6, 20), DayOfWeek.Saturday);
        yield return Row(FiProvider, "FI", 2021, "Midsummer Day", new DateTime(2021, 6, 26), DayOfWeek.Saturday);
        yield return Row(FiProvider, "FI", 2023, "Midsummer Day", new DateTime(2023, 6, 24), DayOfWeek.Saturday);
        yield return Row(SeProvider, "SE", 2026, "Midsummer Day", new DateTime(2026, 6, 20), DayOfWeek.Saturday);

        // All Saints' Day: 31 October is itself a Saturday in 2020 and 2026 (on-the-day, stays in October);
        // otherwise the window rolls forward into November.
        yield return Row(FiProvider, "FI", 2020, "All Saints' Day", new DateTime(2020, 10, 31), DayOfWeek.Saturday);
        yield return Row(SeProvider, "SE", 2020, "All Saints' Day", new DateTime(2020, 10, 31), DayOfWeek.Saturday);
        yield return Row(FiProvider, "FI", 2023, "All Saints' Day", new DateTime(2023, 11, 4), DayOfWeek.Saturday);
        yield return Row(SeProvider, "SE", 2026, "All Saints' Day", new DateTime(2026, 10, 31), DayOfWeek.Saturday);

        // Repentance Day: 22 November is itself a Wednesday in 2023 (on-the-day); a Tuesday in 2022
        // (rolls back to the 16th); a Sunday in 2026 (rolls back to the 18th).
        yield return Row(DeProvider, "DE-SN", 2022, "Repentance Day", new DateTime(2022, 11, 16), DayOfWeek.Wednesday);
        yield return Row(DeProvider, "DE-SN", 2023, "Repentance Day", new DateTime(2023, 11, 22), DayOfWeek.Wednesday);
        yield return Row(DeProvider, "DE-SN", 2026, "Repentance Day", new DateTime(2026, 11, 18), DayOfWeek.Wednesday);
    }

    /// <summary>
    /// Builds a single <see cref="TerritoryNotableDateKnownAnswer" /> row wrapped in the object array shape required by
    /// <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" />.
    /// </summary>
    /// <param name="provider">The provider factory for the territory under test.</param>
    /// <param name="territory">The query territory.</param>
    /// <param name="year">The query year.</param>
    /// <param name="name">The expected holiday name.</param>
    /// <param name="expectedDate">The expected resolved date.</param>
    /// <param name="expectedDayOfWeek">The expected weekday of <paramref name="expectedDate" />.</param>
    /// <returns>A single-element object array carrying the populated known-answer row.</returns>
    private static object[] Row(
        Func<INotableDateRuleProvider> provider,
        string territory,
        int year,
        string name,
        DateTime expectedDate,
        DayOfWeek expectedDayOfWeek) =>
        [
            new TerritoryNotableDateKnownAnswer
            {
                ProviderFactory = provider,
                Territory = territory,
                Year = year,
                Name = name,
                ExpectedDate = expectedDate,
                ExpectedDayOfWeek = expectedDayOfWeek,
            },
        ];
}
