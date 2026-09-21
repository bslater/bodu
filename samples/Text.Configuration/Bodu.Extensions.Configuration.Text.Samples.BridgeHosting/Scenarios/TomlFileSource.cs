// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileSource.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Samples.Extensions.Configuration.Text.BridgeHosting.Scenarios;

/// <summary>
/// Demonstrates <c>AddTomlFile</c>: a TOML document becomes an <see cref="IConfiguration" />
/// source, its tables flattened to the standard colon-separated keys — <c>[server.limits]</c>
/// surfaces as <c>server:limits:*</c> — alongside any other provider in the same builder.
/// </summary>
public static class TomlFileSource
{
    /// <summary>
    /// Builds configuration from <c>Data/settings.toml</c> and reads flattened keys.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "AddTomlFile - a TOML document as an IConfiguration source",
            what: "Registers a TOML file with a standard ConfigurationBuilder, reads a top-level key and two "
                + "levels of nested table, composes a section the way any provider's tree composes, and builds "
                + "again over a file that does not exist while marked optional.",
            why: "Microsoft.Extensions.Configuration is a key/value abstraction with one shape - a flat map of "
                + "colon-separated string keys, layered by provider order. A format only has to answer one "
                + "question to join it: how does a nested structure flatten into that key space. Here a TOML "
                + "table becomes a key prefix, which is the same rule the built-in JSON provider uses, so a TOML "
                + "file layers with appsettings.json, environment variables and command-line arguments without "
                + "anything downstream knowing which provider a value came from. That is the whole value of the "
                + "bridge: the format choice stops being an architectural decision.",
            expect: "Nested tables surface as colon-separated keys, so [server.limits] is read as "
                + "'server:limits:max_connections' - the same shape a JSON provider would produce for the same "
                + "structure. GetSection returns a normal section over that tree. The optional missing file "
                + "builds cleanly with no keys instead of throwing, which is what makes an optional local "
                + "override file safe to register unconditionally.");

        // AddTomlFile reads the path directly (absolute, or relative to the working
        // directory) - it does not consult the builder's file provider, so anchor it.
        var tomlPath = Path.Combine(AppContext.BaseDirectory, "Data", "settings.toml");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTomlFile(tomlPath)
            .Build();

        Console.WriteLine($"  title                    : {configuration["title"]}"
            + "  (a top-level TOML key becomes a top-level configuration key)");
        Console.WriteLine($"  server:host              : {configuration["server:host"]}");
        Console.WriteLine($"  server:port              : {configuration["server:port"]}");
        Console.WriteLine($"  server:limits:max_connections : {configuration["server:limits:max_connections"]}"
            + "  (the nested [server.limits] table flattens to a colon path - the same rule the built-in JSON provider applies)");

        // GetSection composes like any other provider's tree.
        var server = configuration.GetSection("server");
        Console.WriteLine($"  GetSection(\"server\")     : {server.GetChildren().Count()} children"
            + "  (an ordinary section over an ordinary tree - nothing downstream can tell the values came from TOML)");

        // Optional files are skipped instead of throwing.
        var withOptional = new ConfigurationBuilder()
            .AddTomlFile(Path.Combine(AppContext.BaseDirectory, "Data", "does-not-exist.toml"), optional: true)
            .Build();
        Console.WriteLine($"  optional missing file    : builds clean ({withOptional.GetChildren().Count()} keys)"
            + "  (expected 0 keys and no exception - what makes a local override file safe to register unconditionally)");

        Console.WriteLine();
    }
}
