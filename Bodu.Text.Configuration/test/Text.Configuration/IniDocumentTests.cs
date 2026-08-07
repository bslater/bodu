// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Verifies <see cref="IniDocument" />: the global section, section lookup, and the add / get-or-add / remove
/// surface.
/// </summary>
/// <remarks>
/// A document keeps sections in file order alongside a keyed lookup, and the two can legitimately disagree about
/// count: a file may repeat a section heading, in which case the ordered list holds both and the lookup resolves to
/// the first. The tests below pin that asymmetry rather than assuming a one-to-one mapping, because the behavior on
/// removal follows from it - removing a repeated name has to clear every occurrence, not just the indexed one.
/// </remarks>
[TestClass]
public class IniDocumentTests
{
    /// <summary>
    /// Builds a section with a single entry, for use as document content.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <param name="value">The value of the section's <c>k</c> entry.</param>
    /// <returns>The section.</returns>
    private static IniSection Section(string name, string value) =>
        new(name, [new IniEntry("k", value)]);

    /// <summary>
    /// Verifies that a new document starts with an empty, unnamed global section and no sections.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldStartWithAnEmptyGlobalSection()
    {
        var document = new IniDocument();

        Assert.IsNotNull(document.GlobalSection);
        Assert.AreEqual(string.Empty, document.GlobalSection.Name);
        Assert.IsEmpty(document.GlobalSection.Entries);
        Assert.IsEmpty(document.Sections);
    }

    /// <summary>
    /// Verifies that a document built from a global section and a section list exposes both.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSectionsSupplied_ShouldExposeThem()
    {
        var global = new IniSection(string.Empty, [new IniEntry("root", "1")]);

        var document = new IniDocument(global, [Section("a", "1"), Section("b", "2")]);

        Assert.AreEqual("1", document.GlobalSection["root"]);
        CollectionAssert.AreEqual(new[] { "a", "b" }, document.Sections.Select(s => s.Name).ToList());
    }

    /// <summary>
    /// Verifies that a named section cannot stand in for the global one - the global section is identified by having
    /// no name, so accepting a named section there would make the document unwritable.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenGlobalSectionIsNamed_ShouldThrowArgumentException()
    {
        _ = Assert.ThrowsExactly<ArgumentException>(() => _ = new IniDocument(Section("named", "1"), []));
    }

    /// <summary>
    /// Verifies that the constructor rejects null arguments, including a null element in the section sequence.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenArgumentIsNull_ShouldThrow()
    {
        var global = new IniSection(string.Empty, []);

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniDocument(null!, []));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniDocument(global, null!));
        _ = Assert.ThrowsExactly<ArgumentException>(() => _ = new IniDocument(global, [null!]));
    }

    /// <summary>
    /// Verifies that a repeated section heading keeps both sections in file order while the lookup resolves to the
    /// first, matching how the document is written back.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSectionNameRepeats_ShouldKeepBothButIndexTheFirst()
    {
        var document = new IniDocument(new IniSection(string.Empty, []), [Section("a", "first"), Section("a", "second")]);

        Assert.HasCount(2, document.Sections);
        Assert.AreEqual("first", document.GetSection("a")!["k"]);
    }

    /// <summary>
    /// Verifies that section names are matched case-insensitively by default and case-sensitively on request.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenCaseSensitivityVaries_ShouldMatchAccordingly()
    {
        var insensitive = new IniDocument(new IniSection(string.Empty, []), [Section("Server", "1")]);
        Assert.IsNotNull(insensitive.GetSection("server"));

        var sensitive = new IniDocument(new IniSection(string.Empty, []), [Section("Server", "1")], caseSensitiveSections: true);
        Assert.IsNotNull(sensitive.GetSection("Server"));
        Assert.IsNull(sensitive.GetSection("server"));
    }

    /// <summary>
    /// Verifies that looking up an absent section reports <see langword="null" /> rather than throwing, and that a
    /// null name is rejected as a programming error rather than treated as a miss.
    /// </summary>
    [TestMethod]
    public void GetSection_WhenNameIsAbsentOrNull_ShouldReturnNullOrThrow()
    {
        var document = new IniDocument();

        Assert.IsNull(document.GetSection("missing"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = document.GetSection(null!));
    }

    /// <summary>
    /// Verifies that the try-pattern reports presence without throwing, and rejects a null name.
    /// </summary>
    [TestMethod]
    public void TryGetSection_ShouldReportPresence()
    {
        var document = new IniDocument(new IniSection(string.Empty, []), [Section("a", "1")]);

        Assert.IsTrue(document.TryGetSection("a", out IniSection? section));
        Assert.AreEqual("1", section!["k"]);

        Assert.IsFalse(document.TryGetSection("missing", out IniSection? _));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = document.TryGetSection(null!, out _));
    }

    /// <summary>
    /// Verifies that adding a section appends it in order and indexes it for lookup.
    /// </summary>
    [TestMethod]
    public void AddSection_ShouldAppendAndIndex()
    {
        var document = new IniDocument();

        document.AddSection(Section("a", "1"));
        document.AddSection(Section("b", "2"));

        CollectionAssert.AreEqual(new[] { "a", "b" }, document.Sections.Select(s => s.Name).ToList());
        Assert.IsNotNull(document.GetSection("b"));
    }

    /// <summary>
    /// Verifies that the global section cannot be added as an ordinary one: it is already the document's, and a
    /// second unnamed section has no heading to write.
    /// </summary>
    [TestMethod]
    public void AddSection_WhenSectionIsUnnamed_ShouldThrowArgumentException()
    {
        var document = new IniDocument();

        _ = Assert.ThrowsExactly<ArgumentException>(() => document.AddSection(new IniSection(string.Empty, [])));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => document.AddSection(null!));
    }

    /// <summary>
    /// Verifies that get-or-add returns the existing section when the name is present and creates one otherwise, so
    /// a caller building a document does not have to check first.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_ShouldReturnExistingOrCreate()
    {
        var document = new IniDocument(new IniSection(string.Empty, []), [Section("a", "1")]);

        IniSection existing = document.GetOrAddSection("a");
        Assert.AreEqual("1", existing["k"]);
        Assert.HasCount(1, document.Sections);

        IniSection created = document.GetOrAddSection("b");
        Assert.IsEmpty(created.Entries);
        Assert.HasCount(2, document.Sections);
    }

    /// <summary>
    /// Verifies that a section created by get-or-add inherits the document's key comparison, so a case-sensitive
    /// document does not silently produce case-insensitive sections.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_WhenDocumentIsCaseSensitive_ShouldCreateCaseSensitiveSections()
    {
        var document = new IniDocument(caseSensitiveSections: true);

        IniSection created = document.GetOrAddSection("a");
        created.AddEntry(new IniEntry("Key", "1"));

        Assert.IsNull(created["key"]);
    }

    /// <summary>
    /// Verifies that get-or-add rejects an empty or null name, since neither can be written as a heading.
    /// </summary>
    [TestMethod]
    public void GetOrAddSection_WhenNameIsEmptyOrNull_ShouldThrow()
    {
        var document = new IniDocument();

        _ = Assert.ThrowsExactly<ArgumentException>(() => _ = document.GetOrAddSection(string.Empty));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = document.GetOrAddSection(null!));
    }

    /// <summary>
    /// Verifies that removing a section drops it from the ordered list and the lookup, and reports whether one was
    /// there.
    /// </summary>
    [TestMethod]
    public void RemoveSection_ShouldRemoveFromBothViewsAndReportWhetherFound()
    {
        var document = new IniDocument(new IniSection(string.Empty, []), [Section("a", "1"), Section("b", "2")]);

        Assert.IsTrue(document.RemoveSection("a"));
        CollectionAssert.AreEqual(new[] { "b" }, document.Sections.Select(s => s.Name).ToList());
        Assert.IsNull(document.GetSection("a"));

        Assert.IsFalse(document.RemoveSection("a"));
    }

    /// <summary>
    /// Verifies that removing a repeated section name clears every occurrence rather than only the indexed one -
    /// otherwise a later save would still write the section the caller believed it had removed.
    /// </summary>
    [TestMethod]
    public void RemoveSection_WhenNameRepeats_ShouldRemoveEveryOccurrence()
    {
        var document = new IniDocument(
            new IniSection(string.Empty, []),
            [Section("a", "first"), Section("b", "2"), Section("a", "second")]);

        Assert.IsTrue(document.RemoveSection("a"));

        CollectionAssert.AreEqual(new[] { "b" }, document.Sections.Select(s => s.Name).ToList());
        Assert.IsNull(document.GetSection("a"));
    }

    /// <summary>
    /// Verifies that removal honors the document's name comparison, so a case-sensitive document does not remove a
    /// section whose name differs only in case.
    /// </summary>
    [TestMethod]
    public void RemoveSection_WhenDocumentIsCaseSensitive_ShouldNotRemoveACaseVariant()
    {
        var document = new IniDocument(new IniSection(string.Empty, []), [Section("Server", "1")], caseSensitiveSections: true);

        Assert.IsFalse(document.RemoveSection("server"));
        Assert.HasCount(1, document.Sections);

        Assert.IsTrue(document.RemoveSection("Server"));
        Assert.IsEmpty(document.Sections);
    }

    /// <summary>
    /// Verifies that removal rejects a null name.
    /// </summary>
    [TestMethod]
    public void RemoveSection_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => _ = new IniDocument().RemoveSection(null!));
    }
}
