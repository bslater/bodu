// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SaveRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Samples.Text.Configuration.ConfigCascade.Scenarios;

/// <summary>
/// Demonstrates the write phase: the parsed document is the INI document model underneath, so
/// existing sections mutate in place, a rebuilt <see cref="IniDocument" /> can append new ones,
/// and <c>ConfigurationDocument.Save</c> writes the result — comments preserved, the file still
/// owned by the human who wrote it.
/// </summary>
public static class SaveRoundTrip
{
    /// <summary>
    /// Edits a section, appends a new one, saves to a temp file, and re-resolves from it.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Saving - mutate, append, write, and re-resolve",
            what: "Edits a value in an existing section, composes a new document with an extra section appended, "
                + "saves it, counts the comment lines that survived, and resolves a path from the written file.",
            why: "A configuration file a program writes back is usually a file a person also edits, and the "
                + "failure mode is a tool that reformats or silently strips the comments explaining why a setting "
                + "is there. This document model is trivia-preserving underneath, so an edit changes the value it "
                + "was asked to change and leaves everything else - including comments and spacing - as the "
                + "author wrote it. The read-only surface deliberately does not grow sections: adding one is a "
                + "structural change, so it goes through composing a document rather than mutating in place, "
                + "which keeps accidental structural edits out of the common path.",
            expect: "The comment lines are still in the saved file - the edit touched one value, not the "
                + "formatting around it. Re-loading the written file and resolving a path that only the appended "
                + "section matches proves the round trip produced a file the parser accepts, not just text that "
                + "looks right.");

        var document = ConfigurationDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "sample.boduconfig"));

        // Sections are mutable IniSection instances - edit one in place.
        document.GetSection("src/**.cs")!.SetEntry("max_line_length", "110");

        // The read-only document surface doesn't grow sections; compose a new IniDocument
        // from the existing parts plus the appended section, then Save that.
        var docsSection = new IniSection("docs/**.md", new[]
        {
            new IniEntry("max_line_length", "80"),
            new IniEntry("trim_trailing_whitespace", "true"),
        });
        var edited = new IniDocument(document.GlobalSection, document.Sections.Append(docsSection));

        var savedPath = Path.Combine(Path.GetTempPath(), "bodu-sample.boduconfig");
        ConfigurationDocument.Save(edited, savedPath);

        var savedText = File.ReadAllText(savedPath);
        var commentLines = savedText.Split('\n').Count(l => l.TrimStart().StartsWith('#'));
        Console.WriteLine($"  saved {savedText.Length} chars to {Path.GetFileName(savedPath)}; comment lines preserved: {commentLines}"
            + "  (the comments the author wrote survive the edit - a config writer that strips them is a config writer people turn off)");

        // The saved file resolves like the original, plus the new section.
        var reloaded = ConfigurationDocument.Load(savedPath);
        var view = reloaded.Resolve("docs/guide/intro.md");
        Console.WriteLine($"  re-resolved docs/guide/intro.md: max_line_length = {view.GetInt32("max_line_length")}"
            + "  (the value comes from the appended section, so the written file really parses - not just looks right)");

        File.Delete(savedPath);

        Console.WriteLine();
    }
}
