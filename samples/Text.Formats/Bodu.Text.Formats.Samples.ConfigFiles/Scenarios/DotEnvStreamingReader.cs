// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvStreamingReader.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.DotEnv;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates the forward-only <see cref="DotEnvReader" />: one entry in memory at a time,
/// with the line number attached — the surface for scanning env files without materializing a
/// document, e.g. a linter that flags suspicious keys as it streams.
/// </summary>
public static class DotEnvStreamingReader
{
    /// <summary>
    /// Streams <c>Data/env.sample</c> and reports each entry with its source line.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- DotEnv: streaming reader with line numbers ---");

        using var input = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Data", "env.sample"));
        using var reader = new DotEnvReader(input);

        while (reader.Read())
        {
            // A tiny lint pass: flag keys that look like they carry secrets.
            var flag = reader.Key.Contains("URL", StringComparison.Ordinal) ? "  <- check for embedded credentials" : string.Empty;
            Console.WriteLine($"  line {reader.LineNumber,2}: {reader.Key} = '{reader.Value}'{flag}");
        }

        Console.WriteLine();
    }
}
