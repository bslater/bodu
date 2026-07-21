// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.RecursiveSelectInternal.FlagComposition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Extensions;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IEnumerableExtensionsTests_RecursiveSelectInternal
{
    /// <summary>
    /// Builds the two-level fixture used by the flag-composition tests: sibling roots <c>A</c> and <c>B</c>, where
    /// <c>A</c> owns children <c>A1</c> and <c>A2</c> and <c>B</c> owns child <c>B1</c>.
    /// </summary>
    /// <returns>The root sequence.</returns>
    private static Node[] BuildCompositionTree() =>
    [
        new Node
        {
            Name = "A",
            Children =
            {
                new Node { Name = "A1" },
                new Node { Name = "A2" },
            },
        },
        new Node
        {
            Name = "B",
            Children =
            {
                new Node { Name = "B1" },
            },
        },
    ];

    /// <summary>
    /// Verifies that a combined <see cref="RecursiveSelectControl.Break" /> and
    /// <see cref="RecursiveSelectControl.Recurse" /> value composes: the current element's children are visited before
    /// the remaining siblings are skipped. This is the previously broken case where <c>Break</c> was evaluated ahead of
    /// <c>Recurse</c> and silently prevented all recursion.
    /// </summary>
    [TestMethod]
    public void RecursiveSelectInternal_WhenBreakAndRecurseCombined_ShouldRecurseThenStopSiblings()
    {
        Node[] roots = BuildCompositionTree();

        IEnumerable<Node> ChildSelector(Node n) => n.Children;
        string Selector(Node n, int index, int depth) => n.Name;

        RecursiveSelectControl RecursionControl(Node n)
            => n.Name == "A"
                ? RecursiveSelectControl.Yield | RecursiveSelectControl.Break | RecursiveSelectControl.Recurse
                : RecursiveSelectControl.YieldOnly;

        var actual = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(roots, ChildSelector, Selector, RecursionControl, 0)
            .ToList();

        // A yielded, its children A1/A2 visited (Recurse), then sibling B skipped (Break).
        CollectionAssert.AreEqual(new[] { "A", "A1", "A2" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="RecursiveSelectControl.Break" /> without <see cref="RecursiveSelectControl.Recurse" />
    /// yields the current element, does not descend into its children, and stops the remaining siblings.
    /// </summary>
    [TestMethod]
    public void RecursiveSelectInternal_WhenBreakWithoutRecurse_ShouldYieldCurrentAndStopSiblings()
    {
        Node[] roots = BuildCompositionTree();

        IEnumerable<Node> ChildSelector(Node n) => n.Children;
        string Selector(Node n, int index, int depth) => n.Name;

        RecursiveSelectControl RecursionControl(Node n)
            => n.Name == "A"
                ? RecursiveSelectControl.Yield | RecursiveSelectControl.Break
                : RecursiveSelectControl.YieldAndRecurse;

        var actual = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(roots, ChildSelector, Selector, RecursionControl, 0)
            .ToList();

        // A yielded; no recurse into A1/A2; sibling B skipped.
        CollectionAssert.AreEqual(new[] { "A" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="RecursiveSelectControl.Recurse" /> without <see cref="RecursiveSelectControl.Break" />
    /// descends into the current element's children and then continues on to the remaining siblings.
    /// </summary>
    [TestMethod]
    public void RecursiveSelectInternal_WhenRecurseWithoutBreak_ShouldRecurseThenContinueSiblings()
    {
        Node[] roots = BuildCompositionTree();

        IEnumerable<Node> ChildSelector(Node n) => n.Children;
        string Selector(Node n, int index, int depth) => n.Name;

        RecursiveSelectControl RecursionControl(Node n)
            => n.Name == "A"
                ? RecursiveSelectControl.Recurse
                : RecursiveSelectControl.YieldOnly;

        var actual = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(roots, ChildSelector, Selector, RecursionControl, 0)
            .ToList();

        // A not yielded but recursed (A1/A2 yielded); sibling B then reached and yielded.
        CollectionAssert.AreEqual(new[] { "A1", "A2", "B" }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="RecursiveSelectControl.None" /> (no flags set) neither yields the current element nor
    /// recurses into its children, while still advancing to the remaining siblings.
    /// </summary>
    [TestMethod]
    public void RecursiveSelectInternal_WhenNoFlagsSet_ShouldNeitherYieldNorRecurse()
    {
        Node[] roots = BuildCompositionTree();

        IEnumerable<Node> ChildSelector(Node n) => n.Children;
        string Selector(Node n, int index, int depth) => n.Name;

        RecursiveSelectControl RecursionControl(Node n) => RecursiveSelectControl.None;

        var actual = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(roots, ChildSelector, Selector, RecursionControl, 0)
            .ToList();

        Assert.IsEmpty(actual);
    }
}
