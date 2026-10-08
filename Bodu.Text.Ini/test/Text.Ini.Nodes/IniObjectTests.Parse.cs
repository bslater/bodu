// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
