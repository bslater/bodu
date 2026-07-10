// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarDataTestsBase.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides shared scaffolding for the regional calendar data-bundle test classes — a common load-and-resolve
/// smoke test plus resolution and date-tolerance helpers — parameterized over a single region's data factory
/// through the <see cref="SupportedCountries" /> and <see cref="CreateService(string)" /> hooks.
/// </summary>
/// <remarks>
/// Each <c>&lt;Region&gt;CalendarDataTests</c> class derives from this base, overrides the two factory hooks, and
/// inherits the <see cref="CreateService_ForEverySupportedCountry_LoadsAndResolves" /> smoke test. The base itself
/// declares no <see cref="TestClassAttribute" />, so MSTest discovers and runs the inherited methods only under the
/// concrete derived classes in the leaf test assemblies.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The same derivation validates a consumer-built calendar shaped as a data-pack
/// // factory (see samples/Globalization.Calendar/...CustomAlgorithm for a worked example).
/// [TestClass]
/// public sealed class AsiaPacificCalendarDataTests
///     : CalendarDataTestsBase
/// {
///     protected override IReadOnlyList<string> SupportedCountries => AsiaPacificCalendarData.SupportedCountries;
///
///     protected override INotableDateService CreateService(string territory) =>
///         AsiaPacificCalendarData.CreateService(territory);
///
///     [TestMethod]
///     public void Resolve_ForKnownDate_ShouldMatchPublishedExpectation()
///     {
///         NotableDate labourDay = ResolveSingle("AU-VIC", 2026, "labour-day");
///
///         Assert.AreEqual(new DateOnly(2026, 3, 9), labourDay.Date);
///     }
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public abstract class CalendarDataTestsBase
{
    /// <summary>
    /// Gets the ISO 3166 country codes that the region under test supports.
    /// </summary>
    /// <value>The supported country codes exposed by the region's data factory.</value>
    protected abstract IReadOnlyList<string> SupportedCountries { get; }

    /// <summary>
    /// Creates a notable-date service for the supplied territory using the region's data factory.
    /// </summary>
    /// <param name="territory">The ISO 3166 country or subdivision code to load.</param>
    /// <returns>A service that resolves notable dates for <paramref name="territory" />.</returns>
    protected abstract INotableDateService CreateService(string territory);

    /// <summary>
    /// Verifies that every supported country loads and validates (the loader throws on a validation error) and
    /// resolves a non-empty set of holidays for a representative year.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void CreateService_ForEverySupportedCountry_LoadsAndResolves()
    {
        foreach (string country in SupportedCountries)
        {
            IReadOnlyList<NotableDate> holidays = ResolveYear(country, 2024);

            Assert.IsNotEmpty(holidays, $"{country} resolved no holidays for 2024");
        }
    }

    /// <summary>
    /// Resolves every notable date emitted for <paramref name="territory" /> across the whole of
    /// <paramref name="year" />.
    /// </summary>
    /// <param name="territory">The territory code to resolve.</param>
    /// <param name="year">The Gregorian year whose 1 January–31 December window is resolved.</param>
    /// <returns>The notable dates emitted for the territory and year, in resolution order.</returns>
    protected IReadOnlyList<NotableDate> ResolveYear(string territory, int year) =>
        CreateService(territory)
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), territory);

    /// <summary>
    /// Resolves the single occurrence of <paramref name="notableDateId" /> for <paramref name="territory" /> in
    /// <paramref name="year" />, asserting that exactly one occurrence is emitted.
    /// </summary>
    /// <param name="territory">The territory code to resolve.</param>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <param name="notableDateId">The notable-date id to select.</param>
    /// <returns>The single matching occurrence.</returns>
    protected NotableDate ResolveSingle(string territory, int year, string notableDateId)
    {
        var matches = ResolveYear(territory, year)
            .Where(r => r.NotableDateId == notableDateId)
            .ToList();

        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}' for {territory} {year}");
        return matches[0];
    }

    /// <summary>
    /// Asserts that <paramref name="actual" /> falls within <paramref name="toleranceDays" /> days of
    /// <paramref name="expected" />, used for moon-sighting and astronomical festivals whose modeled date may
    /// differ from the published date by a day or two.
    /// </summary>
    /// <param name="actual">The resolved date.</param>
    /// <param name="expected">The known published date.</param>
    /// <param name="toleranceDays">The maximum permitted absolute difference, in days.</param>
    /// <param name="context">A short scenario description included in the failure message.</param>
    protected static void AssertWithinDays(DateOnly actual, DateOnly expected, int toleranceDays, string context)
    {
        int deltaDays = Math.Abs(actual.DayNumber - expected.DayNumber);

        Assert.IsLessThanOrEqualTo(
            toleranceDays, deltaDays,
            $"{context}: resolved {actual}, expected within {toleranceDays} days of {expected}");
    }
}
