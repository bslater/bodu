// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.LastIndexOf.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IListExtensionsTests_LastIndexOf
{
    /// <summary>
    /// Provides multi-match scenarios for verifying that <c>LastIndexOf</c> always returns the last matching index.
    /// </summary>
    public static IEnumerable<object[]> LastMatchData =>
    [
        new object[] { new[] { 5, 1, 5, 5, 5 }, 4 },
        new object[] { new[] { 5, 5, 5, 5, 1 }, 3 },
        new object[] { new[] { 5, 1, 1, 1, 1 }, 0 },
        new object[] { new[] { 5, 5 },          1 },
    ];

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> returns the last matching index when multiple elements satisfy the predicate.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(LastMatchData))]
    public void LastIndexOf_WhenMultipleMatches_ShouldReturnLastMatchingIndex(int[] data, int expected)
    {
        IList<int> list = new List<int>(data);

        int index = list.LastIndexOf(x => x == 5);

        Assert.AreEqual(expected, index);
    }

    /// <summary>
    /// Verifies that <c>LastIndexOf</c> with a ranged search returns the last matching index inside the window
    /// when the suffix also contains matches.
    /// </summary>
    [TestMethod]
    public void LastIndexOf_WithRange_WhenSuffixContainsMatches_ShouldReturnLastMatchInsideWindow()
    {
        IList<int> list = new List<int> { 5, 5, 5, 5, 5 };

        int index = list.LastIndexOf(x => x == 5, 2, 2);

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
}
