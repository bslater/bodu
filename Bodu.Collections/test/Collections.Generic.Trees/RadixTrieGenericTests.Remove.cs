// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RadixTrieGenericTests.Remove.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class RadixTrieGenericTests
{
    /// <summary>
    /// Verifies that removing a key deletes its entry.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyExists_ShouldDeleteEntry()
    {
        var sut = new RadixTrie<int>();
        sut.Add("a", 1);

        Assert.IsTrue(sut.Remove("a"));
        Assert.IsFalse(sut.ContainsKey("a"));
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that removing a branch key merges the surviving pass-through node with its child while preserving
    /// every remaining value.
    /// </summary>
    [TestMethod]
    public void Remove_WhenBranchDeleted_ShouldMergeAndPreserveValues()
    {
        var sut = new RadixTrie<int>
        {
            ["app"] = 1,
            ["apple"] = 2,
            ["applet"] = 3,
        };

        Assert.IsTrue(sut.Remove("apple"));

        Assert.AreEqual(2, sut.Count);
        Assert.AreEqual(1, sut["app"]);
        Assert.AreEqual(3, sut["applet"]);
        Assert.IsFalse(sut.ContainsKey("apple"));
        Assert.IsTrue(sut.StartsWith("apple"));
    }

    /// <summary>
    /// Verifies that removing an absent key — including one ending inside a compressed edge — returns
    /// <see langword="false" /> and leaves values untouched.
    /// </summary>
    [TestMethod]
    public void Remove_WhenKeyAbsent_ShouldReturnFalse()
    {
        var sut = new RadixTrie<int> { ["card"] = 5 };

        Assert.IsFalse(sut.Remove("ca"));
        Assert.IsFalse(sut.Remove("cards"));
        Assert.AreEqual(5, sut["card"]);
        Assert.AreEqual(1, sut.Count);
    }
}
