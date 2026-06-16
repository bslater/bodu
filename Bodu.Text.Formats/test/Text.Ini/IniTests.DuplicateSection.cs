// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.DuplicateSection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{
    /// <summary>
    /// Verifies that the default <see cref="IniDuplicateSectionBehavior.Merge" /> value continues to merge all
    /// duplicate sections into the first occurrence (the historical INI behaviour).
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionBehaviorIsMerge_ShouldMergeAllOccurrencesIntoFirst()
    {
        string source = "[s]\na = 1\n[s]\nb = 2\n[s]\nc = 3\n";

        IniDocument doc = Ini.Parse(source);

        Assert.AreEqual(1, doc.Sections.Count);
        Assert.AreEqual(3, doc.Sections[0].Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IniDuplicateSectionBehavior.MergeAll" /> is an explicit alias for
    /// <see cref="IniDuplicateSectionBehavior.Merge" /> and produces identical output.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionBehaviorIsMergeAll_ShouldBehaveAsMerge()
    {
        string source = "[s]\na = 1\n[s]\nb = 2\n";

        IniDocument merged = Ini.Parse(source, new IniParseOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Merge });
        IniDocument mergedAll = Ini.Parse(source, new IniParseOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.MergeAll });

        Assert.AreEqual(merged.Sections.Count, mergedAll.Sections.Count);
        Assert.AreEqual(merged.Sections[0].Entries.Count, mergedAll.Sections[0].Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IniDuplicateSectionBehavior.Preserve" /> retains every duplicate section as a
    /// separate, ordered occurrence so downstream consumers can apply their own precedence rules.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionBehaviorIsPreserve_ShouldRetainSeparateOccurrences()
    {
        string source = "[s]\na = 1\n[s]\nb = 2\n";

        IniDocument doc = Ini.Parse(source, new IniParseOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Preserve });

        Assert.AreEqual(2, doc.Sections.Count);
        Assert.AreEqual(1, doc.Sections[0].Entries.Count);
        Assert.AreEqual(1, doc.Sections[1].Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IniDuplicateSectionBehavior.MergeAdjacent" /> merges only when the previous
    /// section had the same name. Non-adjacent duplicates remain separate.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionBehaviorIsMergeAdjacent_ShouldOnlyMergeAdjacentDuplicates()
    {
        string source = "[s]\na = 1\n[s]\nb = 2\n[other]\nc = 3\n[s]\nd = 4\n";

        IniDocument doc = Ini.Parse(source, new IniParseOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.MergeAdjacent });

        // First two [s] are merged (adjacent), [other] separates them from the third [s] which stays standalone.
        Assert.AreEqual(3, doc.Sections.Count);
        Assert.AreEqual("s", doc.Sections[0].Name);
        Assert.AreEqual(2, doc.Sections[0].Entries.Count);
        Assert.AreEqual("other", doc.Sections[1].Name);
        Assert.AreEqual("s", doc.Sections[2].Name);
        Assert.AreEqual(1, doc.Sections[2].Entries.Count);
    }
}
