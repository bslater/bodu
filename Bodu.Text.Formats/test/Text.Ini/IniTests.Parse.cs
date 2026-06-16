// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> on empty input returns a document with an
    /// empty global section and no named sections.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsEmpty_ShouldReturnEmptyDocument()
    {
        IniDocument doc = Ini.Parse(string.Empty);

        Assert.IsNotNull(doc.GlobalSection);
        Assert.AreEqual(0, doc.GlobalSection.Entries.Count);
        Assert.AreEqual(0, doc.Sections.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> on whitespace-only input returns an empty
    /// document.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputIsWhitespaceOnly_ShouldReturnEmptyDocument()
    {
        IniDocument doc = Ini.Parse("   \n\t\n  ");

        Assert.AreEqual(0, doc.GlobalSection.Entries.Count);
        Assert.AreEqual(0, doc.Sections.Count);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> ignores lines whose first non-whitespace
    /// character is <c>;</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSemicolonCommentLine_ShouldSkipLine()
    {
        IniDocument doc = Ini.Parse("; this is a comment\nkey=value");

        Assert.AreEqual(1, doc.GlobalSection.Entries.Count);
        Assert.AreEqual("key", doc.GlobalSection.Entries[0].Key);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> ignores lines whose first non-whitespace
    /// character is <c>#</c>.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHashCommentLine_ShouldSkipLine()
    {
        IniDocument doc = Ini.Parse("# this is a comment\nkey=value");

        Assert.AreEqual(1, doc.GlobalSection.Entries.Count);
        Assert.AreEqual("key", doc.GlobalSection.Entries[0].Key);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> correctly parses a document with global
    /// entries and a named section.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalAndNamedSection_ShouldExposeGlobalAndSection()
    {
        const string source = "global_key=global_value\n[section]\nlocal_key=local_value";

        IniDocument doc = Ini.Parse(source);

        Assert.AreEqual("global_value", doc.GlobalSection["global_key"]);
        IniSection? section = doc.GetSection("section");
        Assert.IsNotNull(section);
        Assert.AreEqual("local_value", section["local_key"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> accepts both <c>=</c> and <c>:</c> as
    /// key/value separators.
    /// </summary>
    [TestMethod]
    public void Parse_WhenColonSeparator_ShouldParseEntryCorrectly()
    {
        IniDocument doc = Ini.Parse("[s]\nkey:value");

        IniSection? section = doc.GetSection("s");
        Assert.IsNotNull(section);
        Assert.AreEqual("value", section["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> trims leading and trailing whitespace from
    /// keys and values.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyValueHaveWhitespace_ShouldTrimBothSides()
    {
        IniDocument doc = Ini.Parse("[s]\n  key  =  value  ");

        IniSection? section = doc.GetSection("s");
        Assert.IsNotNull(section);
        Assert.AreEqual("value", section["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> correctly parses a key-only line (no separator)
    /// as a key with an empty value.
    /// </summary>
    [TestMethod]
    public void Parse_WhenKeyOnlyLine_ShouldProduceEntryWithEmptyValue()
    {
        IniDocument doc = Ini.Parse("[s]\nflag");

        IniSection? section = doc.GetSection("s");
        Assert.IsNotNull(section);
        Assert.AreEqual(string.Empty, section["flag"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> correctly parses a key with an empty value
    /// (key followed by separator and nothing).
    /// </summary>
    [TestMethod]
    public void Parse_WhenValueIsEmpty_ShouldProduceEntryWithEmptyValue()
    {
        IniDocument doc = Ini.Parse("[s]\nkey=");

        IniSection? section = doc.GetSection("s");
        Assert.IsNotNull(section);
        Assert.AreEqual(string.Empty, section["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> correctly handles Windows-style <c>\r\n</c>
    /// line endings.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCrLfLineEndings_ShouldParseCorrectly()
    {
        IniDocument doc = Ini.Parse("[section]\r\nkey=value\r\n");

        IniSection? section = doc.GetSection("section");
        Assert.IsNotNull(section);
        Assert.AreEqual("value", section["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> correctly handles <c>\r</c>-only line endings.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCrOnlyLineEndings_ShouldParseCorrectly()
    {
        IniDocument doc = Ini.Parse("[section]\rkey=value\r");

        IniSection? section = doc.GetSection("section");
        Assert.IsNotNull(section);
        Assert.AreEqual("value", section["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> correctly parses multiple named sections in
    /// source order.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultipleSections_ShouldExposeAllInOrder()
    {
        const string source = "[a]\nkey=1\n[b]\nkey=2\n[c]\nkey=3";

        IniDocument doc = Ini.Parse(source);

        Assert.AreEqual(3, doc.Sections.Count);
        Assert.AreEqual("a", doc.Sections[0].Name);
        Assert.AreEqual("b", doc.Sections[1].Name);
        Assert.AreEqual("c", doc.Sections[2].Name);
        Assert.AreEqual("1", doc.GetSection("a")!["key"]);
        Assert.AreEqual("2", doc.GetSection("b")!["key"]);
        Assert.AreEqual("3", doc.GetSection("c")!["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> trims whitespace from section names.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSectionNameHasWhitespace_ShouldTrimName()
    {
        IniDocument doc = Ini.Parse("[  MySection  ]\nkey=value");

        Assert.IsNotNull(doc.GetSection("MySection"));
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Parse(ReadOnlySpan{char})" /> correctly handles a string overload via
    /// implicit span conversion.
    /// </summary>
    [TestMethod]
    public void Parse_WhenStringInput_ShouldParseCorrectly()
    {
        string source = "[db]\nhost=localhost";

        IniDocument doc = Ini.Parse(source);

        Assert.IsNotNull(doc.GetSection("db"));
    }

}
