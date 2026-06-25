// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSectionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Behavioural tests for <see cref="IniSection" />.
/// </summary>
[TestClass]
public sealed class IniSectionTests
{

    // -------------------------------------------------- helpers ---------------------------------------------------

    private static IniSection ParseSection(string sectionName, string iniText, IniParseOptions? options = null)
    {
        IniDocument doc = Ini.Parse(iniText, options ?? IniParseOptions.Default);

        if (string.IsNullOrEmpty(sectionName))
            return doc.GlobalSection;

        IniSection? section = doc.GetSection(sectionName);
        Assert.IsNotNull(section, $"Section '{sectionName}' not found in parsed document.");
        return section;
    }

    // -------------------------------------------------- indexer ---------------------------------------------------

    /// <summary>
    /// Verifies that the indexer returns the correct string value for a present key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsPresent_ShouldReturnValue()
    {
        IniSection section = ParseSection("db", "[db]\nhost=localhost");

        Assert.AreEqual("localhost", section["host"]);
    }

    /// <summary>
    /// Verifies that the indexer returns <see langword="null" /> when the key is absent.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsAbsent_ShouldReturnNull()
    {
        IniSection section = ParseSection("db", "[db]\nhost=localhost");

        Assert.IsNull(section["port"]);
    }

    /// <summary>
    /// Verifies that the indexer returns <see langword="null" /> for a null key without throwing.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldReturnNull()
    {
        IniSection section = ParseSection("db", "[db]\nhost=localhost");

        Assert.IsNull(section[null!]);
    }

    // -------------------------------------------- TryGetValue (string) -------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetValue(string, out string)" /> returns <see langword="true" />
    /// and the value for a present key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsPresent_ShouldReturnTrueAndValue()
    {
        IniSection section = ParseSection("s", "[s]\nkey=val");

        bool result = section.TryGetValue("key", out string? value);

        Assert.IsTrue(result);
        Assert.AreEqual("val", value);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetValue(string, out string)" /> returns <see langword="false" />
    /// for an absent key.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsAbsent_ShouldReturnFalse()
    {
        IniSection section = ParseSection("s", "[s]\nkey=val");

        bool result = section.TryGetValue("missing", out string? value);

        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that key lookup is case-insensitive under the default options.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyDiffersByCase_ShouldMatchByDefault()
    {
        IniSection section = ParseSection("s", "[s]\nPort=443");

        bool result = section.TryGetValue("port", out string? value);

        Assert.IsTrue(result);
        Assert.AreEqual("443", value);
    }

    /// <summary>
    /// Verifies that key lookup is case-sensitive when <see cref="IniParseOptions.CaseSensitiveKeys" /> is
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenCaseSensitiveAndKeyDiffersByCase_ShouldReturnFalse()
    {
        IniParseOptions options = new() { CaseSensitiveKeys = true };
        IniSection section = ParseSection("s", "[s]\nPort=443", options);

        bool result = section.TryGetValue("port", out string? value);

        Assert.IsFalse(result);
        Assert.IsNull(value);
    }

    // --------------------------------------------- GetValue<T> ---------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniSection.GetValue{T}(string)" /> returns the parsed integer for a present key.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsPresentAndValueIsValidInt_ShouldReturnParsedValue()
    {
        IniSection section = ParseSection("s", "[s]\ncount=42");

        int result = section.GetValue<int>("count");

        Assert.AreEqual(42, result);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.GetValue{T}(string)" /> throws <see cref="ArgumentNullException" />
    /// when the key is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsNull_ShouldThrowExactly()
    {
        IniSection section = ParseSection("s", "[s]\ncount=42");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = section.GetValue<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.GetValue{T}(string)" /> throws <see cref="KeyNotFoundException" />
    /// when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsAbsent_ShouldThrowExactly()
    {
        IniSection section = ParseSection("s", "[s]\ncount=42");

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = section.GetValue<int>("missing");
        });
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.GetValue{T}(string)" /> throws <see cref="FormatException" /> when
    /// the value cannot be parsed as <typeparamref name="T" />.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenValueIsInvalidForTargetType_ShouldThrowExactly()
    {
        IniSection section = ParseSection("s", "[s]\ncount=notanumber");

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = section.GetValue<int>("count");
        });
    }

    // ------------------------------------------- TryGetValue<T> --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetValue{T}(string, out T)" /> returns <see langword="true" />
    /// and sets the output for a valid key and parseable value.
    /// </summary>
    [TestMethod]
    public void TryGetValueTyped_WhenKeyIsPresentAndValueIsValid_ShouldReturnTrueAndValue()
    {
        IniSection section = ParseSection("s", "[s]\nport=8080");

        bool result = section.TryGetValue<int>("port", out int value);

        Assert.IsTrue(result);
        Assert.AreEqual(8080, value);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetValue{T}(string, out T)" /> returns <see langword="false" />
    /// for an absent key.
    /// </summary>
    [TestMethod]
    public void TryGetValueTyped_WhenKeyIsAbsent_ShouldReturnFalse()
    {
        IniSection section = ParseSection("s", "[s]\nport=8080");

        bool result = section.TryGetValue<int>("missing", out int value);

        Assert.IsFalse(result);
        Assert.AreEqual(default, value);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetValue{T}(string, out T)" /> returns <see langword="false" />
    /// for a null key without throwing.
    /// </summary>
    [TestMethod]
    public void TryGetValueTyped_WhenKeyIsNull_ShouldReturnFalseWithoutThrowing()
    {
        IniSection section = ParseSection("s", "[s]\nport=8080");

        bool result = section.TryGetValue<int>(null!, out int value);

        Assert.IsFalse(result);
        Assert.AreEqual(default, value);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetValue{T}(string, out T)" /> returns <see langword="false" />
    /// when the value cannot be parsed as the target type.
    /// </summary>
    [TestMethod]
    public void TryGetValueTyped_WhenValueIsInvalidForTargetType_ShouldReturnFalse()
    {
        IniSection section = ParseSection("s", "[s]\nport=notanumber");

        bool result = section.TryGetValue<int>("port", out int value);

        Assert.IsFalse(result);
        Assert.AreEqual(default, value);
    }

    // ---------------------------------------------- Entries ------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniSection.Entries" /> exposes entries in source order.
    /// </summary>
    [TestMethod]
    public void Entries_WhenParsed_ShouldExposeEntriesInSourceOrder()
    {
        IniSection section = ParseSection("s", "[s]\na=1\nb=2\nc=3");

        string[] keys = section.Entries.Select(e => e.Key).ToArray();

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, keys);
    }

    // ---------------------------------------------- Name ---------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniSection.Name" /> returns the empty string for the global section.
    /// </summary>
    [TestMethod]
    public void Name_WhenGlobalSection_ShouldBeEmptyString()
    {
        IniDocument doc = Ini.Parse("key=value");

        Assert.AreEqual(string.Empty, doc.GlobalSection.Name);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.Name" /> returns the exact section name as it appeared in the source.
    /// </summary>
    [TestMethod]
    public void Name_WhenNamedSection_ShouldMatchSourceName()
    {
        IniSection section = ParseSection("MySection", "[MySection]\nkey=value");

        Assert.AreEqual("MySection", section.Name);
    }

    // ------------------------------------------- programmatic construction ----------------------------------------

    /// <summary>
    /// Verifies that constructing a section with duplicate keys resolves them last-wins.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEntriesContainDuplicateKey_ShouldKeepLastWins()
    {
        IniSection section = new("s", [new IniEntry("k", "1"), new IniEntry("k", "2")]);

        Assert.AreEqual((1, "2"), (section.Entries.Count, section["k"]));
    }

    /// <summary>
    /// Verifies that constructing a section with a <see langword="null" /> entry throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenEntriesContainNull_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() => _ = new IniSection("s", [null!]));

        Assert.AreEqual("entries", ex.ParamName);
    }

    // ------------------------------------------- mutation -----------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetEntry(string, out IniEntry)" /> returns the entry for a present key.
    /// </summary>
    [TestMethod]
    public void TryGetEntry_WhenKeyPresent_ShouldReturnTrueAndEntry()
    {
        IniSection section = new("s", [new IniEntry("host", "local")]);

        Assert.IsTrue(section.TryGetEntry("host", out IniEntry? entry));
        Assert.AreEqual("local", entry!.Value);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.TryGetEntry(string, out IniEntry)" /> returns <see langword="false" /> for a
    /// <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void TryGetEntry_WhenKeyIsNull_ShouldReturnFalse()
    {
        IniSection section = new("s", Array.Empty<IniEntry>());

        Assert.IsFalse(section.TryGetEntry(null!, out IniEntry? entry));
        Assert.IsNull(entry);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.AddEntry(IniEntry)" /> replaces an existing entry that shares the key.
    /// </summary>
    [TestMethod]
    public void AddEntry_WhenKeyExists_ShouldReplaceInPlace()
    {
        IniSection section = new("s", [new IniEntry("k", "1")]);

        section.AddEntry(new IniEntry("k", "2"));

        Assert.AreEqual((1, "2"), (section.Entries.Count, section["k"]));
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.RemoveEntry(string)" /> returns <see langword="false" /> for an absent key.
    /// </summary>
    [TestMethod]
    public void RemoveEntry_WhenKeyAbsent_ShouldReturnFalse()
    {
        IniSection section = new("s", Array.Empty<IniEntry>());

        Assert.IsFalse(section.RemoveEntry("missing"));
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.ClearEntries" /> removes every entry.
    /// </summary>
    [TestMethod]
    public void ClearEntries_ShouldRemoveAllEntries()
    {
        IniSection section = new("s", [new IniEntry("a", "1"), new IniEntry("b", "2")]);

        section.ClearEntries();

        Assert.IsEmpty(section.Entries);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.ClearLeadingComments" /> removes every leading comment.
    /// </summary>
    [TestMethod]
    public void ClearLeadingComments_ShouldRemoveAllLeadingComments()
    {
        IniSection section = new("s", Array.Empty<IniEntry>());
        section.AddLeadingComment(new IniComment('#', " note"));

        section.ClearLeadingComments();

        Assert.IsEmpty(section.LeadingComments);
    }
}
