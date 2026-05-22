// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.IndexOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed class IListExtensionsTests_IndexOf
{

    /// <summary>
    /// Provides multi-match scenarios for verifying that <c>IndexOf</c> always returns the first matching index.
    /// </summary>
    public static IEnumerable<object[]> FirstMatchData =>
    [
        [new[] { 5, 1, 5, 5, 5 }, 0],
        [new[] { 1, 5, 5, 5, 5 }, 1],
        [new[] { 1, 1, 1, 1, 5 }, 4],
        [new[] { 5, 5 },          0],
    ];

    /// <summary>
    /// Verifies that the 3-argument <c>IndexOf</c> overload throws <see cref="ArgumentOutOfRangeException"/>
    /// when <paramref name="index"/> exceeds <see cref="ICollection{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_ThreeArg_WhenIndexExceedsCount_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, 10, 0);
        });
    }

    /// <summary>
    /// Verifies that the 3-argument <c>IndexOf</c> overload throws <see cref="ArgumentOutOfRangeException"/>
    /// when <paramref name="index"/> is negative, exercising the index-validation branch of that overload.
    /// </summary>
    [TestMethod]
    public void IndexOf_ThreeArg_WhenIndexIsNegative_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, -1, 1);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="count"/> is negative.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenCountIsNegative_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, 0, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> accepts <c>index == Count</c> (yielding an empty search) without throwing.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexEqualsCount_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        var index = list.IndexOf(x => x == 1, list.Count);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="index"/>
    /// exceeds <see cref="ICollection{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexExceedsCount_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, 4);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when the start index is negative.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexIsNegative_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <c>index + count</c> exceeds the list size.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexPlusCountExceedsCount_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, 1, 5);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> returns <c>-1</c> on an empty list rather than throwing.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenListIsEmpty_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int>();

        var index = list.IndexOf(_ => true);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> works against an <see cref="IList{T}"/> implementation that is not <see cref="List{T}"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenListIsNotSystemList_ShouldReturnIndex()
    {
        IList<string> list = new Collection<string> { "a", "b", "c" };

        var index = list.IndexOf(x => x == "b");

        Assert.AreEqual(1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentNullException"/> when the list is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenListIsNull_ShouldThrowExactly()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.IndexOf(x => x == 0);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> returns the zero-based index of the first matching element when one exists.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenMatchExists_ShouldReturnFirstMatchingIndex()
    {
        IList<int> list = new List<int> { 1, 2, 3, 2, 1 };

        var index = list.IndexOf(x => x == 2);

        Assert.AreEqual(1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> returns the first matching index when multiple elements satisfy the predicate.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FirstMatchData))]
    public void IndexOf_WhenMultipleMatches_ShouldReturnFirstMatchingIndex(int[] data, int expected)
    {
        IList<int> list = new List<int>(data);

        var index = list.IndexOf(x => x == 5);

        Assert.AreEqual(expected, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> returns <c>-1</c> when no element satisfies the predicate.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenNoElementMatches_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        var index = list.IndexOf(x => x > 100);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentNullException"/> when the predicate is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenPredicateIsNull_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1 };
        Func<int, bool>? predicate = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list.IndexOf(predicate!);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> with <paramref name="count"/> equal to <c>0</c> on a non-empty list
    /// returns <c>-1</c> without invoking the predicate.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenCountIsZeroOnNonEmptyList_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        var predicateCalls = 0;

        var index = list.IndexOf(_ => { predicateCalls++; return true; }, 1, 0);

        Assert.AreEqual(-1, index);
        Assert.AreEqual(0, predicateCalls);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> can locate a match at the exact first slot of the search window.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenMatchIsAtFirstSlotOfWindow_ShouldReturnWindowStart()
    {
        IList<int> list = new List<int> { 0, 5, 5, 0 };

        var index = list.IndexOf(x => x == 5, 1, 2);

        Assert.AreEqual(1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> can locate a match at the exact last slot of the search window.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenMatchIsAtLastSlotOfWindow_ShouldReturnWindowEnd()
    {
        IList<int> list = new List<int> { 0, 0, 5, 0 };

        var index = list.IndexOf(x => x == 5, 0, 3);

        Assert.AreEqual(2, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>IndexOf</c> returns the match when it lies inside the window.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenMatchIsInsideWindow_ShouldReturnIndex()
    {
        IList<int> list = new List<int> { 0, 0, 1, 0, 0 };

        var index = list.IndexOf(x => x == 1, 1, 3);

        Assert.AreEqual(2, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>IndexOf</c> honours <paramref name="count"/> by ignoring
    /// matches outside the window.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenMatchIsOutsideWindow_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 0, 0, 1, 0, 0 };

        var index = list.IndexOf(x => x == 1, 0, 2);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> with a ranged search returns the first matching index inside the window
    /// when the prefix also contains matches.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenPrefixContainsMatches_ShouldReturnFirstMatchInsideWindow()
    {
        IList<int> list = new List<int> { 5, 5, 5, 5, 5 };

        var index = list.IndexOf(x => x == 5, 2, 2);

        Assert.AreEqual(2, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> with reference-type elements behaves identically to the value-type case.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithReferenceTypes_ShouldReturnFirstMatchingIndex()
    {
        IList<string?> list = new List<string?> { "a", null, "b", null };

        Assert.AreEqual(1, list.IndexOf(x => x is null));
    }

    /// <summary>
    /// Verifies that the indexed overload of <c>IndexOf</c> begins the search at the specified index.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithStartIndex_WhenIndexExcludesEarlyMatch_ShouldReturnFirstMatchInRange()
    {
        IList<int> list = new List<int> { 2, 0, 0, 2, 0, 2 };

        var index = list.IndexOf(x => x == 2, 1);

        Assert.AreEqual(3, index);
    }

}
