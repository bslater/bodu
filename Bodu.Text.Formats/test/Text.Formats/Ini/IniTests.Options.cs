// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.Options.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.Ini;

public sealed partial class IniTests
{

    // ---------------------------------------- DuplicateKeyBehavior ----------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniDuplicateKeyBehavior.LastWins" /> causes the last value to win when the same
    /// key appears more than once in a section.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyLastWins_ShouldReturnLastValue()
    {
        IniParseOptions options = new() { DuplicateKeyBehavior = IniDuplicateKeyBehavior.LastWins };

        IniDocument doc = Ini.Parse("[s]\nkey=first\nkey=second", options);

        Assert.AreEqual("second", doc.GetSection("s")!["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniDuplicateKeyBehavior.LastWins" /> produces exactly one entry for a duplicated
    /// key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyLastWins_ShouldProduceSingleEntry()
    {
        IniParseOptions options = new() { DuplicateKeyBehavior = IniDuplicateKeyBehavior.LastWins };

        IniDocument doc = Ini.Parse("[s]\nkey=first\nkey=second", options);

        Assert.AreEqual(1, doc.GetSection("s")!.Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IniDuplicateKeyBehavior.FirstWins" /> causes the first value to be retained when
    /// the same key appears more than once in a section.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyFirstWins_ShouldReturnFirstValue()
    {
        IniParseOptions options = new() { DuplicateKeyBehavior = IniDuplicateKeyBehavior.FirstWins };

        IniDocument doc = Ini.Parse("[s]\nkey=first\nkey=second", options);

        Assert.AreEqual("first", doc.GetSection("s")!["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniDuplicateKeyBehavior.FirstWins" /> produces exactly one entry for a duplicated
    /// key.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyFirstWins_ShouldProduceSingleEntry()
    {
        IniParseOptions options = new() { DuplicateKeyBehavior = IniDuplicateKeyBehavior.FirstWins };

        IniDocument doc = Ini.Parse("[s]\nkey=first\nkey=second", options);

        Assert.AreEqual(1, doc.GetSection("s")!.Entries.Count);
    }

    // --------------------------------------- DuplicateSectionBehavior -------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniDuplicateSectionBehavior.Merge" /> (the default) merges entries from the
    /// second occurrence of a section into the first.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionMerge_ShouldCombineEntries()
    {
        IniDocument doc = Ini.Parse("[s]\na=1\n[s]\nb=2");

        IniSection? section = doc.GetSection("s");
        Assert.IsNotNull(section);
        Assert.AreEqual("1", section["a"]);
        Assert.AreEqual("2", section["b"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniDuplicateSectionBehavior.Merge" /> results in a single section in
    /// <see cref="IniDocument.Sections" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionMerge_ShouldProduceSingleSection()
    {
        IniDocument doc = Ini.Parse("[s]\na=1\n[s]\nb=2");

        Assert.AreEqual(1, doc.Sections.Count);
    }

    /// <summary>
    /// Verifies that when duplicate sections are merged, the active <see cref="IniDuplicateKeyBehavior" />
    /// governs key conflicts introduced by the merge.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMergeSectionWithDuplicateKeyLastWins_ShouldReturnLastValue()
    {
        IniParseOptions options = new() { DuplicateKeyBehavior = IniDuplicateKeyBehavior.LastWins };

        IniDocument doc = Ini.Parse("[s]\nkey=first\n[s]\nkey=second", options);

        Assert.AreEqual("second", doc.GetSection("s")!["key"]);
    }

    // ---------------------------------------- CaseSensitiveKeys -------------------------------------------------

    /// <summary>
    /// Verifies that with <see cref="IniParseOptions.CaseSensitiveKeys" /> disabled (the default), keys that
    /// differ only in case are treated as equal.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCaseInsensitiveKeys_ShouldTreatSameKeyWithDifferentCaseAsEqual()
    {
        IniParseOptions options = new()
        {
            CaseSensitiveKeys = false,
            DuplicateKeyBehavior = IniDuplicateKeyBehavior.FirstWins,
        };

        IniDocument doc = Ini.Parse("[s]\nKey=first\nKEY=second", options);

        Assert.AreEqual(1, doc.GetSection("s")!.Entries.Count);
        Assert.AreEqual("first", doc.GetSection("s")!["key"]);
    }

    /// <summary>
    /// Verifies that with <see cref="IniParseOptions.CaseSensitiveKeys" /> enabled, keys that differ only in
    /// case are treated as distinct.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCaseSensitiveKeys_ShouldTreatSameKeyWithDifferentCaseAsDistinct()
    {
        IniParseOptions options = new() { CaseSensitiveKeys = true };

        IniDocument doc = Ini.Parse("[s]\nKey=first\nKEY=second", options);

        Assert.AreEqual(2, doc.GetSection("s")!.Entries.Count);
        Assert.AreEqual("first", doc.GetSection("s")!["Key"]);
        Assert.AreEqual("second", doc.GetSection("s")!["KEY"]);
    }

    // -------------------------------------- CaseSensitiveSections -----------------------------------------------

    /// <summary>
    /// Verifies that with <see cref="IniParseOptions.CaseSensitiveSections" /> disabled (the default), section
    /// names that differ only in case are merged.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCaseInsensitiveSections_ShouldMergeSectionsWithDifferentCase()
    {
        IniDocument doc = Ini.Parse("[Section]\na=1\n[SECTION]\nb=2");

        Assert.AreEqual(1, doc.Sections.Count);
        IniSection? section = doc.GetSection("section");
        Assert.IsNotNull(section);
        Assert.AreEqual("1", section["a"]);
        Assert.AreEqual("2", section["b"]);
    }

    /// <summary>
    /// Verifies that with <see cref="IniParseOptions.CaseSensitiveSections" /> enabled, section names that differ
    /// only in case are treated as distinct sections.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCaseSensitiveSections_ShouldTreatSectionsWithDifferentCaseAsDistinct()
    {
        IniParseOptions options = new() { CaseSensitiveSections = true };

        IniDocument doc = Ini.Parse("[Section]\na=1\n[SECTION]\nb=2", options);

        Assert.AreEqual(2, doc.Sections.Count);
        Assert.IsNotNull(doc.GetSection("Section"));
        Assert.IsNotNull(doc.GetSection("SECTION"));
    }

    // ----------------------------------------- AllowGlobalSection -----------------------------------------------

    /// <summary>
    /// Verifies that with <see cref="IniParseOptions.AllowGlobalSection" /> enabled (the default), keys before
    /// the first section header are collected into <see cref="IniDocument.GlobalSection" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalSectionAllowed_ShouldCollectPreSectionKeys()
    {
        IniDocument doc = Ini.Parse("g=global\n[s]\nk=v");

        Assert.AreEqual("global", doc.GlobalSection["g"]);
    }

    /// <summary>
    /// Verifies that with <see cref="IniParseOptions.AllowGlobalSection" /> disabled, keys that appear before
    /// the first section header cause an <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalSectionDisallowed_ShouldThrowOnPreSectionKey()
    {
        IniParseOptions options = new() { AllowGlobalSection = false };

        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = Ini.Parse("g=global\n[s]\nk=v", options);
        });
    }

    /// <summary>
    /// Verifies that with <see cref="IniParseOptions.AllowGlobalSection" /> disabled, a source that begins
    /// directly with a section header is accepted.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalSectionDisallowedAndSourceStartsWithHeader_ShouldSucceed()
    {
        IniParseOptions options = new() { AllowGlobalSection = false };

        IniDocument doc = Ini.Parse("[s]\nk=v", options);

        Assert.IsNotNull(doc.GetSection("s"));
    }

}
