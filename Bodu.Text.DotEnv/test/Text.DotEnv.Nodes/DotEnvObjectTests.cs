// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvObjectTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.DotEnv.Nodes;

namespace Bodu.Text.DotEnv.Nodes;

/// <summary>
/// Contains tests for the mutable <see cref="DotEnvObject" /> / <see cref="DotEnvValue" /> DOM.
/// </summary>
[TestClass]
public class DotEnvObjectTests
{
    /// <summary>
    /// Verifies that parsing populates the object with the keys and values in source order.
    /// </summary>
    [TestMethod]
    public void Parse_WhenMultipleEntries_ShouldPreserveOrderAndValues()
    {
        DotEnvObject root = DotEnvNode.Parse("A=1\nexport B=two\nC=\"3 3\"\n"u8);

        CollectionAssert.AreEqual(new List<string> { "A", "B", "C" }, root.Keys.ToList());
        Assert.AreEqual("1", root["A"].Value);
        Assert.AreEqual("two", root["B"].Value);
        Assert.AreEqual("3 3", root["C"].Value);
        Assert.IsTrue(root.IsExport("B"));
        Assert.IsFalse(root.IsExport("A"));
    }

    /// <summary>
    /// Verifies that assigning through the indexer replaces the value while preserving the key's position.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyExists_ShouldReplaceValueInPlace()
    {
        DotEnvObject root = DotEnvNode.Parse("A=1\nB=2\n"u8);

        root["A"] = new DotEnvValue("99");

        CollectionAssert.AreEqual(new List<string> { "A", "B" }, root.Keys.ToList());
        Assert.AreEqual("99", root["A"].Value);
    }

    /// <summary>
    /// Verifies that a parsed object writes back to DotEnv text that round-trips, preserving the export prefix.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenRoundTripped_ShouldPreserveEntriesAndExport()
    {
        DotEnvObject root = DotEnvNode.Parse("A=1\nexport B=two\n"u8);

        string text = Encoding.UTF8.GetString(root.ToUtf8Bytes());

        Assert.AreEqual("A=1\nexport B=two\n", text);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvNode.DeepClone" /> produces an independent copy.
    /// </summary>
    [TestMethod]
    public void DeepClone_WhenMutatingClone_ShouldNotAffectOriginal()
    {
        DotEnvObject root = DotEnvNode.Parse("A=1\n"u8);
        var clone = (DotEnvObject)root.DeepClone();

        clone["A"] = new DotEnvValue("2");

        Assert.AreEqual("1", root["A"].Value);
        Assert.AreEqual("2", clone["A"].Value);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvObject.Remove(string)" /> removes the entry and its ordering slot.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldRemoveEntryAndOrder()
    {
        DotEnvObject root = DotEnvNode.Parse("A=1\nB=2\n"u8);

        Assert.IsTrue(root.Remove("A"));
        Assert.IsFalse(root.ContainsKey("A"));
        CollectionAssert.AreEqual(new List<string> { "B" }, root.Keys.ToList());
    }
}
