// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Contains the member tests for <see cref="IniNode.Parse(ReadOnlySpan{byte}, IniReaderOptions, IniDocumentOptions)" />:
/// the two-level tree it builds, the duplicate-section and duplicate-key policies, and the comment trivia it attaches.
/// </summary>
public partial class IniObjectTests
{
    /// <summary>
    /// Verifies that parsing builds the two-level tree: global keys and sections on the root, entries within each
    /// section, in source order.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalKeyAndSections_ShouldBuildTwoLevelTree()
    {
        IniObject root = IniNode.Parse("key0=a\n[db]\nhost=x\nport=5\n"u8);

        CollectionAssert.AreEqual(new List<string> { "key0", "db" }, root.Keys.ToList());
        Assert.AreEqual("a", root["key0"].AsValue().Value);

        IniObject db = root["db"].AsObject();
        CollectionAssert.AreEqual(new List<string> { "host", "port" }, db.Keys.ToList());
        Assert.AreEqual("x", db["host"].AsValue().Value);
        Assert.AreEqual("5", db["port"].AsValue().Value);
    }

    /// <summary>
    /// Verifies that a repeated section name merges into the first occurrence by default.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSections_ShouldMergeByDefault()
    {
        IniObject root = IniNode.Parse("[db]\nhost=x\n[db]\nport=5\n"u8);

        Assert.AreEqual(1, root.Count);
        IniObject db = root["db"].AsObject();
        CollectionAssert.AreEqual(new List<string> { "host", "port" }, db.Keys.ToList());
    }

    /// <summary>
    /// Verifies that a repeated section name throws <see cref="IniFormatException" /> under
    /// <see cref="IniDuplicateSectionBehavior.Disallowed" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateSectionDisallowed_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniNode.Parse(
                "[db]\nhost=x\n[db]\nport=5\n"u8,
                IniReaderOptions.Default,
                new IniDocumentOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed });
        });
    }

    /// <summary>
    /// Verifies that a duplicate key keeps the last value by default, preserving the key's original position.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKey_ShouldKeepLastByDefault()
    {
        IniObject root = IniNode.Parse("[s]\nk=1\nother=o\nk=2\n"u8);

        IniObject section = root["s"].AsObject();
        CollectionAssert.AreEqual(new List<string> { "k", "other" }, section.Keys.ToList());
        Assert.AreEqual("2", section["k"].AsValue().Value);
    }

    /// <summary>
    /// Verifies that a duplicate key keeps the first value under <see cref="IniDuplicateKeyBehavior.FirstWins" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyFirstWins_ShouldKeepFirst()
    {
        IniObject root = IniNode.Parse(
            "[s]\nk=1\nk=2\n"u8,
            IniReaderOptions.Default,
            new IniDocumentOptions { DuplicateKeyBehavior = IniDuplicateKeyBehavior.FirstWins });

        Assert.AreEqual("1", root["s"].AsObject()["k"].AsValue().Value);
    }

    /// <summary>
    /// Verifies that a duplicate key throws <see cref="IniFormatException" /> under
    /// <see cref="IniDuplicateKeyBehavior.Disallowed" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDuplicateKeyDisallowed_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniNode.Parse(
                "[s]\nk=1\nk=2\n"u8,
                IniReaderOptions.Default,
                new IniDocumentOptions { DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed });
        });
    }

    /// <summary>
    /// Verifies that a global key colliding with a section of the same name throws <see cref="IniFormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGlobalKeyCollidesWithSection_ShouldThrowIniFormatException()
    {
        Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniNode.Parse("db=x\n[db]\nhost=y\n"u8);
        });
    }

    /// <summary>
    /// Verifies that comment lines attach as leading trivia of the following section or value, and that the block at
    /// the end of the document attaches as trailing trivia of the innermost object.
    /// </summary>
    [TestMethod]
    public void Parse_WhenComments_ShouldAttachLeadingAndTrailingTrivia()
    {
        IniObject root = IniNode.Parse("; lead\n[db]\n; note\nhost=x\n; tail\n"u8);

        IniObject db = root["db"].AsObject();
        CollectionAssert.AreEqual(new List<string> { " lead" }, db.LeadingComments.ToList());
        CollectionAssert.AreEqual(new List<string> { " note" }, db["host"].LeadingComments.ToList());
        CollectionAssert.AreEqual(new List<string> { " tail" }, db.TrailingComments.ToList());
    }

    /// <summary>
    /// Verifies that the comment ending the last section is kept as that section's trailing comment, and is written back
    /// as the document's last line.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCommentEndsTheLastSection_ShouldKeepItAsTrailingComment()
    {
        const string source =
            "\nglobal key = global value\n;comment for section1\n[section1]\n;comment for key1\nkey 1 =      value 1\n" +
            "key;2 = va:lu;e.5\n[ section 2]\n;comment for myKey1\nmykey1 = value1\n;comment for section2\n";

        IniObject root = IniNode.Parse(Encoding.UTF8.GetBytes(source));

        CollectionAssert.AreEqual(new List<string> { "comment for section2" }, root["section 2"].AsObject().TrailingComments.ToList());
        Assert.EndsWith(";comment for section2\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that a duplicate section or key the document options reject, and a section named like a global key, are
    /// reported at the offending header or key: its line, and the offset of its first byte, the <c>[</c> of a header or
    /// the first byte of a key after any leading whitespace, whatever the line endings and whether comments are kept.
    /// </summary>
    /// <param name="testName">The human-readable scenario label.</param>
    /// <param name="source">The INI source, its lines separated by LF.</param>
    /// <param name="lineEnding">The line ending written in place of each LF.</param>
    /// <param name="skipComments">Whether the reader skips comments.</param>
    /// <param name="line">The expected 1-based line of the offending header or key.</param>
    /// <param name="offset">The expected zero-based byte offset of its first byte.</param>
    [TestMethod]
    [DataRow("duplicate section, LF", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\n", false, 5, 26)]
    [DataRow("duplicate section, CR LF", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\r\n", false, 5, 30)]
    [DataRow("duplicate section, lone CR", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\r", false, 5, 26)]
    [DataRow("duplicate section, CR LF, comments skipped", "; lead\n[s]\nk=1\n; before\n  [s]\nj=2\n", "\r\n", true, 5, 30)]
    [DataRow("duplicate key in a section, LF", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\n", false, 4, 17)]
    [DataRow("duplicate key in a section, CR LF", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\r\n", false, 4, 20)]
    [DataRow("duplicate key in a section, lone CR", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\r", false, 4, 17)]
    [DataRow("duplicate key in a section, CR LF, comments skipped", "[s]\n; note\nk=1\n  k = 2\nx=3\n", "\r\n", true, 4, 20)]
    [DataRow("duplicate global key, LF", "; top\ng=1\ng=2\n[s]\nk=1\n", "\n", false, 3, 10)]
    [DataRow("duplicate global key, CR LF", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r\n", false, 3, 12)]
    [DataRow("duplicate global key, lone CR", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r", false, 3, 10)]
    [DataRow("duplicate global key, CR LF, comments skipped", "; top\ng=1\ng=2\n[s]\nk=1\n", "\r\n", true, 3, 12)]
    [DataRow("section named like a global key, LF", "s=1\n; c\n[s]\nk=1\n", "\n", false, 3, 8)]
    [DataRow("section named like a global key, CR LF", "s=1\n; c\n[s]\nk=1\n", "\r\n", false, 3, 10)]
    [DataRow("section named like a global key, lone CR", "s=1\n; c\n[s]\nk=1\n", "\r", false, 3, 8)]
    [DataRow("section named like a global key, CR LF, comments skipped", "s=1\n; c\n[s]\nk=1\n", "\r\n", true, 3, 10)]
    public void Parse_WhenAPolicyRejectsASectionOrKey_ShouldReportTheOffendingHeaderOrKey(string testName, string source, string lineEnding, bool skipComments, int line, int offset)
    {
        _ = testName;
        byte[] bytes = Encoding.UTF8.GetBytes(source.Replace("\n", lineEnding, StringComparison.Ordinal));
        var readerOptions = new IniReaderOptions { SkipComments = skipComments };
        var documentOptions = new IniDocumentOptions
        {
            DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed,
            DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed,
        };

        IniFormatException ex = Assert.ThrowsExactly<IniFormatException>(() =>
        {
            _ = IniNode.Parse(bytes, readerOptions, documentOptions);
        });

        Assert.AreEqual(line, ex.LineNumber, ex.Message);
        Assert.AreEqual(offset, ex.Offset, ex.Message);
    }
}
