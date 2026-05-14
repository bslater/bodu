// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.IndexOf.MoreCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IListExtensionsTests_IndexOf
{
    /// <summary>
    /// Verifies that <c>IndexOf</c> with <paramref name="count"/> equal to <c>0</c> on a non-empty list
    /// returns <c>-1</c> without invoking the predicate.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenCountIsZeroOnNonEmptyList_ShouldReturnMinusOne()
    {
        IList<int> list = new List<int> { 1, 2, 3 };
        int predicateCalls = 0;

        int index = list.IndexOf(_ => { predicateCalls++; return true; }, 1, 0);

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

        int index = list.IndexOf(x => x == 5, 1, 2);

        Assert.AreEqual(1, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> can locate a match at the exact last slot of the search window.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenMatchIsAtLastSlotOfWindow_ShouldReturnWindowEnd()
    {
        IList<int> list = new List<int> { 0, 0, 5, 0 };

        int index = list.IndexOf(x => x == 5, 0, 3);

        Assert.AreEqual(2, index);
    }
}
