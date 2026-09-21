// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TemporalKinds.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization;
using Bodu.Text.Toml;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates TOML's headline feature over JSON: four native date-time kinds. An offset
/// date-time is an exact instant; a local date-time, local date, and local time deliberately carry
/// no zone — and the serializer maps each to the matching .NET type instead of forcing everything
/// through <see cref="DateTime" />.
/// </summary>
public static class TemporalKinds
{
    /// <summary>
    /// Round-trips each temporal kind and shows the wire form it produces.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "The four native temporal kinds",
            what: "Deserializes a document carrying a local date, a local time and an offset date-time, shows "
                + "which .NET type each became, then serializes the graph back and prints the wire form of each.",
            why: "This is TOML's clearest advantage over JSON for configuration. JSON has no date type, so every "
                + "timestamp is a string and every consumer re-invents the parsing - which is how a release date "
                + "acquires an invented midnight and a local maintenance window silently becomes UTC. TOML "
                + "distinguishes four kinds, and the distinction is semantic rather than cosmetic: an offset "
                + "date-time names an exact instant, while a local date-time deliberately does not, because "
                + "'02:00 on Sunday' for a maintenance window means 02:00 wherever the server is. Mapping them "
                + "onto DateOnly, TimeOnly and DateTimeOffset preserves that distinction in the type system "
                + "instead of losing it at the boundary.",
            expect: "Each value arrives as the .NET type that carries exactly the information the document "
                + "stated - no invented midnight on the date, no assumed zone on the time. Serializing back "
                + "produces the same three distinct wire forms, so the round trip does not quietly promote a "
                + "local value to an instant.");

        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.toml"));
        AppConfig config = TomlSerializer.Deserialize<AppConfig>(toml, options);

        // Each TOML kind arrived as its natural .NET type - no invented midnights or UTC guesses.
        Console.WriteLine($"  released_on        -> DateOnly       : {config.ReleasedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
            + "  (a date with no time component - DateTime would have invented a midnight that the document never stated)");
        Console.WriteLine($"  maintenance_window -> TimeOnly       : {config.MaintenanceWindow.ToString("HH:mm", CultureInfo.InvariantCulture)}"
            + "  (a wall-clock time carrying no zone, deliberately - a maintenance window means that hour wherever the server is)");
        Console.WriteLine($"  build_stamp        -> DateTimeOffset : {config.BuildStamp.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)}"
            + "  (the one kind that names an exact instant, so its offset is part of the value rather than an assumption)");

        // And each .NET type serializes back to its distinct TOML kind.
        var emitted = TomlSerializer.Serialize(config, options);
        foreach (var line in emitted.Split('\n').Where(l =>
            l.StartsWith("released_on", StringComparison.Ordinal) ||
            l.StartsWith("maintenance_window", StringComparison.Ordinal) ||
            l.StartsWith("build_stamp", StringComparison.Ordinal)))
        {
            Console.WriteLine($"  wire: {line.TrimEnd()}");
        }

        Console.WriteLine("  (three distinct wire forms back out - the round trip does not promote a local value to an instant)");

        Console.WriteLine();
    }
}
