// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvStreamingReader.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.DotEnv;
using Bodu.Text.DotEnv.Reader;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates the forward-only <see cref="Utf8DotEnvReader" />: one token in memory at a time, with the line number
/// attached — the surface for scanning env files without materializing a document, e.g. a linter that flags
/// suspicious keys as it streams.
/// </summary>
public static class DotEnvStreamingReader
{
    /// <summary>
    /// Streams <c>Data/env.sample</c> and reports each entry with its source line.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "DotEnv - the forward-only reader with line numbers",
            what: "Streams the same env file one token at a time, holding each key until its value token arrives, "
                + "and runs a small lint pass that flags keys whose names suggest they may carry credentials.",
            why: "The document DOM is the right surface when you want the values; this one is right when you want "
                + "to say something about the file itself. A linter, a secret scanner or a migration tool needs "
                + "the source line to report against, and that is exactly what a materialized document throws "
                + "away - by the time you have a dictionary, the file is gone. Reading forward-only also means "
                + "nothing larger than one token is held, which is what makes scanning a directory of env files "
                + "cheap rather than proportional to their combined size.",
            expect: "Each entry is reported with the line it came from, which is what a diagnostic needs to be "
                + "actionable. The key and value arrive as separate tokens, so the loop holds the key across the "
                + "read - that is the shape of every forward-only reader, and why the line number is captured "
                + "with the key rather than with the value.");

        var envBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "env.sample"));
        var reader = new Utf8DotEnvReader(envBytes);

        string? key = null;
        var line = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                // Key and value arrive as separate tokens: hold the key (and its line) until the value follows.
                case DotEnvTokenType.PropertyName:
                    key = reader.GetString();
                    line = reader.LineNumber;
                    break;

                case DotEnvTokenType.String:
                    // A tiny lint pass: flag keys that look like they carry secrets.
                    var flag = key!.Contains("URL", StringComparison.Ordinal) ? "  <- check for embedded credentials" : string.Empty;
                    Console.WriteLine($"  line {line,2}: {key} = '{reader.GetString()}'{flag}");
                    break;

                default:
                    break;
            }
        }

        Console.WriteLine("  (the line number comes from the reader, not reconstructed - a materialized document would have discarded it)");

        Console.WriteLine();
    }
}
