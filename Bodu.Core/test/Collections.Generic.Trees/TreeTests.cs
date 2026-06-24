// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TreeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Contains unit tests for the <see cref="Tree{T}" /> type.
/// </summary>
[TestClass]
public sealed class TreeTests
{
    /// <summary>
    /// Builds the canonical sample tree used by traversal tests:
    /// <code>
    ///         1
    ///       / | \
    ///      2  3  4
    ///     /|     |
    ///    5 6     7
    /// </code>
    /// </summary>
    /// <returns>The root of the sample tree.</returns>
    private static Tree<int> BuildSample()
    {
        var root = new Tree<int>(1);
        var two = root.AddChild(2);
        root.AddChild(3);
        var four = root.AddChild(4);
        two.AddChild(5);
        two.AddChild(6);
        four.AddChild(7);
        return root;
    }

    /// <summary>
    /// Verifies that a created child is appended and linked back to its parent.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void AddChild_WhenValueSupplied_ShouldAppendAndLinkParent()
    {
        var root = new Tree<string>("root");

        var child = root.AddChild("child");

        Assert.AreEqual(1, root.ChildCount);
        Assert.AreSame(root, child.Parent);
        Assert.AreSame(child, root.Children[0]);
    }

    /// <summary>
    /// Verifies that the constructor seeds a child per supplied child value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenChildValuesSupplied_ShouldCreateChildren()
    {
        var root = new Tree<int>(0, [1, 2, 3]);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, root.Children.Select(c => c.Value).ToArray());
        Assert.IsTrue(root.Children.All(c => ReferenceEquals(c.Parent, root)));
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> child-value sequence throws <see cref="ArgumentNullException" /> naming <c>childValues</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenChildValuesIsNull_ShouldThrowForChildValues()
    {
        Assert.AreEqual(
            "childValues",
            Assert.ThrowsExactly<ArgumentNullException>(() => new Tree<int>(0, null!)).ParamName);
    }

    /// <summary>
    /// Verifies that pre-order traversal visits each node before its children.
    /// </summary>
    [TestMethod]
    public void PreOrder_WhenTraversed_ShouldVisitNodeBeforeChildren()
    {
        var root = BuildSample();

        CollectionAssert.AreEqual(new[] { 1, 2, 5, 6, 3, 4, 7 }, root.PreOrder().Select(n => n.Value).ToArray());
    }

    /// <summary>
    /// Verifies that post-order traversal visits each node after its children.
    /// </summary>
    [TestMethod]
    public void PostOrder_WhenTraversed_ShouldVisitChildrenBeforeNode()
    {
        var root = BuildSample();

        CollectionAssert.AreEqual(new[] { 5, 6, 2, 3, 7, 4, 1 }, root.PostOrder().Select(n => n.Value).ToArray());
    }

    /// <summary>
    /// Verifies that level-order traversal visits nodes breadth-first.
    /// </summary>
    [TestMethod]
    public void LevelOrder_WhenTraversed_ShouldVisitBreadthFirst()
    {
        var root = BuildSample();

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6, 7 }, root.LevelOrder().Select(n => n.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Tree{T}.Descendants" /> excludes the node itself.
    /// </summary>
    [TestMethod]
    public void Descendants_WhenEnumerated_ShouldExcludeSelf()
    {
        var root = BuildSample();

        CollectionAssert.AreEqual(new[] { 2, 5, 6, 3, 4, 7 }, root.Descendants().Select(n => n.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Tree{T}.Ancestors" /> walks from parent to root.
    /// </summary>
    [TestMethod]
    public void Ancestors_WhenEnumerated_ShouldWalkToRoot()
    {
        var root = BuildSample();
        var seven = root.PreOrder().First(n => n.Value == 7);

        CollectionAssert.AreEqual(new[] { 4, 1 }, seven.Ancestors().Select(n => n.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Tree{T}.Leaves" /> returns only childless nodes.
    /// </summary>
    [TestMethod]
    public void Leaves_WhenEnumerated_ShouldReturnChildlessNodes()
    {
        var root = BuildSample();

        CollectionAssert.AreEqual(new[] { 5, 6, 3, 7 }, root.Leaves().Select(n => n.Value).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Tree{T}.Root" /> returns the topmost ancestor.
    /// </summary>
    [TestMethod]
    public void Root_WhenCalledOnDescendant_ShouldReturnTopmostAncestor()
    {
        var root = BuildSample();
        var seven = root.PreOrder().First(n => n.Value == 7);

        Assert.AreSame(root, seven.Root());
    }

    /// <summary>
    /// Verifies that depth and height are measured in edges.
    /// </summary>
    [TestMethod]
    public void DepthAndHeight_WhenQueried_ShouldCountEdges()
    {
        var root = BuildSample();
        var seven = root.PreOrder().First(n => n.Value == 7);
        var five = root.PreOrder().First(n => n.Value == 5);

        Assert.AreEqual(0, root.Depth);
        Assert.AreEqual(2, seven.Depth);
        Assert.AreEqual(2, root.Height);
        Assert.AreEqual(0, five.Height);
    }

    /// <summary>
    /// Verifies that the root and leaf state flags reflect the node position.
    /// </summary>
    [TestMethod]
    public void IsRootAndIsLeaf_WhenQueried_ShouldReflectPosition()
    {
        var root = BuildSample();
        var three = root.PreOrder().First(n => n.Value == 3);

        Assert.IsTrue(root.IsRoot);
        Assert.IsFalse(root.IsLeaf);
        Assert.IsFalse(three.IsRoot);
        Assert.IsTrue(three.IsLeaf);
    }

    /// <summary>
    /// Verifies that attaching a detached subtree links it under the new parent.
    /// </summary>
    [TestMethod]
    public void AddChild_WhenSubtreeDetached_ShouldAttachAndLinkParent()
    {
        var root = new Tree<int>(1);
        var subtree = new Tree<int>(2);
        subtree.AddChild(3);

        root.AddChild(subtree);

        Assert.AreSame(root, subtree.Parent);
        Assert.AreEqual(2, root.PreOrder().Count(n => n.Value == 2 || n.Value == 3));
    }

    /// <summary>
    /// Verifies that attaching a <see langword="null" /> child throws <see cref="ArgumentNullException" /> naming <c>child</c>.
    /// </summary>
    [TestMethod]
    public void AddChild_WhenChildIsNull_ShouldThrowForChild()
    {
        var root = new Tree<int>(1);

        Assert.AreEqual(
            "child",
            Assert.ThrowsExactly<ArgumentNullException>(() => root.AddChild((Tree<int>)null!)).ParamName);
    }

    /// <summary>
    /// Verifies that attaching a node that already has a parent throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void AddChild_WhenChildAlreadyHasParent_ShouldThrowInvalidOperationException()
    {
        var root = new Tree<int>(1);
        var first = new Tree<int>(2);
        root.AddChild(first);
        var other = new Tree<int>(3);

        Assert.ThrowsExactly<InvalidOperationException>(() => other.AddChild(first));
    }

    /// <summary>
    /// Verifies that attaching an ancestor (or the node itself) as a child throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void AddChild_WhenCreatingCycle_ShouldThrowInvalidOperationException()
    {
        var root = new Tree<int>(1);
        var child = root.AddChild(2);

        Assert.ThrowsExactly<InvalidOperationException>(() => child.AddChild(root));
        Assert.ThrowsExactly<InvalidOperationException>(() => root.AddChild(root));
    }

    /// <summary>
    /// Verifies that removing a child detaches it and clears its parent.
    /// </summary>
    [TestMethod]
    public void RemoveChild_WhenChildPresent_ShouldDetachAndClearParent()
    {
        var root = new Tree<int>(1);
        var child = root.AddChild(2);

        Assert.IsTrue(root.RemoveChild(child));
        Assert.IsNull(child.Parent);
        Assert.AreEqual(0, root.ChildCount);
    }

    /// <summary>
    /// Verifies that removing a node that is not a child returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void RemoveChild_WhenChildAbsent_ShouldReturnFalse()
    {
        var root = new Tree<int>(1);
        var stranger = new Tree<int>(9);

        Assert.IsFalse(root.RemoveChild(stranger));
    }

    /// <summary>
    /// Verifies that <see cref="Tree{T}.Remove" /> detaches a node from its parent and returns <see langword="false" /> for a root.
    /// </summary>
    [TestMethod]
    public void Remove_WhenNodeHasParent_ShouldDetachOtherwiseReturnFalse()
    {
        var root = new Tree<int>(1);
        var child = root.AddChild(2);

        Assert.IsTrue(child.Remove());
        Assert.IsNull(child.Parent);
        Assert.AreEqual(0, root.ChildCount);
        Assert.IsFalse(root.Remove());
    }

    /// <summary>
    /// Verifies that <see cref="Tree{T}.Clear" /> detaches every child.
    /// </summary>
    [TestMethod]
    public void Clear_WhenChildrenPresent_ShouldDetachAll()
    {
        var root = BuildSample();
        var children = root.Children.ToArray();

        root.Clear();

        Assert.AreEqual(0, root.ChildCount);
        Assert.IsTrue(children.All(c => c.Parent is null));
    }

    /// <summary>
    /// Verifies that a very deep tree can be traversed and measured iteratively without overflowing the stack.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void PreOrder_WhenTreeIsVeryDeep_ShouldTraverseWithoutStackOverflow()
    {
        const int depth = 100_000;
        var root = new Tree<int>(0);
        var current = root;
        for (var i = 1; i <= depth; i++)
            current = current.AddChild(i);

        Assert.AreEqual(depth + 1, root.PreOrder().Count());
        Assert.AreEqual(depth, root.Height);
        Assert.AreEqual(depth, current.Depth);
    }

    /// <summary>
    /// Verifies that the <see cref="Tree{T}.Children" /> view does not expose the mutable backing list and cannot be
    /// downcast to it.
    /// </summary>
    [TestMethod]
    public void Children_WhenExposed_ShouldNotBeMutableBackingList()
    {
        var root = new Tree<int>(0);
        root.AddChild(1);

        Assert.IsFalse(root.Children is List<Tree<int>>);
        Assert.ThrowsExactly<NotSupportedException>(() => ((IList<Tree<int>>)root.Children).Add(new Tree<int>(2)));
    }

    /// <summary>
    /// Verifies that the <see cref="Tree{T}.Children" /> view reflects children added and removed after it was read.
    /// </summary>
    [TestMethod]
    public void Children_WhenChildrenChange_ShouldReflectLiveState()
    {
        var root = new Tree<int>(0);
        var children = root.Children;

        var child = root.AddChild(1);
        Assert.AreEqual(1, children.Count);

        root.RemoveChild(child);
        Assert.AreEqual(0, children.Count);
    }
}
