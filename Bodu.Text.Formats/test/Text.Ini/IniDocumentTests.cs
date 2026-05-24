// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Behavioural tests for <see cref="IniDocument" />.
/// </summary>
[TestClass]
public sealed class IniDocumentTests
{

    // ------------------------------------------- GlobalSection ---------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniDocument.GlobalSection" /> is never <see langword="null" />, even when the
    /// source contains no global entries.
    /// </summary>
    [TestMethod]
    public void GlobalSection_WhenNoGlobalEntries_ShouldBeEmptyNotNull()
    {
        IniDocument doc = Ini.Parse("[section]\nkey=value");

        Assert.IsNotNull(doc.GlobalSection);
        Assert.AreEqual(0, doc.GlobalSection.Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.GlobalSection" /> exposes keys that appeared before the first section
    /// header.
    /// </summary>
    [TestMethod]
    public void GlobalSection_WhenGlobalEntriesPresent_ShouldExposeEntries()
    {
        IniDocument doc = Ini.Parse("global_key=global_value\n[section]\nkey=value");

        Assert.AreEqual(1, doc.GlobalSection.Entries.Count);
        Assert.AreEqual("global_key", doc.GlobalSection.Entries[0].Key);
    }

    // ----------------------------------------------- Sections ---------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniDocument.Sections" /> is empty when the source has no named section headers.
    /// </summary>
    [TestMethod]
    public void Sections_WhenNoNamedSections_ShouldBeEmpty()
    {
        IniDocument doc = Ini.Parse("key=value");

        Assert.AreEqual(0, doc.Sections.Count);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.Sections" /> returns named sections in source order and does not
    /// include the global section.
    /// </summary>
    [TestMethod]
    public void Sections_WhenMultipleSections_ShouldExposeInSourceOrder()
    {
        IniDocument doc = Ini.Parse("[a]\n[b]\n[c]");

        var names = doc.Sections.Select(s => s.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, names);
    }

    // --------------------------------------------- GetSection ---------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniDocument.GetSection(string)" /> returns the correct section for a present name.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenSectionIsPresent_ShouldReturnSection()
    {
        IniDocument doc = Ini.Parse("[server]\nhost=example.com");

        IniSection? section = doc.GetSection("server");

        Assert.IsNotNull(section);
        Assert.AreEqual("server", section.Name);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.GetSection(string)" /> returns <see langword="null" /> when the
    /// section is absent.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenSectionIsAbsent_ShouldReturnNull()
    {
        IniDocument doc = Ini.Parse("[server]\nhost=example.com");

        IniSection? section = doc.GetSection("missing");

        Assert.IsNull(section);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.GetSection(string)" /> throws <see cref="ArgumentNullException" />
    /// when the name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenNameIsNull_ShouldThrowExactly()
    {
        IniDocument doc = Ini.Parse("[server]\nhost=example.com");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = doc.GetSection(null!);
        });
    }

    /// <summary>
    /// Verifies that section lookup is case-insensitive under default options.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenNameDiffersByCase_ShouldMatchByDefault()
    {
        IniDocument doc = Ini.Parse("[Server]\nhost=example.com");

        IniSection? section = doc.GetSection("server");

        Assert.IsNotNull(section);
    }

    // -------------------------------------------- TryGetSection -------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniDocument.TryGetSection(string, out IniSection)" /> returns
    /// <see langword="true" /> and sets the output for a present section.
    /// </summary>
    [TestMethod]
    public void TryGetSection_WhenSectionIsPresent_ShouldReturnTrueAndSection()
    {
        IniDocument doc = Ini.Parse("[db]\nhost=localhost");

        var result = doc.TryGetSection("db", out IniSection? section);

        Assert.IsTrue(result);
        Assert.IsNotNull(section);
        Assert.AreEqual("db", section.Name);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.TryGetSection(string, out IniSection)" /> returns
    /// <see langword="false" /> for an absent section.
    /// </summary>
    [TestMethod]
    public void TryGetSection_WhenSectionIsAbsent_ShouldReturnFalse()
    {
        IniDocument doc = Ini.Parse("[db]\nhost=localhost");

        var result = doc.TryGetSection("missing", out IniSection? section);

        Assert.IsFalse(result);
        Assert.IsNull(section);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.TryGetSection(string, out IniSection)" /> throws
    /// <see cref="ArgumentNullException" /> when the name is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetSection_WhenNameIsNull_ShouldThrowExactly()
    {
        IniDocument doc = Ini.Parse("[db]\nhost=localhost");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = doc.TryGetSection(null!, out _);
        });
    }

}
