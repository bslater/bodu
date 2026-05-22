// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.TryMove.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IListExtensionsTests_TryMove
{

    /// <summary>
    /// Provides valid <c>TryMove</c> scenarios covering left-moves, right-moves, move-to-end,
    /// and same-position no-ops.
    /// </summary>
    public static IEnumerable<object[]> GetTryMoveTestCases() =>
    [
        [new[] { "a", "b", "c", "d", "e" }, 3, 0, new[] { "d", "a", "b", "c", "e" }],
        [new[] { "a", "b", "c", "d", "e" }, 0, 4, new[] { "b", "c", "d", "a", "e" }],
        [new[] { "a", "b", "c", "d", "e" }, 0, 5, new[] { "b", "c", "d", "e", "a" }],
        [new[] { "a", "b", "c", "d", "e" }, 2, 2, new[] { "a", "b", "c", "d", "e" }],
        [new[] { "a", "b", "c", "d", "e" }, 2, 3, new[] { "a", "b", "c", "d", "e" }],
        [new[] { "a", "b" }, 0, 2, new[] { "b", "a" }],
    ];

    /// <summary>
    /// Verifies that <c>TryMove</c> correctly repositions the specified element and returns <see langword="true" />
    /// for in-range valid scenarios, including same-position and adjacent-insertion-point no-ops.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetTryMoveTestCases))]
    public void TryMove_WhenArgumentsAreValid_ForIndexScenarios_ShouldMoveElementAndReturnTrue(
        string[] initial,
        int oldIndex,
        int newIndex,
        string[] expected)
    {
        var list = new List<string>(initial);

        var result = list.TryMove(oldIndex, newIndex);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(expected, list);
    }

    /// <summary>
    /// Verifies that <c>TryMove</c> returns <see langword="false" /> and leaves the list unmodified when an index is out of range.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0, DisplayName = "Negative oldIndex")]
    [DataRow(0, -1, DisplayName = "Negative newIndex")]
    [DataRow(5, 0, DisplayName = "oldIndex equals Count")]
    [DataRow(10, 0, DisplayName = "oldIndex greater than Count")]
    [DataRow(0, 6, DisplayName = "newIndex greater than Count")]
    public void TryMove_WhenIndexIsOutOfRange_ShouldReturnFalseWithoutMutating(int oldIndex, int newIndex)
    {
        var list = new List<int> { 1, 2, 3, 4, 5 };

        var result = list.TryMove(oldIndex, newIndex);

        Assert.IsFalse(result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list);
    }

    /// <summary>
    /// Verifies that <c>TryMove</c> on a single-element list is a no-op and returns <see langword="true" />
    /// when called with <c>(0, 0)</c> or <c>(0, 1)</c>, and returns <see langword="false" /> for invalid indices.
    /// </summary>
    [TestMethod]
    public void TryMove_WhenListContainsOneElement_ShouldHandleAllValidPositions()
    {
        var list = new List<int> { 42 };

        Assert.IsTrue(list.TryMove(0, 0));
        Assert.IsTrue(list.TryMove(0, 1));
        Assert.IsFalse(list.TryMove(1, 0));

        CollectionAssert.AreEqual(new[] { 42 }, list);
    }

    /// <summary>
    /// Verifies that <c>TryMove</c> works with a non-<see cref="List{T}"/> <see cref="IList{T}"/> implementation
    /// (<see cref="Collection{T}"/>), exercising the general path through the list interface.
    /// </summary>
    [TestMethod]
    public void TryMove_WhenListIsNotSystemList_ShouldMoveElement()
    {
        IList<string> list = new Collection<string> { "a", "b", "c", "d" };

        var result = list.TryMove(0, 3);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(new[] { "b", "c", "a", "d" }, (Collection<string>)list);
    }

    /// <summary>
    /// Verifies that <c>TryMove</c> throws <see cref="ArgumentNullException" /> when the list is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryMove_WhenListIsNull_ShouldThrowExactly()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.TryMove(0, 1);
        });
    }

    /// <summary>
    /// Verifies that <c>TryMove</c> returns <see langword="true" /> without mutating when <paramref name="newIndex"/>
    /// is one greater than <paramref name="oldIndex"/>, matching the method's documented no-op contract.
    /// </summary>
    [TestMethod]
    public void TryMove_WhenNewIndexIsAdjacentToOldIndex_ShouldReturnTrueWithoutMutating()
    {
        var list = new List<int> { 10, 20, 30, 40 };

        var result = list.TryMove(1, 2);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(new[] { 10, 20, 30, 40 }, list);
    }

    /// <summary>
    /// Verifies that <c>TryMove</c> returns <see langword="true" /> without mutating the list when <paramref name="oldIndex"/>
    /// equals <paramref name="newIndex"/>.
    /// </summary>
    [TestMethod]
    public void TryMove_WhenOldIndexEqualsNewIndex_ShouldReturnTrueWithoutMutating()
    {
        var list = new List<int> { 10, 20, 30, 40 };

        var result = list.TryMove(2, 2);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(new[] { 10, 20, 30, 40 }, list);
    }

}
