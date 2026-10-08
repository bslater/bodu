// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Contains the member tests for <see cref="IniObject.this[string]" />: adding, replacing and reading entries and
/// sections by name.
/// </summary>
public partial class IniObjectTests
{
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
    /// Verifies that assigning a section to its own name keeps the section and its entries rather than clearing it, so
    /// the document writes back unchanged.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSectionAssignedToItself_ShouldKeepItsEntries()
    {
        IniObject root = IniNode.Parse("[section2]\nname22=value22\n"u8);

        root["section2"] = root["section2"];

        IniObject section = root["section2"].AsObject();
        CollectionAssert.AreEqual(new List<string> { "name22" }, section.Keys.ToList());
        Assert.AreEqual("value22", section["name22"].AsValue().Value);
        Assert.AreEqual("[section2]\nname22=value22\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that replacing a section with a new object keeps the section's place among the others, drops the old
    /// section's entries, and writes the sections in their original order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSectionReplaced_ShouldKeepSectionOrder()
    {
        IniObject root = IniNode.Parse("[section1]\nname1=value1\n[section2]\nname2=value2\n[section3]\nname3=value3\n"u8);
        var replacement = new IniObject();
        replacement["name22"] = new IniValue("value22");

        root["section2"] = replacement;

        CollectionAssert.AreEqual(new List<string> { "section1", "section2", "section3" }, root.Keys.ToList());
        Assert.IsFalse(root["section2"].AsObject().ContainsKey("name2"));
        Assert.AreEqual(
            "[section1]\nname1=value1\n[section2]\nname22=value22\n[section3]\nname3=value3\n",
            Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that a value carrying a leading comment keeps it when assigned to a section's key, and that the comment
    /// is written on the line before the value's entry.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenValueWithLeadingCommentAssigned_ShouldKeepTheComment()
    {
        IniObject root = IniNode.Parse("[TestSection]\n"u8);
        IniObject section = root["TestSection"].AsObject();
        var value = new IniValue("TestValue");
        value.LeadingComments.Add("This is a comment");

        section["TestKey"] = value;

        CollectionAssert.AreEqual(new List<string> { "This is a comment" }, section["TestKey"].LeadingComments.ToList());
        Assert.AreEqual("[TestSection]\n;This is a comment\nTestKey=TestValue\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }

    /// <summary>
    /// Verifies that assigning a value to a key whose value is empty replaces the empty value, so the entry is written
    /// with the new one.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenExistingValueIsEmpty_ShouldReplaceValue()
    {
        IniObject root = IniNode.Parse("DefaultKey=\n"u8);

        root["DefaultKey"] = "Value";

        Assert.AreEqual("DefaultKey=Value\n", Encoding.UTF8.GetString(root.ToUtf8Bytes()));
    }
}
