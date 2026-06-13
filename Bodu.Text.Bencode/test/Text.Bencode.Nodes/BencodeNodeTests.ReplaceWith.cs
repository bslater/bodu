// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeTests.ReplaceWith.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Verifies <see cref="BencodeNode.ReplaceWith" />: in-place replacement within object and array parents, detachment
/// of the replaced node, the root no-op, and rejection of replacements that already have a parent.
/// </summary>
public partial class BencodeNodeTests
{
    /// <summary>
    /// Verifies that replacing a node stored under an object key swaps the value and detaches the replaced node.
    /// </summary>
    [TestMethod]
    public void ReplaceWith_WhenParentIsObject_ShouldSwapValueAndDetachOldNode()
    {
        var root = new BencodeObject { ["name"] = "old" };
        BencodeNode replaced = root["name"]!;

        replaced.ReplaceWith("new");

        Assert.AreEqual("new", root["name"]!.GetValue<string>());
        Assert.IsNull(replaced.Parent);
    }

    /// <summary>
    /// Verifies that replacing an array element swaps the element in place and detaches the replaced node.
    /// </summary>
    [TestMethod]
    public void ReplaceWith_WhenParentIsArray_ShouldSwapElementAndDetachOldNode()
    {
        var root = new BencodeArray(1, 2, 3);
        BencodeNode replaced = root[1]!;

        replaced.ReplaceWith(20);

        Assert.AreEqual(20L, root[1]!.GetValue<long>());
        Assert.AreEqual(3, root.Count);
        Assert.IsNull(replaced.Parent);
    }

    /// <summary>
    /// Verifies that replacing with a container node re-parents the container under the original slot.
    /// </summary>
    [TestMethod]
    public void ReplaceWith_WhenReplacementIsContainer_ShouldReparentContainer()
    {
        var root = new BencodeObject { ["info"] = 0 };
        var replacement = new BencodeObject { ["length"] = 1024 };

        root["info"]!.ReplaceWith(replacement);

        Assert.AreSame(replacement, root["info"]);
        Assert.AreSame(root, replacement.Parent);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeNode.ReplaceWith" /> on a node with no parent does nothing.
    /// </summary>
    [TestMethod]
    public void ReplaceWith_WhenNodeIsRoot_ShouldDoNothing()
    {
        BencodeNode node = BencodeValue.Create(1);

        node.ReplaceWith(2);

        Assert.IsNull(node.Parent);
        Assert.AreEqual(1L, node.GetValue<long>());
    }

    /// <summary>
    /// Verifies that replacing with a node that already belongs to another container throws
    /// <see cref="InvalidOperationException" />, preserving the single-parent rule.
    /// </summary>
    [TestMethod]
    public void ReplaceWith_WhenReplacementHasParent_ShouldThrowInvalidOperationException()
    {
        var other = new BencodeArray(7);
        var root = new BencodeObject { ["a"] = 1 };

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            root["a"]!.ReplaceWith(other[0]);
        });
    }
}
