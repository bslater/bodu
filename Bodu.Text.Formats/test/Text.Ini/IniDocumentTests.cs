// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        Assert.IsEmpty(doc.GlobalSection.Entries);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.GlobalSection" /> exposes keys that appeared before the first section
    /// header.
    /// </summary>
    [TestMethod]
    public void GlobalSection_WhenGlobalEntriesPresent_ShouldExposeEntries()
    {
        IniDocument doc = Ini.Parse("global_key=global_value\n[section]\nkey=value");

        Assert.HasCount(1, doc.GlobalSection.Entries);
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

        Assert.IsEmpty(doc.Sections);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.Sections" /> returns named sections in source order and does not
    /// include the global section.
    /// </summary>
    [TestMethod]
    public void Sections_WhenMultipleSections_ShouldExposeInSourceOrder()
    {
        IniDocument doc = Ini.Parse("[a]\n[b]\n[c]");

        string[] names = doc.Sections.Select(s => s.Name).ToArray();

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

        bool result = doc.TryGetSection("db", out IniSection? section);

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

        bool result = doc.TryGetSection("missing", out IniSection? section);

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

    // ------------------------------------------- construction ----------------------------------------------------

    /// <summary>
    /// Verifies that the global-and-sections constructor exposes the supplied global section and named sections.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenGivenGlobalAndSections_ShouldExposeThem()
    {
        IniSection global = new(string.Empty, Array.Empty<IniEntry>());
        IniSection db = new("db", [new IniEntry("host", "local")]);

        IniDocument doc = new(global, [db]);

        Assert.AreSame(global, doc.GlobalSection);
        Assert.AreEqual((1, "local"), (doc.Sections.Count, doc.GetSection("db")!["host"]));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> global section throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenGlobalSectionIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniDocument(null!, Array.Empty<IniSection>()));

        Assert.AreEqual("globalSection", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> sections sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSectionsIsNull_ShouldThrowArgumentNullException()
    {
        IniSection global = new(string.Empty, Array.Empty<IniEntry>());

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniDocument(global, null!));

        Assert.AreEqual("sections", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a global section with a non-empty name throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenGlobalSectionHasName_ShouldThrowArgumentException()
    {
        IniSection notGlobal = new("named", Array.Empty<IniEntry>());

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => _ = new IniDocument(notGlobal, Array.Empty<IniSection>()));

        Assert.AreEqual("globalSection", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> entry in the sections sequence throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSectionsContainNull_ShouldThrowArgumentException()
    {
        IniSection global = new(string.Empty, Array.Empty<IniEntry>());

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => _ = new IniDocument(global, [null!]));

        Assert.AreEqual("sections", ex.ParamName);
    }

    // ------------------------------------------- mutation --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniDocument.AddSection(IniSection)" /> appends a named section.
    /// </summary>
    [TestMethod]
    public void AddSection_WhenNamed_ShouldAppend()
    {
        IniDocument doc = new();

        doc.AddSection(new IniSection("db", Array.Empty<IniEntry>()));

        Assert.HasCount(1, doc.Sections);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.AddSection(IniSection)" /> rejects the global (empty-named) section.
    /// </summary>
    [TestMethod]
    public void AddSection_WhenSectionIsGlobal_ShouldThrowArgumentException()
    {
        IniDocument doc = new();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => doc.AddSection(new IniSection(string.Empty, Array.Empty<IniEntry>())));

        Assert.AreEqual("section", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.RemoveSection(string)" /> removes a present section and returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void RemoveSection_WhenPresent_ShouldRemoveAndReturnTrue()
    {
        IniDocument doc = new();
        doc.AddSection(new IniSection("db", Array.Empty<IniEntry>()));

        Assert.IsTrue(doc.RemoveSection("db"));
        Assert.IsEmpty(doc.Sections);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.RemoveSection(string)" /> returns <see langword="false" /> for an absent
    /// section name.
    /// </summary>
    [TestMethod]
    public void RemoveSection_WhenAbsent_ShouldReturnFalse()
    {
        IniDocument doc = new();

        Assert.IsFalse(doc.RemoveSection("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.GetOrAddSection(string)" /> rejects an empty section name.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_WhenNameIsEmpty_ShouldThrowArgumentException()
    {
        IniDocument doc = new();

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => _ = doc.GetOrAddSection(string.Empty));

        Assert.AreEqual("name", ex.ParamName);
    }
}
