// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IListExtensions.ReplaceAll.Additional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

public sealed partial class IListExtensionsTests_ReplaceAll
{
    /// <summary>
    /// Verifies that the <c>IEqualityComparer&lt;T&gt;</c> overload throws <see cref="ArgumentNullException"/>
    /// when the list is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithComparer_WhenListIsNull_ShouldThrowArgumentNullException()
    {
        IList<string>? list = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = list!.ReplaceAll("a", "b", StringComparer.Ordinal);
        });
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll</c> with reference-type elements replaces matching non-null values.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WithReferenceTypes_ShouldReplaceMatches()
    {
        var list = new List<string> { "a", "b", "a", "c", "a" };

        int replaced = list.ReplaceAll("a", "z");

        Assert.AreEqual(3, replaced);
        CollectionAssert.AreEqual(new[] { "z", "b", "z", "c", "z" }, list);
    }

    /// <summary>
    /// Verifies that <c>ReplaceAll</c> on a single-element list works for the match and non-match cases.
    /// </summary>
    [TestMethod]
    public void ReplaceAll_WhenListHasSingleElement_ShouldHandleMatchAndNonMatch()
    {
        var list = new List<int> { 7 };

        Assert.AreEqual(1, list.ReplaceAll(7, 9));
        CollectionAssert.AreEqual(new[] { 9 }, list);

        Assert.AreEqual(0, list.ReplaceAll(7, 9));
        CollectionAssert.AreEqual(new[] { 9 }, list);
    }
}
