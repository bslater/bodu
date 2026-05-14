// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.IndexOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IListExtensionsTests_IndexOf
{
    /// <summary>
    /// Verifies that <c>IndexOf</c> returns the zero-based index of the first matching element when one exists.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenMatchExists_ShouldReturnFirstMatchingIndex()
    {
        IList<int> list = new List<int> { 1, 2, 3, 2, 1 };

        int index = list.IndexOf(x => x == 2);

        Assert.AreEqual(1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> returns <c>-1</c> when no element satisfies the predicate.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenNoElementMatches_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        int index = list.IndexOf(x => x > 100);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> returns <c>-1</c> on an empty list rather than throwing.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenListIsEmpty_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int>();

        int index = list.IndexOf(_ => true);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that the indexed overload of <c>IndexOf</c> begins the search at the specified index.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithStartIndex_WhenIndexExcludesEarlyMatch_ShouldReturnFirstMatchInRange()
    {
        IList<int> list = new List<int> { 2, 0, 0, 2, 0, 2 };

        int index = list.IndexOf(x => x == 2, 1);

        Assert.AreEqual(3, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>IndexOf</c> honours <paramref name="count"/> by ignoring
    /// matches outside the window.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenMatchIsOutsideWindow_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 0, 0, 1, 0, 0 };

        int index = list.IndexOf(x => x == 1, 0, 2);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>IndexOf</c> returns the match when it lies inside the window.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenMatchIsInsideWindow_ShouldReturnIndex()
    {
        IList<int> list = new List<int> { 0, 0, 1, 0, 0 };

        int index = list.IndexOf(x => x == 1, 1, 3);

        Assert.AreEqual(2, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentNullException"/> when the list is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenListIsNull_ShouldThrowArgumentNullException()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.IndexOf(x => x == 0);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentNullException"/> when the predicate is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenPredicateIsNull_ShouldThrowArgumentNullException()
    {
        IList<int> list = new List<int> { 1 };
        Func<int, bool>? predicate = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list.IndexOf(predicate!);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when the start index is negative.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="index"/>
    /// exceeds <see cref="ICollection{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexExceedsCount_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, 4);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> accepts <c>index == Count</c> (yielding an empty search) without throwing.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexEqualsCount_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        int index = list.IndexOf(x => x == 1, list.Count);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="count"/> is negative.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, 0, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <c>index + count</c> exceeds the list size.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenIndexPlusCountExceedsCount_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.IndexOf(x => x == 1, 1, 5);
        });
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> works against an <see cref="IList{T}"/> implementation that is not <see cref="List{T}"/>.
    /// </summary>
    [TestMethod]
    public void IndexOf_WhenListIsNotSystemList_ShouldReturnIndex()
    {
        IList<string> list = new Collection<string> { "a", "b", "c" };

        int index = list.IndexOf(x => x == "b");

        Assert.AreEqual(1, index);
    }
}
