// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini.Nodes;
using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Contains tests for the mutable <see cref="IniObject" /> / <see cref="IniValue" /> DOM, including the
/// duplicate-section/key policies and the comment-trivia round-trip.
/// </summary>
[TestClass]
public class IniObjectTests
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
    /// Verifies that a parsed document writes back to identical INI text, including its comment trivia.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenRoundTripped_ShouldPreserveEntriesAndComments()
    {
        const string source = "g=1\n; lead\n[db]\n; note\nhost=x\n; tail\n";
        IniObject root = IniNode.Parse(Encoding.UTF8.GetBytes(source));

        Assert.AreEqual(source, Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that a global value added after a section is still emitted before the first section header, because a
    /// key written after a header would be re-read as belonging to that section.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenGlobalAddedAfterSection_ShouldEmitGlobalEntriesFirst()
    {
        var root = new IniObject();
        var section = new IniObject();
        section["host"] = new IniValue("x");
        root["db"] = section;
        root["g"] = new IniValue("1");

        Assert.AreEqual("g=1\n[db]\nhost=x\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that writing an object nested inside a section throws <see cref="InvalidOperationException" />,
    /// because INI cannot represent a third level.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenObjectNestedInSection_ShouldThrowInvalidOperationException()
    {
        var root = new IniObject();
        var section = new IniObject();
        section["inner"] = new IniObject();
        root["s"] = section;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = root.ToUtf8Bytes();
        });
    }

    /// <summary>
    /// Verifies that assigning through the indexer replaces the node while preserving the name's position.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReplaceValueInPlace()
    {
        IniObject root = IniNode.Parse("a=1\nb=2\n"u8);

        root["a"] = new IniValue("99");

        CollectionAssert.AreEqual(new List<string> { "a", "b" }, root.Keys.ToList());
        Assert.AreEqual("99", root["a"].AsValue().Value);
    }

    /// <summary>
    /// Verifies that <see cref="IniObject.Remove(string)" /> removes the entry and its ordering slot.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldRemoveEntryAndOrder()
    {
        IniObject root = IniNode.Parse("a=1\nb=2\n"u8);

        Assert.IsTrue(root.Remove("a"));
        Assert.IsFalse(root.ContainsKey("a"));
        CollectionAssert.AreEqual(new List<string> { "b" }, root.Keys.ToList());
    }

    /// <summary>
    /// Verifies that <see cref="IniNode.DeepClone" /> produces an independent copy including trivia.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenMutatingClone_ShouldNotAffectOriginal()
    {
        IniObject root = IniNode.Parse("; note\n[s]\nk=1\n"u8);
        var clone = (IniObject)root.DeepClone();

        clone["s"].AsObject()["k"] = new IniValue("2");
        clone["s"].AsObject().LeadingComments.Add(" extra");

        Assert.AreEqual("1", root["s"].AsObject()["k"].AsValue().Value);
        Assert.AreEqual("2", clone["s"].AsObject()["k"].AsValue().Value);
        CollectionAssert.AreEqual(new List<string> { " note" }, root["s"].AsObject().LeadingComments.ToList());
    }
}
