// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.LastIndexOf.MoreCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IListExtensionsTests_LastIndexOf
{
    /// <summary>
    /// Verifies that <c>LastIndexOf</c> with <paramref name="count"/> equal to <c>0</c> on a non-empty list
    /// returns <c>-1</c> without invoking the predicate.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenCountIsZeroOnNonEmptyList_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        int predicateCalls = 0;

        int index = list.LastIndexOf(_ => { predicateCalls++; return true; }, 2, 0);

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

        int index = list.LastIndexOf(x => x == 5, 3, 4);

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

        int index = list.LastIndexOf(x => x == 5, 3, 4);

        Assert.AreEqual(3, index);
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
}
