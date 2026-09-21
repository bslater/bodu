// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Toml;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates the serializer's core loop: deserialize a committed TOML file into a typed POCO
/// graph (nested tables, an array of tables), then serialize it back and read it again — the
/// System.Text.Json-shaped workflow, for TOML.
/// </summary>
public static class SerializerRoundTrip
{
    /// <summary>
    /// Deserializes <c>Data/app-config.toml</c>, mutates the POCO, and round-trips it.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "TomlSerializer - file to POCO to TOML and back",
            what: "Deserializes the committed config file into a typed object graph with a nested table and an "
                + "array of tables, reads the values, changes one, serializes the graph back to TOML, and "
                + "deserializes that result again.",
            why: "TOML exists because INI has no specification and JSON is awkward to hand-edit - it is a "
                + "configuration format with a grammar, which means a document can be validated rather than "
                + "merely parsed. The serializer is deliberately shaped like System.Text.Json so the "
                + "options, attributes and naming policies are the ones a .NET developer already knows. The "
                + "round trip is the interesting assertion here rather than the deserialize: a serializer that "
                + "reads a format but cannot re-emit it is a parser, and config tooling usually needs to write "
                + "the file back.",
            expect: "Both structural shapes survive: the nested table binds to a class property and the array of "
                + "tables to a list. After the edit the re-emitted document parses back with the changed value "
                + "and the same number of endpoints, so re-emitting did not flatten or reorder the structure.");

        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.toml"));

        // Deserialize with the snake_case policy so service_name binds to ServiceName.
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        AppConfig config = TomlSerializer.Deserialize<AppConfig>(toml, options);

        Console.WriteLine($"  Service   : {config.ServiceName} (enabled: {config.Enabled}, retries: {config.MaxRetries})");
        Console.WriteLine($"  Database  : {config.Database.Host}:{config.Database.Port}"
            + "  (a [database] table binds to a nested class - TOML structure maps onto object structure)");
        Console.WriteLine($"  Endpoints : {string.Join(", ", config.Endpoints.Select(e => $"{e.Name} -> {e.Address}"))}"
            + "  (repeated [[endpoints]] blocks bind to a list - the array-of-tables form TOML uses for collections)");

        // Mutate and serialize back - nested tables and the array of tables re-emit as TOML.
        config.MaxRetries = 5;
        var emitted = TomlSerializer.Serialize(config, options);

        AppConfig reloaded = TomlSerializer.Deserialize<AppConfig>(emitted, options);
        Console.WriteLine($"  Round trip: retries {reloaded.MaxRetries}, endpoints {reloaded.Endpoints.Count} (values preserved)"
            + "  (the edit survives and the structure does not flatten - a serializer that cannot re-emit is only a parser)");

        Console.WriteLine();
    }
}
