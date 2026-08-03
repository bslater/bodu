// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservationBasedVariant.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Scenarios;

/// <summary>
/// Demonstrates an observation-based built-in algorithm: the <c>tehran-nowruz</c> key computes Nowruz from the
/// true vernal-equinox instant at the Tehran standard meridian — the official Iranian rule — as an opt-in
/// alternative to tabular Persian-calendar resources.
/// </summary>
public static class ObservationBasedVariant
{
    /// <summary>
    /// Authors a rule over the built-in observation-based key and resolves the boundary years.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- tehran-nowruz: an observation-based built-in algorithm ---");

        // The key is built in, so a document can reference it without registering anything. The rule is
        // opt-in: nothing changes for documents anchored on the tabular Persian calendar.
        NotableDateResource resource = NotableDateDocumentBuilder.Create("sample.nowruz")
            .AddNotableDate("nowruz", "Nowruz", NotableDateCategory.PublicHoliday, d => d
                .AddRule("astronomical", r => r.Algorithm("tehran-nowruz")))
            .Build();

        var service = new NotableDateService(resource);

        // 2024 -> March 20 (equinox before Tehran apparent noon); 2025 -> March 21 (equinox after noon):
        // the rule reproduces the published March 20/21 boundary exactly.
        foreach (int year in new[] { 2024, 2025, 2026 })
        {
            var nowruz = service.Resolve(year, "IR").Single(n => n.NotableDateId == "nowruz");
            Console.WriteLine($"Nowruz {year}: {nowruz.Date:yyyy-MM-dd}");
        }

        Console.WriteLine();
    }
}
