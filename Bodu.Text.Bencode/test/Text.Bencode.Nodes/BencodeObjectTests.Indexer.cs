// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeObjectTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies the <see cref="BencodeObject" /> indexer, including the rule that a node appears at most once in a tree.
/// </summary>
public partial class BencodeObjectTests
{
    /// <summary>
    /// Verifies that assigning the value of one key to another existing key of the same object throws
    /// <see cref="InvalidOperationException" /> rather than holding the node twice, and leaves both entries unchanged.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetToValueOfAnotherKey_ShouldThrowInvalidOperationException()
    {
        var first = BencodeValue.Create(1L);
        var second = BencodeValue.Create(2L);
        var obj = new BencodeObject { { "a", first }, { "b", second } };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            obj["b"] = first;
        });

        Assert.AreSame(second, obj["b"], "The entry after the rejected assignment.");
        Assert.AreSame(obj, second.Parent, "The parent of the value that was not replaced.");
    }

    /// <summary>
    /// Verifies that assigning the value of an existing key to a new key of the same object throws
    /// <see cref="InvalidOperationException" /> and adds no entry.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetUnderANewKeyToAValueTheObjectHolds_ShouldThrowInvalidOperationException()
    {
        var value = BencodeValue.Create(1L);
        var obj = new BencodeObject { { "a", value } };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            obj["b"] = value;
        });

        Assert.AreEqual(1, obj.Count, "The object's count after the rejected assignment.");
        Assert.IsFalse(obj.ContainsKey("b"), "Whether the rejected key was added.");
    }
}
