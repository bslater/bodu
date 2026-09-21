// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReadTypedValues.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;
using Bodu.Text.Ini.Document;
using Bodu.Text.Serialization;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates reading an INI file: the global (sectionless) keys hoisted onto the root object, named sections as
/// nested objects via the read-only <see cref="IniDocument" />, and typed access by binding the whole file onto a
/// settings class with <see cref="IniSerializer" /> and the snake_case naming policy. Note the parser deliberately
/// treats <c>;</c> after a value as value content — values may legitimately contain semicolons.
/// </summary>
public static class IniReadTypedValues
{
    /// <summary>
    /// Parses <c>Data/app.ini</c> and reads global, section, and typed data.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "INI - global keys, named sections, and typed binding",
            what: "Parses the committed INI file, reads a key that appears before any section header, lists the "
                + "named sections, reaches into one by element access, then binds the whole file onto a settings "
                + "class - one section as a POCO, another as a dictionary.",
            why: "INI has no specification, so a reader's job is largely choosing which of the conflicting "
                + "conventions to honour and saying so. Two choices show up here. Keys that appear before the "
                + "first header are hoisted onto the root rather than dropped or given a synthetic section, which "
                + "matches how most INI files are actually written. And a ';' after a value is treated as value "
                + "content rather than a comment, because connection strings and paths legitimately contain "
                + "semicolons and silently truncating them is the worse failure. The typed layer matters for a "
                + "different reason: INI values are all strings, so binding is where a port becomes an int once "
                + "rather than at every call site.",
            expect: "The global key resolves without naming a section. A section binds to a POCO where its shape "
                + "is known and to a dictionary where its keys are open-ended - both are ordinary properties on "
                + "the same class, so a file can mix the two. The typed port arithmetic works because the value "
                + "arrived as an int.");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "app.ini");
        var iniBytes = File.ReadAllBytes(path);
        using var document = IniDocument.Parse(iniBytes);
        var root = document.RootElement;

        // Keys before the first [section] hoist onto the root object; sections follow as objects.
        Console.WriteLine($"  global      : environment = {root.GetProperty("environment").GetString()}"
            + "  (declared before any [section] header, so it hoists onto the root rather than needing a synthetic section name)");

        var sections = new List<string>();
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == IniValueKind.Object)
            {
                sections.Add(property.Name);
            }
        }

        Console.WriteLine($"  sections    : {string.Join(", ", sections)}");

        // Element access into a section.
        var server = root.GetProperty("server");
        Console.WriteLine($"  [server]    : {server.GetProperty("host").GetString()}:{server.GetProperty("port").GetString()}"
            + "  (both values arrive as strings - INI has no types, which is what the typed layer below exists to fix)");

        // Typed access: bind the whole document onto a settings class via snake_case names.
        var options = new IniSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var config = IniSerializer.Deserialize<AppConfig>(File.ReadAllText(path), options);

        Console.WriteLine($"  typed       : port = {config.Server!.Port}, timeout = {config.Server.RequestTimeout}s"
            + "  (parsed once at bind time; request_timeout mapped to RequestTimeout by the snake_case naming policy)");
        Console.WriteLine($"  dictionary  : [logging] level = {config.Logging!["level"]}, keys = {config.Logging.Count}"
            + "  (a section whose keys are open-ended binds to a dictionary - the same class can mix POCO and dictionary sections)");

        Console.WriteLine();
    }

    /// <summary>
    /// A typed view of the sample file: scalar members bind global keys; object members bind sections.
    /// </summary>
    private sealed class AppConfig
    {
        public string? Environment { get; set; }

        public ServerSection? Server { get; set; }

        public Dictionary<string, string>? Logging { get; set; }
    }

    /// <summary>
    /// The <c>[server]</c> section as a typed POCO.
    /// </summary>
    private sealed class ServerSection
    {
        public string? Host { get; set; }

        public int Port { get; set; }

        public int RequestTimeout { get; set; }
    }
}
