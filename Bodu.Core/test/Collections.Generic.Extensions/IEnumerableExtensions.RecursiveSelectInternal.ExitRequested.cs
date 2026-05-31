// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.RecursiveSelectInternal.ExitRequested.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Extensions;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_RecursiveSelectInternal
{

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.RecursiveSelectInternal{TSource,TResult}" /> short-circuits at
    /// the top of the outer foreach via the line-279 check when <c>state.ExitRequested</c> is set before evaluation
    /// reaches the next sibling element.
    /// </summary>
    [TestMethod]
    public void RecursiveSelectInternal_WhenExitRequestedFires_ShouldStopBeforeNextSibling()
    {
        var state = new IEnumerableExtensions.RecursionState();
        Node[] roots =
        [
            new Node { Name = "A" },
            new Node { Name = "B" },
            new Node { Name = "C" },
        ];

        IEnumerable<Node> ChildSelector(Node n) => n.Children;
        string Selector(Node n, int index, int depth) => n.Name;

        RecursiveSelectControl RecursionControl(Node n)
            => n.Name == "A" ? RecursiveSelectControl.YieldAndExit : RecursiveSelectControl.YieldAndRecurse;

        var actual = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(roots, ChildSelector, Selector, RecursionControl, 0, state)
            .ToList();

        CollectionAssert.AreEqual(new[] { "A" }, actual);
    }
    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.RecursiveSelectInternal{TSource,TResult}" /> short-circuits
    /// inside the inner recursive consumption loop via the <c>state.ExitRequested</c> check (line 315-316) when the
    /// flag is flipped to <see langword="true" /> after the child iterator yields its first value.
    /// </summary>
    /// <remarks>
    /// The recursion control delegate returns <see cref="RecursiveSelectControl.YieldAndRecurse" /> for the parent so
    /// that the iterator enters the recurse-children branch, and returns
    /// <see cref="RecursiveSelectControl.YieldOnly" /> for the child while flipping
    /// <see cref="IEnumerableExtensions.RecursionState.ExitRequested" /> to <see langword="true" /> as a side effect.
    /// As a result the child is yielded to the outer iterator, the outer iterator observes the elevated flag on the
    /// next iteration of its inner-foreach over the child enumerator, and short-circuits at the inner check before
    /// yielding the child onward.
    /// </remarks>
    [TestMethod]
    public void RecursiveSelectInternal_WhenStateExitsAfterChildYields_ShouldYieldBreakAtInnerCheck()
    {
        var state = new IEnumerableExtensions.RecursionState();
        Node[] root =
        [
            new Node
            {
                Name = "Root",
                Children =
                {
                    new Node { Name = "Child" },
                },
            },
        ];

        IEnumerable<Node> ChildSelector(Node n) => n.Children;
        string Selector(Node n, int index, int depth) => n.Name;

        // Yield Root, recurse into children; on Child, set ExitRequested but return YieldOnly so the child yields successfully and
        // the outer iterator hits line 315-316 on its next inner-foreach iteration.
        RecursiveSelectControl RecursionControl(Node n)
        {
            if (n.Name == "Child")
            {
                state.ExitRequested = true;
                return RecursiveSelectControl.YieldOnly;
            }

            return RecursiveSelectControl.YieldAndRecurse;
        }

        var actual = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(root, ChildSelector, Selector, RecursionControl, 0, state)
            .ToList();

        // Root yielded; Child consumed by the inner-foreach but blocked by the inner ExitRequested check before being passed onward.
        CollectionAssert.AreEqual(new[] { "Root" }, actual);
        Assert.IsTrue(state.ExitRequested, "Recursion state should remain in the exit-requested condition after traversal stops.");
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.RecursiveSelectInternal{TSource,TResult}" /> initialises a fresh
    /// <see cref="IEnumerableExtensions.RecursionState" /> when one is not supplied, so two independent enumerations of
    /// the same source produce identical output.
    /// </summary>
    [TestMethod]
    public void RecursiveSelectInternal_WhenStateIsNotSupplied_ShouldInitialiseFreshState()
    {
        Node[] root =
        [
            new Node
            {
                Name = "Root",
                Children =
                {
                    new Node { Name = "Child" },
                },
            },
        ];

        var first = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(
                root,
                n => n.Children,
                (n, _, _) => n.Name,
                _ => RecursiveSelectControl.YieldAndRecurse,
                0)
            .ToList();

        var second = IEnumerableExtensions
            .RecursiveSelectInternal<Node, string>(
                root,
                n => n.Children,
                (n, _, _) => n.Name,
                _ => RecursiveSelectControl.YieldAndRecurse,
                0)
            .ToList();

        CollectionAssert.AreEqual(first, second);
    }

}
