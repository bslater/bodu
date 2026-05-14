// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.TrySwap.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IListExtensionsTests_TrySwap
{
    /// <summary>
    /// Provides valid <c>TrySwap</c> scenarios covering adjacent, non-adjacent, endpoint, and
    /// same-index no-op swaps.
    /// </summary>
    public static IEnumerable<object[]> GetTrySwapTestCases() =>
    [
        [new[] { 1, 2, 3, 4 }, 0, 1, new[] { 2, 1, 3, 4 }],
        [new[] { 1, 2, 3, 4 }, 1, 2, new[] { 1, 3, 2, 4 }],
        [new[] { 1, 2, 3, 4 }, 0, 3, new[] { 4, 2, 3, 1 }],
        [new[] { 1, 2, 3, 4 }, 2, 2, new[] { 1, 2, 3, 4 }],
    ];

    /// <summary>
    /// Verifies that <c>TrySwap</c> swaps elements at the specified indices and returns <see langword="true" />
    /// across adjacent, non-adjacent, endpoint, and same-index cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetTrySwapTestCases))]
    public void TrySwap_WhenArgumentsAreValid_ForIndexScenarios_ShouldSwapElementsAndReturnTrue(
        int[] initial,
        int indexA,
        int indexB,
        int[] expected)
    {
        var list = new List<int>(initial);

        var result = list.TrySwap(indexA, indexB);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(expected, list);
    }

    /// <summary>
    /// Verifies that <c>TrySwap</c> with equal indices returns <see langword="true" /> and leaves the list untouched.
    /// </summary>
    [TestMethod]
    public void TrySwap_WhenIndicesAreEqual_ShouldReturnTrueWithoutMutating()
    {
        var list = new List<string> { "a", "b", "c" };

        var result = list.TrySwap(1, 1);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, list);
    }

    /// <summary>
    /// Verifies that <c>TrySwap</c> returns <see langword="false" /> without mutating the list when either index is out of range.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0, DisplayName = "Negative indexA")]
    [DataRow(0, -1, DisplayName = "Negative indexB")]
    [DataRow(4, 0, DisplayName = "indexA equals Count")]
    [DataRow(0, 4, DisplayName = "indexB equals Count")]
    [DataRow(10, 0, DisplayName = "indexA greater than Count")]
    public void TrySwap_WhenIndexIsOutOfRange_ShouldReturnFalseWithoutMutating(int indexA, int indexB)
    {
        var list = new List<int> { 1, 2, 3, 4 };

        var result = list.TrySwap(indexA, indexB);

        Assert.IsFalse(result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, list);
    }

    /// <summary>
    /// Verifies that <c>TrySwap</c> throws <see cref="ArgumentNullException" /> when the list is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TrySwap_WhenListIsNull_ShouldThrowArgumentNullException()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.TrySwap(0, 1);
        });
    }

    /// <summary>
    /// Verifies that <c>TrySwap</c> works with a non-<see cref="List{T}"/> <see cref="IList{T}"/> implementation
    /// (<see cref="Collection{T}"/>), exercising the general path through the list interface.
    /// </summary>
    [TestMethod]
    public void TrySwap_WhenListIsNotSystemList_ShouldSwapElements()
    {
        IList<string> list = new Collection<string> { "a", "b", "c", "d" };

        var result = list.TrySwap(0, 3);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(new[] { "d", "b", "c", "a" }, (Collection<string>)list);
    }

    /// <summary>
    /// Verifies that <c>TrySwap</c> on an empty list always returns <see langword="false" /> and does not mutate the list.
    /// </summary>
    [TestMethod]
    public void TrySwap_WhenListIsEmpty_ShouldReturnFalse()
    {
        var list = new List<int>();

        Assert.IsFalse(list.TrySwap(0, 0));
        Assert.AreEqual(0, list.Count);
    }
}
