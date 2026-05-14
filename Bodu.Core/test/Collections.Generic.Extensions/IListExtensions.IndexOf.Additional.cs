// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.IndexOf.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IListExtensionsTests_IndexOf
{
    /// <summary>
    /// Provides multi-match scenarios for verifying that <c>IndexOf</c> always returns the first matching index.
    /// </summary>
    public static IEnumerable<object[]> FirstMatchData =>
    [
        new object[] { new[] { 5, 1, 5, 5, 5 }, 0 },
        new object[] { new[] { 1, 5, 5, 5, 5 }, 1 },
        new object[] { new[] { 1, 1, 1, 1, 5 }, 4 },
        new object[] { new[] { 5, 5 },          0 },
    ];

    /// <summary>
    /// Verifies that <c>IndexOf</c> returns the first matching index when multiple elements satisfy the predicate.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(FirstMatchData))]
    public void IndexOf_WhenMultipleMatches_ShouldReturnFirstMatchingIndex(int[] data, int expected)
    {
        IList<int> list = new List<int>(data);

        int index = list.IndexOf(x => x == 5);

        Assert.AreEqual(expected, index);
    }

    /// <summary>
    /// Verifies that <c>IndexOf</c> with a ranged search returns the first matching index inside the window
    /// when the prefix also contains matches.
    /// </summary>
    [TestMethod]
    public void IndexOf_WithRange_WhenPrefixContainsMatches_ShouldReturnFirstMatchInsideWindow()
    {
        IList<int> list = new List<int> { 5, 5, 5, 5, 5 };

        int index = list.IndexOf(x => x == 5, 2, 2);

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
}
