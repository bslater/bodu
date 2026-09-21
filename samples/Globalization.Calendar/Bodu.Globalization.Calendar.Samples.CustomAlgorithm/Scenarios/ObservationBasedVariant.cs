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
        SampleConsole.Scenario(
            "tehran-nowruz - an observation-based built-in algorithm",
            what: "Authors a rule over the built-in tehran-nowruz key, without registering anything, and "
                + "resolves three consecutive years across the March 20/21 boundary.",
            why: "Nowruz is the official Iranian new year and its date is not a calendar arithmetic problem: it "
                + "falls on the day containing the true vernal equinox, measured at the Tehran standard "
                + "meridian, so which calendar date it lands on depends on whether that instant falls before or "
                + "after local apparent noon. A tabular Persian calendar approximates this and disagrees with "
                + "the official date in some years. Shipping the astronomical computation as a built-in key "
                + "means a document can have the official rule without the author implementing celestial "
                + "mechanics, and making it opt-in rather than the default means nothing changes for documents "
                + "that are anchored on the tabular calendar on purpose.",
            expect: "The three years do not agree on a calendar date, and the disagreement is the point: 2024 "
                + "resolves to 20 March and 2025 to 21 March, reproducing the published boundary that a fixed "
                + "date or a tabular approximation gets wrong.");

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
            Console.WriteLine($"  Nowruz {year}: {nowruz.Date:yyyy-MM-dd}");
        }

        Console.WriteLine("  (the 20/21 March boundary reproduced from the equinox instant at the Tehran meridian - what a fixed date or a tabular approximation gets wrong)");

        Console.WriteLine();
    }
}
