// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.LastIndexOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IListExtensionsTests_LastIndexOf
{
    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns the index of the last matching element when one exists.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenMatchExists_ShouldReturnLastMatchingIndex()
    {
        IList<int> list = new List<int> { 1, 2, 3, 2, 1 };

        int index = list.LastIndexOf(x => x == 2);

        Assert.AreEqual(3, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns <c>-1</c> when no element satisfies the predicate.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenNoElementMatches_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        int index = list.LastIndexOf(x => x > 100);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns <c>-1</c> on an empty list without throwing.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsEmpty_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int>();

        int index = list.LastIndexOf(_ => true);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that the indexed overload of <c>LastIndexOf</c> searches backward from <paramref name="startIndex"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithStartIndex_WhenIndexExcludesLaterMatch_ShouldReturnLastMatchInPrefix()
    {
        IList<int> list = new List<int> { 2, 0, 2, 0, 0, 2 };

        int index = list.LastIndexOf(x => x == 2, 3);

        Assert.AreEqual(2, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>LastIndexOf</c> honours <paramref name="count"/> by ignoring matches
    /// outside the window.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenMatchIsOutsideWindow_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 0, 0, 0, 0 };

        int index = list.LastIndexOf(x => x == 1, 4, 4);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>LastIndexOf</c> returns the match when it lies inside the window.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenMatchIsInsideWindow_ShouldReturnIndex()
    {
        IList<int> list = new List<int> { 1, 0, 0, 0, 0 };

        int index = list.LastIndexOf(x => x == 1, 4, 5);

        Assert.AreEqual(0, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentNullException"/> when the list is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsNull_ShouldThrowArgumentNullException()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.LastIndexOf(x => x == 0);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentNullException"/> when the predicate is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenPredicateIsNull_ShouldThrowArgumentNullException()
    {
        IList<int> list = new List<int> { 1 };
        Func<int, bool>? predicate = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list.LastIndexOf(predicate!);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <paramref name="startIndex"/> is negative on a non-empty list.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenStartIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <paramref name="startIndex"/> equals <see cref="ICollection{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenStartIndexEqualsCount_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, list.Count);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> accepts <c>startIndex == -1</c> on an empty list and returns <c>-1</c>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsEmptyAndStartIndexIsMinusOne_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int>();

        int index = list.LastIndexOf(x => x == 1, -1);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="count"/>
    /// is negative.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenCountIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, 2, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when the backward range
    /// extends before the start of the list.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenCountExtendsBeforeStart_ShouldThrowArgumentOutOfRangeException()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, 1, 5);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> works against an <see cref="IList{T}"/> implementation that is not <see cref="List{T}"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsNotSystemList_ShouldReturnIndex()
    {
        IList<string> list = new Collection<string> { "a", "b", "c", "b" };

        int index = list.LastIndexOf(x => x == "b");

        Assert.AreEqual(3, index);
    }
}
