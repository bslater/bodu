// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniTests.Mutate.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public sealed partial class IniTests
{
    /// <summary>
    /// Verifies that <see cref="IniEntry.Value" /> is mutable, allowing in-place edits between parse and
    /// format.
    /// </summary>
    [TestMethod]
    public void Mutate_WhenEntryValueIsAssigned_ShouldRoundTripUpdatedValue()
    {
        IniDocument doc = Ini.Parse("[s]\nkey = old\n");

        doc.Sections[0].Entries[0].Value = "new";
        var text = Ini.Format(doc);

        StringAssert.Contains(text, "key = new");
        Assert.IsFalse(text.Contains("key = old"));
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.SetEntry(string, string)" /> creates a new entry when the key is
    /// absent and updates the existing value when the key is already present.
    /// </summary>
    [TestMethod]
    public void Mutate_WhenSetEntryIsCalled_ShouldCreateOrUpdate()
    {
        IniDocument doc = new();
        IniSection section = doc.GetOrAddSection("s");

        section.SetEntry("a", "1");
        section.SetEntry("b", "2");
        section.SetEntry("a", "1-updated");

        Assert.AreEqual(2, section.Entries.Count);
        Assert.AreEqual("1-updated", section["a"]);
        Assert.AreEqual("2", section["b"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniSection.RemoveEntry(string)" /> deletes the entry and updates the lookup.
    /// </summary>
    [TestMethod]
    public void Mutate_WhenRemoveEntryIsCalled_ShouldDropEntryFromSection()
    {
        IniDocument doc = Ini.Parse("[s]\na = 1\nb = 2\n");

        var removed = doc.Sections[0].RemoveEntry("a");

        Assert.IsTrue(removed);
        Assert.AreEqual(1, doc.Sections[0].Entries.Count);
        Assert.IsNull(doc.Sections[0]["a"]);
    }

    /// <summary>
    /// Verifies that <see cref="IniDocument.GetOrAddSection(string)" /> returns an existing section when one
    /// matches and appends a new one otherwise.
    /// </summary>
    [TestMethod]
    public void Mutate_WhenGetOrAddSectionIsCalled_ShouldReturnExistingOrCreate()
    {
        IniDocument doc = new();

        IniSection first = doc.GetOrAddSection("server");
        IniSection second = doc.GetOrAddSection("server");

        Assert.AreSame(first, second);
        Assert.AreEqual(1, doc.Sections.Count);

        IniSection other = doc.GetOrAddSection("client");
        Assert.AreEqual(2, doc.Sections.Count);
        Assert.AreNotSame(first, other);
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> emits leading-comment trivia on both sections and
    /// entries so authored INI text can be re-emitted faithfully.
    /// </summary>
    [TestMethod]
    public void Format_WhenLeadingCommentsPresent_ShouldRoundTripComments()
    {
        const string source = "# top of file\n[server]\n# before host\nhost = localhost\n";

        IniDocument doc = Ini.Parse(source);
        var text = Ini.Format(doc);

        StringAssert.Contains(text, "# top of file");
        StringAssert.Contains(text, "# before host");
        Assert.IsTrue(text.IndexOf("# top of file", StringComparison.Ordinal)
            < text.IndexOf("[server]", StringComparison.Ordinal));
        Assert.IsTrue(text.IndexOf("# before host", StringComparison.Ordinal)
            < text.IndexOf("host = localhost", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="Ini.Format(IniDocument)" /> emits an inline comment placed on
    /// <see cref="IniEntry.InlineComment" />, separated from the value by a single space.
    /// </summary>
    [TestMethod]
    public void Format_WhenEntryHasInlineComment_ShouldEmitOnSameLine()
    {
        IniDocument doc = new();
        IniSection section = doc.GetOrAddSection("s");
        IniEntry entry = section.SetEntry("indent", "4");
        entry.InlineComment = new IniComment('#', " standard");

        var text = Ini.Format(doc);

        StringAssert.Contains(text, "indent = 4 # standard");
    }

    /// <summary>
    /// Verifies that a programmatically authored document round-trips through <see cref="Ini.Format" /> and
    /// <see cref="Ini.Parse(ReadOnlySpan{char})" /> with equivalent content.
    /// </summary>
    [TestMethod]
    public void Mutate_WhenDocumentAuthoredProgrammatically_ShouldRoundTrip()
    {
        IniDocument doc = new();
        IniSection section = doc.GetOrAddSection("server");
        section.SetEntry("host", "localhost");
        section.SetEntry("port", "8080");
        section.AddLeadingComment(new IniComment('#', " server config"));

        var text = Ini.Format(doc);
        IniDocument reparsed = Ini.Parse(text);

        Assert.AreEqual("localhost", reparsed.Sections[0]["host"]);
        Assert.AreEqual("8080", reparsed.Sections[0]["port"]);
        Assert.AreEqual(1, reparsed.Sections[0].LeadingComments.Count);
    }
}
