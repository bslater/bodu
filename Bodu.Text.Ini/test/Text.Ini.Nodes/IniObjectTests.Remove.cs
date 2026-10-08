// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniObjectTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Contains the member tests for <see cref="IniObject.Remove(string)" />.
/// </summary>
public partial class IniObjectTests
{
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
}
