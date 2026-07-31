// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ContosoCalendarData.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomAlgorithm;

/// <summary>
/// The sample's calendar packaged as a data-pack-shaped factory — the same
/// <c>SupportedCountries</c> / <c>LoadResource</c> / <c>CreateService</c> surface the regional
/// packs expose. Shaping a custom calendar this way makes it a drop-in peer of the shipped packs
/// and, importantly, lets the shared <c>CalendarDataTestsBase</c> contract tests validate it (see
/// the companion test project).
/// </summary>
public static class ContosoCalendarData
{
    /// <summary>
    /// Gets the territories this calendar serves. Contoso operates in Australia only.
    /// </summary>
    public static IReadOnlyList<string> SupportedCountries { get; } = ["AU"];

    /// <summary>
    /// Loads the authored company calendar with its custom algorithm registered.
    /// </summary>
    /// <param name="territory">The territory to load for (must be supported).</param>
    /// <returns>The validated resource.</returns>
    public static NotableDateResource LoadResource(string territory)
    {
        // Accept subdivision codes (e.g. AU-VIC) by validating the country prefix, as the regional packs do.
        if (!SupportedCountries.Contains(territory.Split('-')[0]))
            throw new ArgumentException($"Territory '{territory}' is not supported by the Contoso calendar.", nameof(territory));

        var xml = NotableDateDocumentBuilder.Create("contoso-holidays")
            .WithMetadata("Contoso holidays", "Company-observed days for Contoso Australia")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("algorithm", r => r.Algorithm("company-founding")))
            .AddNotableDate("new-year", "New Year's Day", NotableDateCategory.PublicHoliday, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(1, 1)))
            .ToXml();

        // The middle argument is the catalogue-import resolver; this document declares no imports, so it returns
        // null. Passing the registry lets the loader validate that every referenced algorithm key is registered.
        return NotableDateResourceLoader.Load(xml, _ => null, Algorithms);
    }

    /// <summary>
    /// Creates a ready-to-query service over the company calendar.
    /// </summary>
    /// <param name="territory">The territory to serve.</param>
    /// <returns>An immutable, thread-safe service.</returns>
    public static NotableDateService CreateService(string territory) =>
        // The service takes the same registry so it can dispatch the custom algorithm at resolve time.
        new(LoadResource(territory), new NotableDateServiceOptions { Algorithms = Algorithms });

    /// <summary>
    /// Gets the algorithm registry the calendar's rules reference.
    /// </summary>
    private static NotableDateAlgorithmRegistry Algorithms { get; } =
        new NotableDateAlgorithmRegistry().Register("company-founding", new CompanyFoundingDayAlgorithm());
}
