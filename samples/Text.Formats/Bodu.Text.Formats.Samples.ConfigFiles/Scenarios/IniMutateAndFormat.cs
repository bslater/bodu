// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniMutateAndFormat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates the edit loop: parse an INI file, change values, add a section, and format the
/// document back to text — with every comment from the original file surviving the round trip.
/// This is the workflow for tooling that rewrites config files a human still owns.
/// </summary>
public static class IniMutateAndFormat
{
    /// <summary>
    /// Parses <c>Data/app.ini</c>, edits it, and re-emits the text.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- INI: mutate + format (comments survive) ---");

        var ini = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app.ini"));
        var document = Ini.Parse(ini);

        // Edit an existing entry, add a new one, and add a whole new section.
        document.GetSection("server")!.SetEntry("port", "9090");
        document.GetSection("logging")!.SetEntry("retention_days", "14");

        var metrics = document.GetOrAddSection("metrics");
        metrics.SetEntry("enabled", "true");
        metrics.SetEntry("endpoint", "/metrics");

        // Inline comments are a write-side feature: attach one and Format emits it.
        metrics.Entries.First(e => e.Key == "enabled").InlineComment = new IniComment(';', " scrape target");

        var emitted = Ini.Format(document);
        Console.WriteLine("edited document:");
        foreach (var line in emitted.TrimEnd().Split('\n'))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        // The comments from the source file are still there.
        var commentCount = emitted.Split('\n').Count(l => l.TrimStart().StartsWith(';'));
        Console.WriteLine($"comment lines preserved: {commentCount}");

        Console.WriteLine();
    }
}
