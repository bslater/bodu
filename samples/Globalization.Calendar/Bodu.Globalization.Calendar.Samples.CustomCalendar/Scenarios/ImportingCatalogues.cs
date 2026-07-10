// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ImportingCatalogues.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomCalendar.Scenarios;

/// <summary>
/// Demonstrates composing an authored calendar with the shared catalogues the data packs themselves
/// import: declare an <c>Import</c>, cherry-pick concepts with <c>Use</c> (optionally re-categorised
/// or re-scoped per territory), and resolve the import through
/// <see cref="CommonNotableDateResources.Resolver" /> at build time. No rule is copied — the
/// catalogue stays the single source of truth for the Easter computus and friends.
/// </summary>
public static class ImportingCatalogues
{
    /// <summary>
    /// Imports Easter concepts from the shared western-Christian catalogue into a company calendar.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Importing the shared catalogues ---");

        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("contoso-with-easter")
            // Cherry-pick concepts from the catalogue and mark them non-working for this company.
            // Good Friday and Easter Monday are defined as offsets from easter-sunday, so the
            // anchor concept must be used too - the validator enforces the dependency.
            .AddImport("christian-western", i => i
                .Use("easter-sunday")
                .Use("good-friday", u => u.WithCategory(NotableDateCategory.PublicHoliday).AsNonWorking())
                .Use("easter-monday", u => u.WithCategory(NotableDateCategory.PublicHoliday).AsNonWorking()))
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(3, 12)));

        // Build(resolver) resolves the import by name. CommonNotableDateResources.Resolver serves
        // the same embedded catalogues the regional data packs use.
        NotableDateResource resource = builder.Build(CommonNotableDateResources.Resolver);
        var service = new NotableDateService(resource);

        foreach (NotableDate date in service.Resolve(2024, "AU").OrderBy(d => d.Date))
            Console.WriteLine($"  {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-22} non-working: {date.IsNonWorkingDay}");

        // The Easter dates came from the catalogue's computus algorithm - nothing was hand-coded.
        Console.WriteLine();
    }
}
