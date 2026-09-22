// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadingAPlugin.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Builder;
using Bodu.Globalization.Calendar.Plugins;

namespace Bodu.Globalization.Calendar.Samples.Plugins.Scenarios;

/// <summary>
/// Demonstrates loading an external plugin assembly, registering the algorithms it contributes, and resolving a
/// notable date whose rule references one of them by key.
/// </summary>
public static class LoadingAPlugin
{
    /// <summary>
    /// Loads the plugin, registers its algorithms, and resolves a year against a rule that uses one.
    /// </summary>
    /// <param name="pluginPath">The plugin assembly to load.</param>
    public static void Run(string pluginPath)
    {
        SampleConsole.Scenario(
            "Loading a plugin and using the algorithm it contributes",
            what: "Loads a plugin assembly by file path, prints the name and version it reports, registers its "
                + "algorithms into a NotableDateAlgorithmRegistry, and resolves a year against a rule document that "
                + "references one of them by key.",
            why: "A date-calculation algorithm is code, and some calendars need code no library can ship - a company "
                + "observance, a regional rule, an astronomical calculation with a house convention. Without plugins "
                + "the only way to add one is to recompile the host, which is not available to an operator holding a "
                + "deployed binary. The key is what keeps the two sides decoupled: the plugin publishes "
                + "'contoso.founding-day' and the rule document references that string, so neither side needs a type "
                + "from the other. Note what this host does NOT have - its project reference to the plugin sets "
                + "ReferenceOutputAssembly=false, so the plugin's types are genuinely unavailable at compile time.",
            expect: "The plugin reports 'Contoso Calendar' version 1.0.0 and contributes one algorithm. The resolved "
                + "year shows Founding Day on a Friday - 13 March 2026 - because the algorithm rolls 12 March to the "
                + "Friday of its week. New Year's Day resolves from an ordinary fixed rule beside it, so plugin-fed "
                + "and built-in rules sit in one document.");

        // AllowAllPluginTrustPolicy is correct here and nowhere else: this assembly was produced by
        // this repository's own build. The next scenario covers what to use for anything else.
        using NotableDatePluginHandle handle = NotableDatePluginLoader.LoadFromFile(
            pluginPath, new AllowAllPluginTrustPolicy());

        Console.WriteLine($"  Loaded from             : {Path.GetFileName(pluginPath)}");
        Console.WriteLine($"  Plugin name / version   : {handle.Plugin.Name} {handle.Plugin.Version}");
        Console.WriteLine($"  Unloadable              : {handle.IsUnloadable}   (loaded into a collectible context, so disposing the handle releases the assembly)");

        // Registering copies the plugin's key/algorithm pairs into a registry the engine can dispatch through.
        var registry = new NotableDateAlgorithmRegistry();
        var registered = NotableDatePluginLoader.RegisterAlgorithms(handle.Plugin, registry);
        Console.WriteLine($"  Algorithms registered   : {registered}");
        Console.WriteLine();

        // A rule document referencing the plugin's key by string - no type from the plugin appears here.
        var xml = NotableDateDocumentBuilder.Create("contoso-plugin-holidays")
            .WithMetadata("Contoso holidays", "Company days, one of them calculated by a plugin")
            .AddNotableDate("founding-day", "Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("algorithm", r => r.Algorithm("contoso.founding-day")))
            .AddNotableDate("new-year", "New Year's Day", NotableDateCategory.PublicHoliday, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(1, 1)))
            .ToXml();

        // Passing the registry to the loader makes an unregistered key a load-time failure rather
        // than a silent gap at resolve time.
        var resource = NotableDateResourceLoader.Load(xml, _ => null, registry);
        var service = new NotableDateService(resource, new NotableDateServiceOptions { Algorithms = registry });

        Console.WriteLine("  Resolved 2026 (AU):");
        foreach (NotableDate date in service.Resolve(2026, "AU"))
        {
            var source = date.Identity.RuleId == "algorithm" ? "plugin algorithm" : "built-in fixed rule";
            Console.WriteLine($"    {date.Date:yyyy-MM-dd ddd}  {date.DisplayName,-18} via {source}");
        }

        Console.WriteLine();
    }
}
