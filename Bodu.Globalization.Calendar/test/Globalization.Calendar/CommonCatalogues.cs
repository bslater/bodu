// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CommonCatalogues.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads the bundled common notable-date catalogues (the shipped <c>Resources\*.xml</c> files) into a resolvable
/// <see cref="NotableDateService" />, so the known-answer tests validate the exact catalogues that territory packs
/// inherit rather than a test-local copy.
/// </summary>
/// <remarks>
/// <para>
/// A catalogue concept is authored without a territory scope, so it resolves for any territory; the tests resolve with
/// the neutral placeholder territory <c>"XX"</c>. Catalogues that declare <c>&lt;Imports&gt;</c> (for example
/// <c>christian-protestant</c>) are loaded through <see cref="CommonNotableDateResources.Resolver" /> so the imported
/// concepts resolve against the sibling catalogues.
/// </para>
/// </remarks>
internal static class CommonCatalogues
{
    /// <summary>
    /// The neutral placeholder territory used to resolve unscoped catalogue concepts.
    /// </summary>
    public const string NeutralTerritory = "XX";

    /// <summary>
    /// Loads a bundled common catalogue and returns a service over it.
    /// </summary>
    /// <param name="catalogueName">The catalogue name, for example <c>global-bahai</c>.</param>
    /// <returns>A service for the loaded catalogue.</returns>
    /// <exception cref="InvalidOperationException">The catalogue is not a bundled resource.</exception>
    public static NotableDateService Service(string catalogueName)
    {
        var content = CommonNotableDateResources.Resolve(catalogueName)
            ?? throw new InvalidOperationException($"Unknown common catalogue '{catalogueName}'.");
        NotableDateResource resource = NotableDateResourceLoader.Load(content, CommonNotableDateResources.Resolver);

        return new NotableDateService(resource);
    }

    /// <summary>
    /// Resolves the occurrences of a named concept whose anchor falls in the supplied Gregorian year, ordered by date.
    /// </summary>
    /// <param name="service">The service to resolve through.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The occurrences anchored in the requested year.</returns>
    /// <remarks>
    /// <para>
    /// The <c>Date.Year == year</c> filter discards an occurrence whose multi-day span or calendar-year sweep carries
    /// it across the year boundary from an anchor in the adjacent Gregorian year (notably Hanukkah, which can begin 25
    /// or 26 December and therefore also surfaces when the following year is queried).
    /// </para>
    /// </remarks>
    public static List<NotableDate> ResolveForYear(NotableDateService service, string notableDateId, int year) =>
        service
            .Resolve(new DateRange(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)), NeutralTerritory)
            .Where(r => r.NotableDateId == notableDateId && r.Date.Year == year)
            .OrderBy(r => r.Date)
            .ToList();

    /// <summary>
    /// Resolves the single occurrence of a named concept anchored in the supplied Gregorian year, asserting that
    /// exactly one is produced.
    /// </summary>
    /// <param name="service">The service to resolve through.</param>
    /// <param name="notableDateId">The notable-date id to resolve.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The single occurrence anchored in the requested year.</returns>
    public static NotableDate ResolveSingle(NotableDateService service, string notableDateId, int year)
    {
        List<NotableDate> matches = ResolveForYear(service, notableDateId, year);

        Assert.HasCount(1, matches, $"expected exactly one '{notableDateId}' anchored in {year}");
        return matches[0];
    }
}
