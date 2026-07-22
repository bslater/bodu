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
        Console.WriteLine("--- DotEnv: literal values, quoting, export prefix ---");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "env.sample");
        var envBytes = File.ReadAllBytes(path);
        using var document = DotEnvDocument.Parse(envBytes);
        var root = document.RootElement;

        var entryCount = 0;
        foreach (var _ in root.EnumerateObject())
        {
            entryCount++;
        }

        Console.WriteLine($"entries      : {entryCount}");

        // 'export APP_ENV=...' binds as APP_ENV; the prefix is shell syntax, not part of the key.
        Console.WriteLine($"export prefix: APP_ENV = {root.GetProperty("APP_ENV").GetString()}");

        // Quotes delimit, they are not content; the inline comment on FEATURE_FLAGS is dropped
        // from the value.
        Console.WriteLine($"double-quoted: DATABASE_URL = {root.GetProperty("DATABASE_URL").GetString()}");
        Console.WriteLine($"single-quoted: GREETING = {root.GetProperty("GREETING").GetString()}");
        Console.WriteLine($"inline commt : FEATURE_FLAGS = '{root.GetProperty("FEATURE_FLAGS").GetString()}'");

        // Empty is a real value, distinct from absent.
        var missing = root.TryGetProperty("MISSING", out _);
        Console.WriteLine($"empty vs null: EMPTY_VALUE = '{root.GetProperty("EMPTY_VALUE").GetString()}', MISSING present = {missing}");

        // Typed access via the serializer and the Web (SCREAMING_SNAKE_CASE) defaults.
        var settings = DotEnvSerializer.Deserialize<EnvSettings>(
            File.ReadAllText(path),
            new DotEnvSerializerOptions(DotEnvSerializerDefaults.Web));
        Console.WriteLine($"typed        : APP_PORT + 1 = {settings.AppPort + 1}");

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
