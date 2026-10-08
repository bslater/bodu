// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
