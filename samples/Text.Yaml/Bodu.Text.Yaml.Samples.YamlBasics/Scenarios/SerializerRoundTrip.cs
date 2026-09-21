// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml.Samples.YamlBasics.Scenarios;

/// <summary>
/// Demonstrates the serializer's core loop: deserialize a committed YAML file into a typed POCO
/// graph (a nested mapping, a block sequence of mappings), then serialize it back and read it
/// again — the System.Text.Json-shaped workflow, for YAML.
/// </summary>
public static class SerializerRoundTrip
{
    /// <summary>
    /// Deserializes <c>Data/app-config.yaml</c>, mutates the POCO, and round-trips it.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "YamlSerializer - file to POCO to YAML and back",
            what: "Deserializes the committed config file into a typed object graph with a nested mapping and a "
                + "block sequence of mappings, reads the values, changes one, serializes back, and deserializes "
                + "the result again.",
            why: "YAML is the format most configuration is written in and the one with the largest gap between "
                + "what it can express and what a program should accept. Deliberately shaping this serializer "
                + "like System.Text.Json is the point: the options, attributes and naming policies are the ones "
                + "a .NET developer already knows, and a typed model puts a bound on the document rather than "
                + "accepting whatever the file happens to contain. The round trip matters because config tooling "
                + "usually has to write files back, and a library that reads a format but cannot re-emit it "
                + "leaves the caller hand-building YAML.",
            expect: "The nested mapping binds to a class property and the sequence of mappings to a list. After "
                + "the edit the re-emitted document parses back with the changed value and the same number of "
                + "endpoints, so re-emitting preserved both structures rather than flattening them.");

        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.yaml"));

        // Deserialize with the snake_case policy so service_name binds to ServiceName.
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        AppConfig config = YamlSerializer.Deserialize<AppConfig>(yaml, options)!;

        Console.WriteLine($"  Service   : {config.ServiceName} (enabled: {config.Enabled}, retries: {config.MaxRetries})");
        Console.WriteLine($"  Database  : {config.Database.Host}:{config.Database.Port}"
            + "  (a nested mapping binds to a nested class - YAML structure maps onto object structure)");
        Console.WriteLine($"  Endpoints : {string.Join(", ", config.Endpoints.Select(e => $"{e.Name} -> {e.Address}"))}"
            + "  (a block sequence of mappings binds to a list of objects)");

        // Mutate and serialize back - the nested mapping and the sequence re-emit as block YAML.
        config.MaxRetries = 5;
        var emitted = YamlSerializer.Serialize(config, options);

        AppConfig reloaded = YamlSerializer.Deserialize<AppConfig>(emitted, options)!;
        Console.WriteLine($"  Round trip: retries {reloaded.MaxRetries}, endpoints {reloaded.Endpoints.Count} (values preserved)"
            + "  (the edit survives and neither structure flattens - config tooling usually has to write the file back)");

        Console.WriteLine();
    }
}
