// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmKnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" /> data-source and
/// display-name helpers for every <see cref="INotableDateAlgorithm" /> known-answer test in the calendar test suite.
/// One method per algorithm exposes the full known-answer table; a matching <c>*Smoke</c> method exposes the 1-5
/// representative rows that stay in the default BVT run.
/// </summary>
/// <remarks>
/// <para>
/// Wire methods up with
/// <c>[DynamicData(nameof(NotableDateAlgorithmKnownAnswers.Vesak),typeof(NotableDateAlgorithmKnownAnswers),DynamicDataDisplayName = nameof(NotableDateAlgorithmKnownAnswers.GetDisplayName),DynamicDataDisplayNameDeclaringType = typeof(NotableDateAlgorithmKnownAnswers))]</c>
/// . MSTest 4.x requires <c>DynamicDataDisplayNameDeclaringType</c> to be set explicitly because the
/// constructor-supplied <see cref="Type" /> argument is used only for the data-source method.
/// </para>
/// <para>
/// Full datasets that exceed the BVT row-count budget (rule of thumb: more than ~5 rows) are routed through a
/// <c>[TestCategory("Regression")]</c> test method so they do not bloat the default build run. The smaller
/// <c>*Smoke</c> providers stay uncategorised and execute on every BVT build.
/// </para>
/// </remarks>
public static partial class NotableDateAlgorithmKnownAnswers
{
    // -----------------------------------------------------------------------------------------------------------
    // Algorithm-factory enrollment (drives the shared boundary contract tests).
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Enumerates one <see cref="AlgorithmFactoryCase" /> row per shipped <see cref="INotableDateAlgorithm" />
    /// implementation. Drives the boundary-contract tests in <c>NotableDateAlgorithmContractTests</c> across every
    /// algorithm from a single set of <c>[DataTestMethod]</c> declarations.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is an <see cref="AlgorithmFactoryCase" />.
    /// </returns>
    public static IEnumerable<object[]> AllAlgorithmFactories()
    {
        yield return Factory("Easter", () => new EasterSundayNotableDateAlgorithm());
        yield return Factory("Losar", () => new LosarNotableDateAlgorithm());
        yield return Factory("Vesak", () => new VesakNotableDateAlgorithm());
        yield return Factory("AsalhaPuja", () => new AsalhaPujaNotableDateAlgorithm());
        yield return Factory("Qingming", () => new QingmingNotableDateAlgorithm());
        yield return Factory(
            "HinduLunar",
            () => new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15));
    }

    // -----------------------------------------------------------------------------------------------------------
    // Losar (Tibetan New Year) — lunar approximation, +/- 1 day tolerance.
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Provides the smoke subset of Losar known-answer rows that runs on every BVT build. One row per representative
    /// year where the algorithm is known to agree with the official Tibetan lunisolar calendar within a one-day
    /// tolerance.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> LosarSmoke()
    {
        yield return LosarRow(2024, new DateTime(2024, 2, 10));
    }

    /// <summary>
    /// Provides the full Losar known-answer table. Each row identifies a year where the lunar-approximation algorithm
    /// is expected to fall within +/- one day of the official Tibetan New Year date; years where the Tibetan calendar
    /// diverges by an intercalary month are deliberately excluded.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> Losar()
    {
        yield return LosarRow(2021, new DateTime(2021, 2, 12));
        yield return LosarRow(2024, new DateTime(2024, 2, 10));
    }

    // -----------------------------------------------------------------------------------------------------------
    // Vesak (Visakha Bucha) — exact match against published Thai dates.
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Provides the smoke subset of Vesak known-answer rows. One representative year matched against the Thai Visakha
    /// Bucha public holiday.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> VesakSmoke()
    {
        yield return VesakRow(2024, new DateTime(2024, 5, 23));
    }

    /// <summary>
    /// Provides the full Vesak known-answer table. Each row matches the Thai Visakha Bucha public holiday — the first
    /// full moon on or after 1 May.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> Vesak()
    {
        yield return VesakRow(2022, new DateTime(2022, 5, 16));
        yield return VesakRow(2023, new DateTime(2023, 5, 5));
        yield return VesakRow(2024, new DateTime(2024, 5, 23));
        yield return VesakRow(2025, new DateTime(2025, 5, 12));
    }

    // -----------------------------------------------------------------------------------------------------------
    // Asalha Puja (Asanha Bucha) — exact match against published Thai dates.
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Provides the smoke subset of Asalha Puja known-answer rows. One representative year matched against the Thai
    /// Asanha Bucha public holiday.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> AsalhaPujaSmoke()
    {
        yield return AsalhaPujaRow(2025, new DateTime(2025, 7, 10));
    }

    /// <summary>
    /// Provides the full Asalha Puja known-answer table. Each row matches the Thai Asanha Bucha public holiday for
    /// years where the algorithm's lunation matches the official observance; Thai intercalary-month years (2023, 2024)
    /// are excluded because the official observance is moved to the following lunation.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> AsalhaPuja()
    {
        yield return AsalhaPujaRow(2022, new DateTime(2022, 7, 13));
        yield return AsalhaPujaRow(2025, new DateTime(2025, 7, 10));
    }

    // -----------------------------------------------------------------------------------------------------------
    // Qingming — solar-term approximation, +/- 1 day tolerance.
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Provides the smoke subset of Qingming known-answer rows. One representative year matched against the published
    /// astronomical date within a one-day tolerance.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> QingmingSmoke()
    {
        yield return QingmingRow(2024, new DateTime(2024, 4, 4));
    }

    /// <summary>
    /// Provides the full Qingming known-answer table. Each row matches the published astronomical date within a one-day
    /// tolerance — the algorithm computes solar-term transitions in Universal Time and may differ from the East Asian
    /// (CST) calendar date when the transition falls near local midnight.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> Qingming()
    {
        yield return QingmingRow(2020, new DateTime(2020, 4, 4));
        yield return QingmingRow(2021, new DateTime(2021, 4, 5));
        yield return QingmingRow(2022, new DateTime(2022, 4, 5));
        yield return QingmingRow(2023, new DateTime(2023, 4, 5));
        yield return QingmingRow(2024, new DateTime(2024, 4, 4));
        yield return QingmingRow(2025, new DateTime(2025, 4, 4));
        yield return QingmingRow(2026, new DateTime(2026, 4, 5));
    }

    // -----------------------------------------------------------------------------------------------------------
    // Hindu lunar festivals — Diwali, Holi, Navaratri. Approximate tithi, +/- 2 day tolerance.
    // Each festival uses a distinct (HinduLunarMonth, HinduPaksha, tithi) triple, so each gets its own factory.
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Provides the smoke subset of Diwali (Amavasya of Kartik) known-answer rows. One representative year matched
    /// against published panchanga dates within a two-day tolerance.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> HinduLunarDiwaliSmoke()
    {
        yield return HinduLunarDiwaliRow(2024, new DateTime(2024, 11, 1));
    }

    /// <summary>
    /// Provides the full Diwali known-answer table. Each row matches the Indian panchanga Amavasya of Kartik for years
    /// without an intercalary Kartik month — 2022 and 2023 are excluded because the intercalary month leads the
    /// algorithm to the wrong lunation.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> HinduLunarDiwali()
    {
        yield return HinduLunarDiwaliRow(2024, new DateTime(2024, 11, 1));
    }

    /// <summary>
    /// Provides the smoke subset of Holi (Purnima of Phalguna) known-answer rows. One representative year matched
    /// against the published panchanga date within a two-day tolerance.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> HinduLunarHoliSmoke()
    {
        yield return HinduLunarHoliRow(2023, new DateTime(2023, 3, 7));
    }

    /// <summary>
    /// Provides the full Holi known-answer table. Each row matches the Indian panchanga Purnima of Phalguna for years
    /// without an intercalary Phalguna month — 2022 and 2024 are excluded because the intercalary month leads the
    /// algorithm to the wrong lunation.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> HinduLunarHoli()
    {
        yield return HinduLunarHoliRow(2023, new DateTime(2023, 3, 7));
    }

    /// <summary>
    /// Provides the smoke subset of Navaratri start (Pratipada of Ashvin) known-answer rows. One representative year
    /// matched against the published panchanga date within a two-day tolerance.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> HinduLunarNavaratriSmoke()
    {
        yield return HinduLunarNavaratriRow(2022, new DateTime(2022, 9, 26));
    }

    /// <summary>
    /// Provides the full Navaratri start known-answer table. Each row matches the Indian panchanga Pratipada of Ashvin
    /// for years without an intercalary Ashvin month — 2023 and 2024 are excluded because the intercalary month leads
    /// the algorithm to the wrong lunation.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays whose only entry is a <see cref="NotableDateAlgorithmKnownAnswer" />.
    /// </returns>
    public static IEnumerable<object[]> HinduLunarNavaratri()
    {
        yield return HinduLunarNavaratriRow(2022, new DateTime(2022, 9, 26));
    }

    // -----------------------------------------------------------------------------------------------------------
    // Display name + row construction helpers.
    // -----------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Renders a <see cref="NotableDateAlgorithmKnownAnswer" /> or <see cref="AlgorithmFactoryCase" /> row as the
    /// MSTest display name. Algorithm-known-answer rows render as
    /// <c>"Losar | 2024 | Default -&gt; 2024-02-10 [+/- 1d]"</c>; factory rows render as just the algorithm label, for
    /// example <c>"Losar"</c>.
    /// </summary>
    /// <param name="methodInfo">
    /// The test method under discovery. Unused; required by
    /// <see cref="Microsoft.VisualStudio.TestTools.UnitTesting.DynamicDataAttribute" />.
    /// </param>
    /// <param name="data">The single-element row produced by one of the provider methods.</param>
    /// <returns>An ASCII display name no longer than ~100 characters.</returns>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data)
    {
        _ = methodInfo;

        return data[0] switch
        {
            NotableDateAlgorithmKnownAnswer ka => FormatKnownAnswer(ka),
            AlgorithmFactoryCase fc => fc.Algorithm,
            _ => string.Join(", ", data),
        };
    }

    /// <summary>
    /// Builds a single-element object array wrapping a <see cref="AlgorithmFactoryCase" /> for the
    /// <see cref="AllAlgorithmFactories" /> enrollment.
    /// </summary>
    /// <param name="algorithm">The algorithm label rendered in row display names.</param>
    /// <param name="factory">The delegate that constructs a fresh algorithm instance.</param>
    /// <returns>A single-element object array carrying the constructed case.</returns>
    private static object[] Factory(string algorithm, Func<INotableDateAlgorithm> factory) =>
        [
            new AlgorithmFactoryCase { Algorithm = algorithm, Factory = factory },
        ];

    /// <summary>
    /// Builds a single-element object array wrapping a <see cref="NotableDateAlgorithmKnownAnswer" /> row.
    /// </summary>
    /// <param name="algorithm">The algorithm label.</param>
    /// <param name="factory">The factory that constructs the algorithm instance.</param>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The expected result date.</param>
    /// <param name="toleranceDays">The symmetric tolerance window in days; <c>0</c> requires an exact match.</param>
    /// <param name="calendarKind">The calendar encoding for the row.</param>
    /// <param name="source">Optional provenance citation appended to the display name.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] Row(
        string algorithm,
        Func<INotableDateAlgorithm> factory,
        int year,
        DateTime expectedDate,
        int toleranceDays = 0,
        AlgorithmCalendarKind calendarKind = AlgorithmCalendarKind.Default,
        string? source = null) =>
        [
            new NotableDateAlgorithmKnownAnswer
            {
                Algorithm = algorithm,
                Factory = factory,
                Year = year,
                ExpectedDate = expectedDate,
                CalendarKind = calendarKind,
                ToleranceDays = toleranceDays,
                Source = source,
            },
        ];

    /// <summary>
    /// Builds a Losar row (one-day tolerance).
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published Tibetan New Year date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] LosarRow(int year, DateTime expectedDate) =>
        Row("Losar", () => new LosarNotableDateAlgorithm(), year, expectedDate, toleranceDays: 1);

    /// <summary>
    /// Builds a Vesak row (exact match).
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published Thai Visakha Bucha date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] VesakRow(int year, DateTime expectedDate) =>
        Row("Vesak", () => new VesakNotableDateAlgorithm(), year, expectedDate);

    /// <summary>
    /// Builds an Asalha Puja row (exact match).
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published Thai Asanha Bucha date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] AsalhaPujaRow(int year, DateTime expectedDate) =>
        Row("AsalhaPuja", () => new AsalhaPujaNotableDateAlgorithm(), year, expectedDate);

    /// <summary>
    /// Builds a Qingming row (one-day tolerance).
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published astronomical Qingming date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] QingmingRow(int year, DateTime expectedDate) =>
        Row("Qingming", () => new QingmingNotableDateAlgorithm(), year, expectedDate, toleranceDays: 1);

    /// <summary>
    /// Builds a Hindu lunar Diwali row (Amavasya of Kartik, two-day tolerance).
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published panchanga Diwali date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] HinduLunarDiwaliRow(int year, DateTime expectedDate) =>
        Row(
            "HinduLunar(Diwali)",
            () => new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Kartik, HinduPaksha.Krishna, 15),
            year,
            expectedDate,
            toleranceDays: 2);

    /// <summary>
    /// Builds a Hindu lunar Holi row (Purnima of Phalguna, two-day tolerance).
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published panchanga Holi date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] HinduLunarHoliRow(int year, DateTime expectedDate) =>
        Row(
            "HinduLunar(Holi)",
            () => new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Phalguna, HinduPaksha.Shukla, 15),
            year,
            expectedDate,
            toleranceDays: 2);

    /// <summary>
    /// Builds a Hindu lunar Navaratri row (Pratipada of Ashvin, two-day tolerance).
    /// </summary>
    /// <param name="year">The year passed to the algorithm.</param>
    /// <param name="expectedDate">The published panchanga Navaratri start date for <paramref name="year" />.</param>
    /// <returns>A single-element object array carrying the constructed row.</returns>
    private static object[] HinduLunarNavaratriRow(int year, DateTime expectedDate) =>
        Row(
            "HinduLunar(Navaratri)",
            () => new HinduLunarNotableDateAlgorithm(HinduLunarMonth.Ashvin, HinduPaksha.Shukla, 1),
            year,
            expectedDate,
            toleranceDays: 2);

    /// <summary>
    /// Formats a <see cref="NotableDateAlgorithmKnownAnswer" /> row as a pipe-separated, ASCII-only display name.
    /// Suffixes the tolerance window when non-zero and the provenance source when populated.
    /// </summary>
    /// <param name="ka">The row to format.</param>
    /// <returns>The rendered display name.</returns>
    private static string FormatKnownAnswer(NotableDateAlgorithmKnownAnswer ka)
    {
        var calendar = ka.CalendarKind switch
        {
            AlgorithmCalendarKind.Gregorian => "Gregorian",
            AlgorithmCalendarKind.Julian => "Julian",
            _ => "Default",
        };

        var tolerance = ka.ToleranceDays > 0 ? $" [+/- {ka.ToleranceDays}d]" : string.Empty;
        var source = !string.IsNullOrEmpty(ka.Source) ? $" [{ka.Source}]" : string.Empty;

        return $"{ka.Algorithm} | {ka.Year} | {calendar} -> {ka.ExpectedDate:yyyy-MM-dd}{tolerance}{source}";
    }
}
