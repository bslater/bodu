// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvBasics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.DotEnv;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates the DotEnv document surface: <c>export</c> prefixes, double/single quoting,
/// inline comments, and empty values — and the deliberate design constraint that values are
/// returned <em>literally</em>: no <c>${VAR}</c> interpolation happens at parse time, so what
/// the file says is what your process gets.
/// </summary>
public static class DotEnvBasics
{
    /// <summary>
    /// Parses <c>Data/env.sample</c> and reads its entries.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- DotEnv: literal values, quoting, export prefix ---");

        var env = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "env.sample"));
        var document = DotEnv.Parse(env);

        Console.WriteLine($"entries      : {document.Entries.Count}");

        // 'export APP_ENV=...' binds as APP_ENV; the prefix is shell syntax, not part of the key.
        Console.WriteLine($"export prefix: APP_ENV = {document["APP_ENV"]}");

        // Quotes delimit, they are not content; the inline comment on FEATURE_FLAGS is dropped
        // from the value.
        Console.WriteLine($"double-quoted: DATABASE_URL = {document["DATABASE_URL"]}");
        Console.WriteLine($"single-quoted: GREETING = {document["GREETING"]}");
        Console.WriteLine($"inline commt : FEATURE_FLAGS = '{document["FEATURE_FLAGS"]}'");

        // Empty is a real value, distinct from absent.
        Console.WriteLine($"empty vs null: EMPTY_VALUE = '{document["EMPTY_VALUE"]}', MISSING = {(document["MISSING"] is null ? "null" : "?")}");

        // Typed access works here too.
        Console.WriteLine($"typed        : APP_PORT + 1 = {document.GetValue<int>("APP_PORT") + 1}");

        Console.WriteLine();
    }
}
