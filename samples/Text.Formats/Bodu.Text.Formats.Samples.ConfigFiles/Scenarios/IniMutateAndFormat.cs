// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniMutateAndFormat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Ini.Nodes;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates the edit loop on the mutable, trivia-bearing <see cref="IniNode" /> DOM: parse an INI file, change
/// values, add a section with a comment, and write the document back to text — with every comment from the original
/// file surviving the round trip. This is the workflow for tooling that rewrites config files a human still owns.
/// </summary>
public static class IniMutateAndFormat
{
    /// <summary>
    /// Parses <c>Data/app.ini</c>, edits it, and re-emits the text.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- INI: mutate + write (comments survive) ---");

        var iniBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "app.ini"));
        var root = IniNode.Parse(iniBytes);

        // Edit an existing entry, add a new one, and add a whole new section.
        root["server"].AsObject()["port"].AsValue().Value = "9090";
        root["logging"].AsObject()["retention_days"] = new IniValue("14");

        var metrics = new IniObject();
        var enabled = new IniValue("true");
        enabled.LeadingComments.Add(" scrape target");
        metrics["enabled"] = enabled;
        metrics["endpoint"] = new IniValue("/metrics");
        root["metrics"] = metrics;

        var emitted = Encoding.UTF8.GetString(root.ToUtf8Bytes());
        Console.WriteLine("edited document:");
        foreach (var line in emitted.TrimEnd().Split('\n'))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        // The comments from the source file are still there, plus the one we authored.
        var commentCount = emitted.Split('\n').Count(l => l.TrimStart().StartsWith(';'));
        Console.WriteLine($"comment lines emitted: {commentCount}");

        Console.WriteLine();
    }
}
