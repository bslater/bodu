// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Document;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates the DotEnv read surfaces: <c>export</c> prefixes, double/single quoting, inline comments, and empty
/// values via the read-only <see cref="DotEnvDocument" /> — plus <see cref="DotEnvSerializer" /> binding the file
/// straight onto a typed settings class with the SCREAMING_SNAKE_CASE naming policy. Values are returned
/// <em>literally</em>: no <c>${VAR}</c> interpolation happens at parse time.
/// </summary>
public static class DotEnvBasics
{
    /// <summary>
    /// Parses <c>Data/env.sample</c> and reads its entries.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "DotEnv - literal values, quoting, and the export prefix",
            what: "Parses the committed env file and reads an exported key, a double-quoted value, a "
                + "single-quoted one, a value followed by an inline comment, and an empty value - then binds the "
                + "same file onto a typed settings class.",
            why: "A .env file looks like shell but is not shell, and the gap is where the bugs live. This reader "
                + "returns values literally: a ${VAR} reference stays the six characters it is, because "
                + "interpolating at parse time would mean a config file could read the process environment, and "
                + "which variables were in scope would decide what the file meant. Quotes are delimiters rather "
                + "than content, an 'export' prefix is shell syntax rather than part of the key, and a trailing "
                + "comment is not part of the value. The empty-versus-absent distinction is the other one worth "
                + "knowing: an empty value is a value, and a consumer that treats it as unset will silently "
                + "substitute a default the author explicitly overrode.",
            expect: "The exported key binds without its prefix, quotes do not appear in the values, and the "
                + "inline comment is gone. The empty value reads as an empty string while a genuinely missing key "
                + "reports absent - two different answers that a naive reader collapses into one. The typed row "
                + "shows the SCREAMING_SNAKE_CASE policy mapping APP_PORT onto an int property.");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "env.sample");
        var envBytes = File.ReadAllBytes(path);
        using var document = DotEnvDocument.Parse(envBytes);
        var root = document.RootElement;

        var entryCount = 0;
        foreach (var _ in root.EnumerateObject())
        {
            entryCount++;
        }

        Console.WriteLine($"  entries      : {entryCount}");

        // 'export APP_ENV=...' binds as APP_ENV; the prefix is shell syntax, not part of the key.
        Console.WriteLine($"  export prefix: APP_ENV = {root.GetProperty("APP_ENV").GetString()}"
            + "  (written as 'export APP_ENV=...' - the prefix is shell syntax and is not part of the key)");

        // Quotes delimit, they are not content; the inline comment on FEATURE_FLAGS is dropped
        // from the value.
        Console.WriteLine($"  double-quoted: DATABASE_URL = {root.GetProperty("DATABASE_URL").GetString()}");
        Console.WriteLine($"  single-quoted: GREETING = {root.GetProperty("GREETING").GetString()}"
            + "  (the quotes delimit the value, they are not content)");
        Console.WriteLine($"  inline commt : FEATURE_FLAGS = '{root.GetProperty("FEATURE_FLAGS").GetString()}'"
            + "  (the trailing comment is stripped, and the value is not trimmed of meaning beyond it)");

        // Empty is a real value, distinct from absent.
        var missing = root.TryGetProperty("MISSING", out _);
        Console.WriteLine($"  empty vs null: EMPTY_VALUE = '{root.GetProperty("EMPTY_VALUE").GetString()}', MISSING present = {missing}"
            + "  (an empty value is a value - collapsing it into 'absent' would silently restore a default the author overrode)");

        // Typed access via the serializer and the Web (SCREAMING_SNAKE_CASE) defaults.
        var settings = DotEnvSerializer.Deserialize<EnvSettings>(
            File.ReadAllText(path),
            new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web));
        Console.WriteLine($"  typed        : APP_PORT + 1 = {settings.AppPort + 1}"
            + "  (arithmetic works because the Web preset's SCREAMING_SNAKE_CASE policy bound APP_PORT onto an int property)");

        Console.WriteLine();
    }

    /// <summary>
    /// A typed view of the sample env file; the Web defaults map APP_PORT onto <see cref="AppPort" />.
    /// </summary>
    private sealed class EnvSettings
    {
        public string? AppEnv { get; set; }

        public int AppPort { get; set; }

        public string? DatabaseUrl { get; set; }
    }
}
