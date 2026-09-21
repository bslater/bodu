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
        SampleConsole.Scenario(
            "INI - editing a file a human still owns",
            what: "Parses the INI file into the mutable DOM, changes an existing value, adds a key to an existing "
                + "section, appends a new section carrying an authored comment, prints the emitted text, and "
                + "counts the comment lines in the result.",
            why: "Config files that tooling rewrites are usually files a person also edits, and the comments in "
                + "them are the only record of why a setting has the value it does. A DOM that discards trivia "
                + "makes every automated edit destructive, so the fix on Monday erases the explanation written on "
                + "Friday. This DOM is deliberately trivia-bearing for that reason - the one sanctioned deviation "
                + "from the trivia-free document models its sibling formats use - and comments are addressable, "
                + "so a tool can also explain the entry it just added rather than leaving an unexplained value.",
            expect: "Every comment from the source file is still in the emitted text, plus the one authored here "
                + "above the new entry. The edited value and the added section appear in place, with the "
                + "untouched parts of the file unchanged - an edit should be a diff of what was asked for, not a "
                + "reformat.");

        var iniBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "app.ini"));
        var root = IniNode.Parse(iniBytes);

        // Edit an existing entry, add a new one, and add a whole new section.
        root["server"].AsObject()["port"].AsValue().Value = "9090";
        root["logging"].AsObject()["retention_days"] = new IniValue("14");

        var metrics = new IniObject();
        var enabled = new IniValue("true");

        // LeadingComments is the comment-trivia surface: this text emits as a ';' line above the entry.
        enabled.LeadingComments.Add(" scrape target");
        metrics["enabled"] = enabled;
        metrics["endpoint"] = new IniValue("/metrics");
        root["metrics"] = metrics;

        var emitted = Encoding.UTF8.GetString(root.ToUtf8Bytes());
        Console.WriteLine("  edited document:");
        foreach (var line in emitted.TrimEnd().Split('\n'))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        // The comments from the source file are still there, plus the one we authored.
        var commentCount = emitted.Split('\n').Count(l => l.TrimStart().StartsWith(';'));
        Console.WriteLine($"  comment lines emitted: {commentCount}"
            + "  (the source file's comments plus the one authored above [metrics].enabled - an automated edit that erases them is a destructive edit)");

        Console.WriteLine();
    }
}
