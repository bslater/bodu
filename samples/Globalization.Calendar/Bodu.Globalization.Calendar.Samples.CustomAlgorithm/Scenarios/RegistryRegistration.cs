// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RegistryRegistration.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Scenarios;

/// <summary>
/// Demonstrates the registry route for custom algorithms: register the implementation under a key,
/// reference the key declaratively from a rule (<c>Algorithm("...")</c>), and hand the registry to
/// both the loader (which validates the reference) and the service (which dispatches it). Rules stay
/// data; only the date mathematics is code.
/// </summary>
public static class RegistryRegistration
{
    /// <summary>
    /// Wires the founding-day algorithm through registry, document, loader, and service.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "A custom algorithm through the registry",
            what: "Registers a hand-written algorithm under a key, authors a document whose rule references that "
                + "key, loads it with the registry and serves it with the same registry, then resolves three "
                + "years including one that predates the founding date.",
            why: "The engine is data-driven so that adding a holiday does not mean shipping code - but some "
                + "dates cannot be expressed declaratively, because their calculation is genuinely arbitrary. "
                + "The registry is the seam for exactly those: the rule stays data and references a key, and only "
                + "the mathematics is code. Passing the registry to both the loader and the service is "
                + "deliberate rather than redundant - the loader validates that the key exists, so a typo is a "
                + "load-time diagnostic instead of a silent missing holiday at run time, and the service is what "
                + "dispatches the calculation per year. The algorithm returning null is part of the contract, "
                + "which is how a concept that does not exist in every year is expressed.",
            expect: "The 2024 occurrence is shifted off the actual founding anniversary, because the algorithm "
                + "itself decides that - the shifting is inside the code, not a declarative adjustment. The year "
                + "before the company existed yields no occurrence at all rather than a nonsense date, which is "
                + "the null return doing its job.");

        // 1. Register the algorithm under the key rules will reference.
        var algorithms = new NotableDateAlgorithmRegistry()
            .Register("company-founding", new CompanyFoundingDayAlgorithm());

        // 2. Author (or hand-write) a document whose rule uses the AlgorithmDateStrategy by key.
        var xml = NotableDateDocumentBuilder.Create("contoso-algorithms")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("algorithm", r => r.Algorithm("company-founding")))
            .ToXml();

        // 3. Load with the registry (the loader validates the key exists) and serve with the same
        //    registry (the service dispatches Calculate per resolved year).
        NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null, algorithms);
        var service = new NotableDateService(resource, new NotableDateServiceOptions { Algorithms = algorithms });

        // 12 March 2024 is a Tuesday -> celebrated Friday 15 March; 1997 predates the founding.
        foreach (var year in new[] { 2024, 2026, 1997 })
        {
            IReadOnlyList<NotableDate> dates = service.Resolve(year, "AU");
            Console.WriteLine(dates.Count == 0
                ? $"    {year}: no occurrence (algorithm returned null)"
                : $"    {year}: {dates[0].Date:yyyy-MM-dd} ({dates[0].Date.DayOfWeek}) {dates[0].DisplayName}");
        }

        Console.WriteLine("  (the shift off the anniversary is inside the algorithm, not a declarative adjustment - and 1997 returns null rather than a nonsense date)");

        // --- To load algorithms from an external plugin assembly instead -------------------------
        // The Bodu.Globalization.Calendar.Plugins package adds trust-gated discovery, so operations
        // can drop algorithm plugins next to the host without recompiling it:
        // 1. dotnet add package Bodu.Globalization.Calendar.Plugins
        // 2. In the plugin assembly: implement INotableDateAlgorithmPlugin and declare
        //        [assembly: NotableDatePlugin(typeof(ContosoAlgorithmsPlugin))]
        // 3. In the host - never load unverified assemblies; pin the publisher:
        //        var trust = new StrongNamePluginTrustPolicy(expectedPublicKeyTokens);
        //        INotableDatePlugin plugin = NotableDatePluginLoader.LoadFrom(pluginPath, trust);
        //        NotableDatePluginLoader.RegisterAlgorithms(plugin, algorithms);
        //    ...then pass `algorithms` to the loader and service exactly as above.
        // ------------------------------------------------------------------------------------------

        Console.WriteLine();
    }
}
