// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TreeTests.DebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

public sealed partial class TreeTests
{
    /// <summary>
    /// Verifies that <see cref="TreeDebugView{T}" /> surfaces the node's own value and its immediate children.
    /// </summary>
    [TestMethod]
    public void DebugView_ShouldSurfaceValueAndImmediateChildren()
    {
        var root = new Tree<int>(1);
        root.AddChild(2);
        Tree<int> three = root.AddChild(3);
        three.AddChild(4);

        var view = new TreeDebugView<int>(root);

        Assert.AreEqual(1, view.Value);
        Assert.AreEqual(2, view.Children.Length);
        CollectionAssert.AreEqual(new[] { 2, 3 }, view.Children.Select(child => child.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="TreeDebugView{T}.Children" /> is empty for a leaf node, exposing only the
    /// immediate children rather than the whole subtree.
    /// </summary>
    [TestMethod]
    public void DebugView_Children_WhenNodeIsLeaf_ShouldBeEmpty()
    {
        var view = new TreeDebugView<int>(new Tree<int>(9));

        Assert.AreEqual(9, view.Value);
        Assert.AreEqual(0, view.Children.Length);
    }

    /// <summary>
    /// Verifies that constructing <see cref="TreeDebugView{T}" /> with a <see langword="null" /> node throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void DebugView_Ctor_WhenNodeIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TreeDebugView<int>(null!);
        });

        Assert.AreEqual("node", ex.ParamName);
    }
}
