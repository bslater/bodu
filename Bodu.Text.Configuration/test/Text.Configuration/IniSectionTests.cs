// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSectionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Verifies <see cref="IniSection" />: entry lookup, typed access, mutation, and the comment trivia it preserves.
/// </summary>
/// <remarks>
/// The document model is what makes an edit-and-save round trip possible without rewriting the file, so a section
/// keeps both an ordered entry list and a keyed lookup and must never let the two disagree. Every mutating test below
/// therefore asserts against both views: an entry replaced through one must be visible - once - through the other.
/// </remarks>
[TestClass]
public class IniSectionTests
{
    /// <summary>
    /// Builds a section with the supplied key/value entries.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <param name="entries">The key/value pairs.</param>
    /// <returns>The section.</returns>
    private static IniSection Section(string name, params (string Key, string Value)[] entries) =>
        new(name, entries.Select(e => new IniEntry(e.Key, e.Value)));

    /// <summary>
    /// Verifies that a section built from entries exposes them in order and by key.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEntriesSupplied_ShouldExposeThemInOrderAndByKey()
    {
        IniSection section = Section("server", ("host", "localhost"), ("port", "8080"));

        Assert.AreEqual("server", section.Name);
        CollectionAssert.AreEqual(new[] { "host", "port" }, section.Entries.Select(e => e.Key).ToList());
        Assert.AreEqual("localhost", section["host"]);
        Assert.AreEqual("8080", section["port"]);
    }

    /// <summary>
    /// Verifies that a duplicate key replaces the earlier entry in place rather than appending a second one, so the
    /// ordered list and the lookup agree on which entry wins - the last one, matching how a reader collapses
    /// duplicates.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEntriesContainADuplicateKey_ShouldReplaceInPlace()
    {
        IniSection section = Section("server", ("host", "first"), ("port", "8080"), ("host", "second"));

        Assert.HasCount(2, section.Entries);
        CollectionAssert.AreEqual(new[] { "host", "port" }, section.Entries.Select(e => e.Key).ToList());
        Assert.AreEqual("second", section["host"]);
    }

    /// <summary>
    /// Verifies that keys are matched case-insensitively by default and case-sensitively on request, since the two
    /// dialects disagree and a section has to pick one.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCaseSensitivityVaries_ShouldMatchKeysAccordingly()
    {
        IniSection insensitive = Section("s", ("Host", "a"));
        Assert.AreEqual("a", insensitive["host"]);
        Assert.AreEqual("a", insensitive["HOST"]);

        var sensitive = new IniSection("s", [new IniEntry("Host", "a")], caseSensitiveKeys: true);
        Assert.AreEqual("a", sensitive["Host"]);
        Assert.IsNull(sensitive["host"]);
    }

    /// <summary>
    /// Verifies that a case-sensitive section keeps keys differing only in case as separate entries, where the
    /// default comparison would collapse them.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCaseSensitiveAndKeysDifferOnlyByCase_ShouldKeepBoth()
    {
        var sensitive = new IniSection("s", [new IniEntry("Host", "a"), new IniEntry("host", "b")], caseSensitiveKeys: true);

        Assert.HasCount(2, sensitive.Entries);
    }

    /// <summary>
    /// Verifies that the constructor rejects a null name, a null entry sequence, and a null element within it -
    /// the last of which would otherwise surface much later as a null reference during lookup.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenArgumentIsNull_ShouldThrow()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniSection(null!, []));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniSection("s", null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() => _ = new IniSection("s", [null!]));
    }

    /// <summary>
    /// Verifies that the indexer reports <see langword="null" /> for an absent key rather than throwing, since a
    /// missing setting is an ordinary outcome for a configuration file.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsAbsent_ShouldReturnNull()
    {
        Assert.IsNull(Section("server", ("host", "localhost"))["missing"]);
    }

    /// <summary>
    /// Verifies that typed access parses the stored text with invariant formatting, so a value does not change
    /// meaning with the machine's locale.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyExists_ShouldParseWithInvariantFormatting()
    {
        IniSection section = Section("s", ("port", "8080"), ("ratio", "1.5"));

        Assert.AreEqual(8080, section.GetValue<int>("port"));
        Assert.AreEqual(1.5d, section.GetValue<double>("ratio"));
    }

    /// <summary>
    /// Verifies that typed access to an absent key throws <see cref="KeyNotFoundException" /> naming the key, so the
    /// failure identifies the missing setting.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsAbsent_ShouldThrowKeyNotFound()
    {
        var ex = Assert.ThrowsExactly<KeyNotFoundException>(() => _ = Section("s", ("port", "8080")).GetValue<int>("missing"));

        Assert.Contains("missing", ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.GetValue{T}(string)" /> rejects a null key rather than treating it as a
    /// miss.
    /// </summary>
    [TestMethod]
    public void GetValue_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = Section("s").GetValue<int>(null!));
    }

    /// <summary>
    /// Verifies that the typed try-pattern reports failure for an absent key, a null key, and a value that will not
    /// parse - three different misses that a caller handles the same way.
    /// </summary>
    [TestMethod]
    public void TryGetValueOfT_WhenLookupOrParseFails_ShouldReturnFalse()
    {
        IniSection section = Section("s", ("port", "8080"), ("bad", "not-a-number"));

        Assert.IsTrue(section.TryGetValue("port", out int port));
        Assert.AreEqual(8080, port);

        Assert.IsFalse(section.TryGetValue("missing", out int _));
        Assert.IsFalse(section.TryGetValue(null!, out int _));
        Assert.IsFalse(section.TryGetValue("bad", out int _));
    }

    /// <summary>
    /// Verifies that the string try-pattern reports the raw text for a present key and fails for an absent or null
    /// one.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyIsPresentOrAbsent_ShouldReportAccordingly()
    {
        IniSection section = Section("s", ("host", "localhost"));

        Assert.IsTrue(section.TryGetValue("host", out string? value));
        Assert.AreEqual("localhost", value);

        Assert.IsFalse(section.TryGetValue("missing", out string? _));
        Assert.IsFalse(section.TryGetValue(null!, out string? _));
    }

    /// <summary>
    /// Verifies that the entry try-pattern hands back the entry itself, so a caller can reach its line number and
    /// comment trivia rather than only its value.
    /// </summary>
    [TestMethod]
    public void TryGetEntry_WhenKeyIsPresent_ShouldReportTheEntry()
    {
        IniSection section = Section("s", ("host", "localhost"));

        Assert.IsTrue(section.TryGetEntry("host", out IniEntry? entry));
        Assert.AreEqual("localhost", entry!.Value);

        Assert.IsFalse(section.TryGetEntry("missing", out IniEntry? _));
        Assert.IsFalse(section.TryGetEntry(null!, out IniEntry? _));
    }

    /// <summary>
    /// Verifies that adding an entry appends it and makes it reachable by key.
    /// </summary>
    [TestMethod]
    public void AddEntry_WhenKeyIsNew_ShouldAppendAndIndex()
    {
        IniSection section = Section("s", ("host", "localhost"));

        section.AddEntry(new IniEntry("port", "8080"));

        CollectionAssert.AreEqual(new[] { "host", "port" }, section.Entries.Select(e => e.Key).ToList());
        Assert.AreEqual("8080", section["port"]);
    }

    /// <summary>
    /// Verifies that adding an entry whose key already exists replaces it in place, keeping the section's order and
    /// its lookup consistent.
    /// </summary>
    [TestMethod]
    public void AddEntry_WhenKeyExists_ShouldReplaceInPlace()
    {
        IniSection section = Section("s", ("host", "first"), ("port", "8080"));

        section.AddEntry(new IniEntry("host", "second"));

        Assert.HasCount(2, section.Entries);
        CollectionAssert.AreEqual(new[] { "host", "port" }, section.Entries.Select(e => e.Key).ToList());
        Assert.AreEqual("second", section["host"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.SetEntry(string, string)" /> creates an entry when the key is new and
    /// updates it otherwise, returning the entry either way so the caller can attach trivia to it.
    /// </summary>
    [TestMethod]
    public void SetEntry_ShouldCreateOrUpdateAndReturnTheEntry()
    {
        IniSection section = Section("s");

        IniEntry created = section.SetEntry("host", "localhost");
        Assert.AreEqual("localhost", created.Value);
        Assert.HasCount(1, section.Entries);

        IniEntry updated = section.SetEntry("host", "example.com");
        Assert.AreEqual("example.com", updated.Value);
        Assert.HasCount(1, section.Entries);
        Assert.AreEqual("example.com", section["host"]);
    }

    /// <summary>
    /// Verifies that removing an entry drops it from both views and reports whether one was there.
    /// </summary>
    [TestMethod]
    public void RemoveEntry_ShouldRemoveFromBothViewsAndReportWhetherFound()
    {
        IniSection section = Section("s", ("host", "localhost"), ("port", "8080"));

        Assert.IsTrue(section.RemoveEntry("host"));
        Assert.HasCount(1, section.Entries);
        Assert.IsNull(section["host"]);

        Assert.IsFalse(section.RemoveEntry("host"));
    }

    /// <summary>
    /// Verifies that clearing empties both the ordered list and the lookup, so a cleared section does not answer a
    /// stale key.
    /// </summary>
    [TestMethod]
    public void ClearEntries_ShouldEmptyBothViews()
    {
        IniSection section = Section("s", ("host", "localhost"));

        section.ClearEntries();

        Assert.IsEmpty(section.Entries);
        Assert.IsNull(section["host"]);
    }

    /// <summary>
    /// Verifies that a section's leading comments can be appended, replaced wholesale, and cleared - the trivia that
    /// makes a save preserve the file's commentary rather than stripping it.
    /// </summary>
    [TestMethod]
    public void LeadingComments_ShouldSupportAppendReplaceAndClear()
    {
        IniSection section = Section("s");

        section.AddLeadingComment(new IniComment('#', " first"));
        Assert.HasCount(1, section.LeadingComments);

        section.SetLeadingComments([new IniComment(';', " a"), new IniComment(';', " b")]);
        Assert.HasCount(2, section.LeadingComments);
        Assert.AreEqual(" a", section.LeadingComments[0].Text);

        section.ClearLeadingComments();
        Assert.IsEmpty(section.LeadingComments);
    }

    /// <summary>
    /// Verifies that an entry carries the same comment trivia surface as a section, including an inline comment that
    /// trails the value on its own line.
    /// </summary>
    [TestMethod]
    public void IniEntry_ShouldCarryLeadingAndInlineComments()
    {
        var entry = new IniEntry("host", "localhost", lineNumber: 7);

        Assert.AreEqual(7, entry.LineNumber);
        Assert.IsEmpty(entry.LeadingComments);
        Assert.IsNull(entry.InlineComment);

        entry.AddLeadingComment(new IniComment('#', " why"));
        entry.InlineComment = new IniComment(';', " trailing");

        Assert.HasCount(1, entry.LeadingComments);
        Assert.AreEqual(" trailing", entry.InlineComment!.Value.Text);

        entry.SetLeadingComments([new IniComment('#', " replaced")]);
        Assert.AreEqual(" replaced", entry.LeadingComments[0].Text);

        entry.ClearLeadingComments();
        Assert.IsEmpty(entry.LeadingComments);
    }

    /// <summary>
    /// Verifies that a comment's identity includes its position: two comments match only when their prefix, text and
    /// line number all agree.
    /// </summary>
    /// <remarks>
    /// Including the line number is deliberate for a model whose purpose is to reproduce a file faithfully - where a
    /// comment sits is part of what is being preserved, so the same text on a different line is a different piece of
    /// trivia. It also means comment equality is not a way to ask "is this the same remark"; a caller wanting that
    /// compares <see cref="IniComment.Text" />.
    /// </remarks>
    [TestMethod]
    public void IniComment_ShouldCompareByPrefixTextAndLineNumber()
    {
        var first = new IniComment('#', " note", lineNumber: 1);
        var same = new IniComment('#', " note", lineNumber: 1);
        var moved = new IniComment('#', " note", lineNumber: 99);
        var reprefixed = new IniComment(';', " note", lineNumber: 1);

        Assert.AreEqual(first, same);
        Assert.IsTrue(first == same);
        Assert.AreEqual(first.GetHashCode(), same.GetHashCode());

        Assert.AreNotEqual(first, moved);
        Assert.IsTrue(first != moved);
        Assert.AreNotEqual(first, reprefixed);
        Assert.IsFalse(first.Equals("not a comment"));
    }

    /// <summary>
    /// Verifies that a comment renders as its prefix followed by its text, which is the form a save writes back.
    /// </summary>
    [TestMethod]
    public void IniComment_ToString_ShouldRenderPrefixAndText()
    {
        Assert.AreEqual("# note", new IniComment('#', " note").ToString());
    }
}
