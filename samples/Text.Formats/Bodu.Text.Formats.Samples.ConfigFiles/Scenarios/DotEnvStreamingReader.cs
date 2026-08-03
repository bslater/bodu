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
        Console.WriteLine("--- DotEnv: streaming reader with line numbers ---");

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

        Console.WriteLine();
    }
}
