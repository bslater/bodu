// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.LastIndexOf.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed class IListExtensionsTests_LastIndexOf
{

    /// <summary>
    /// Provides multi-match scenarios for verifying that <c>LastIndexOf</c> always returns the last matching index.
    /// </summary>
    public static IEnumerable<object[]> LastMatchData =>
    [
        [new[] { 5, 1, 5, 5, 5 }, 4],
        [new[] { 5, 5, 5, 5, 1 }, 3],
        [new[] { 5, 1, 1, 1, 1 }, 0],
        [new[] { 5, 5 },          1],
    ];

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when the backward range
    /// extends before the start of the list.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenCountExtendsBeforeStart_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, 1, 5);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="count"/>
    /// is negative.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenCountIsNegative_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, 2, -1);
        });
    }

    /// <summary>
    /// Verifies that the no-arg <c>LastIndexOf</c> overload returns <c>-1</c> on a single-element list
    /// when the only element does not match.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListHasSingleNonMatchingElement_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 7 };

        Assert.AreEqual(-1, list.LastIndexOf(x => x == 5));
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns <c>-1</c> on an empty list without throwing.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsEmpty_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int>();

        var index = list.LastIndexOf(_ => true);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> accepts <c>startIndex == -1</c> on an empty list and returns <c>-1</c>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsEmptyAndStartIndexIsMinusOne_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int>();

        var index = list.LastIndexOf(x => x == 1, -1);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when the list is
    /// empty and <paramref name="startIndex"/> is anything other than <c>-1</c>, exercising the empty-list
    /// branch of the validator.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(-2)]
    public void LastIndexOf_WhenListIsEmptyAndStartIndexIsNotMinusOne_ShouldThrowExactly(int startIndex)
    {
        IList<int> list = new List<int>();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, startIndex);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> works against an <see cref="IList{T}"/> implementation that is not <see cref="List{T}"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsNotSystemList_ShouldReturnIndex()
    {
        IList<string> list = new Collection<string> { "a", "b", "c", "b" };

        var index = list.LastIndexOf(x => x == "b");

        Assert.AreEqual(3, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentNullException"/> when the list is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenListIsNull_ShouldThrowExactly()
    {
        IList<int>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.LastIndexOf(x => x == 0);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns the index of the last matching element when one exists.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenMatchExists_ShouldReturnLastMatchingIndex()
    {
        IList<int> list = new List<int> { 1, 2, 3, 2, 1 };

        var index = list.LastIndexOf(x => x == 2);

        Assert.AreEqual(3, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns the last matching index when multiple elements satisfy the predicate.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LastMatchData))]
    public void LastIndexOf_WhenMultipleMatches_ShouldReturnLastMatchingIndex(int[] data, int expected)
    {
        IList<int> list = new List<int>(data);

        var index = list.LastIndexOf(x => x == 5);

        Assert.AreEqual(expected, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns <c>-1</c> when no element satisfies the predicate.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenNoElementMatches_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        var index = list.LastIndexOf(x => x > 100);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentNullException"/> when the predicate is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenPredicateIsNull_ShouldThrowExactly()
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
    /// <paramref name="startIndex"/> equals <see cref="ICollection{T}.Count"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenStartIndexEqualsCount_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, list.Count);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> throws <see cref="ArgumentOutOfRangeException"/> when
    /// <paramref name="startIndex"/> is negative on a non-empty list.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WhenStartIndexIsNegative_ShouldThrowExactly()
    {
        IList<int> list = new List<int> { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = list.LastIndexOf(x => x == 1, -1);
        });
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> with <paramref name="count"/> equal to <c>0</c> on a non-empty list
    /// returns <c>-1</c> without invoking the predicate.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenCountIsZeroOnNonEmptyList_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        var predicateCalls = 0;

        var index = list.LastIndexOf(_ => { predicateCalls++; return true; }, 2, 0);

        Assert.AreEqual(-1, index);
        Assert.AreEqual(0, predicateCalls);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> can locate a match at the exact first slot of the backward search range.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenMatchIsAtRangeStart_ShouldReturnIndex()
    {
        IList<int> list = new List<int> { 5, 0, 0, 0 };

        var index = list.LastIndexOf(x => x == 5, 3, 4);

        Assert.AreEqual(0, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> can locate a match at the exact startIndex (last slot of the
    /// backward range).
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenMatchIsAtStartIndex_ShouldReturnStartIndex()
    {
        IList<int> list = new List<int> { 0, 0, 0, 5 };

        var index = list.LastIndexOf(x => x == 5, 3, 4);

        Assert.AreEqual(3, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>LastIndexOf</c> returns the match when it lies inside the window.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenMatchIsInsideWindow_ShouldReturnIndex()
    {
        IList<int> list = new List<int> { 1, 0, 0, 0, 0 };

        var index = list.LastIndexOf(x => x == 1, 4, 5);

        Assert.AreEqual(0, index);
    }

    /// <summary>
    /// Verifies that the ranged overload of <c>LastIndexOf</c> honours <paramref name="count"/> by ignoring matches
    /// outside the window.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenMatchIsOutsideWindow_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 0, 0, 0, 0 };

        var index = list.LastIndexOf(x => x == 1, 4, 4);

        Assert.AreEqual(-1, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> with a ranged search returns the last matching index inside the window
    /// when the suffix also contains matches.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenSuffixContainsMatches_ShouldReturnLastMatchInsideWindow()
    {
        IList<int> list = new List<int> { 5, 5, 5, 5, 5 };

        var index = list.LastIndexOf(x => x == 5, 2, 2);

        Assert.AreEqual(2, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> with reference-type elements correctly locates the final occurrence.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithReferenceTypes_ShouldReturnLastMatchingIndex()
    {
        IList<string?> list = new List<string?> { null, "a", null, "b" };

        Assert.AreEqual(2, list.LastIndexOf(x => x is null));
    }

    /// <summary>
    /// Verifies that the indexed overload of <c>LastIndexOf</c> searches backward from <paramref name="startIndex"/>.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithStartIndex_WhenIndexExcludesLaterMatch_ShouldReturnLastMatchInPrefix()
    {
        IList<int> list = new List<int> { 2, 0, 2, 0, 0, 2 };

        var index = list.LastIndexOf(x => x == 2, 3);

        Assert.AreEqual(2, index);
    }

}
