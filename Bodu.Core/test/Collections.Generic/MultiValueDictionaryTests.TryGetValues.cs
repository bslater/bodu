// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.TryGetValues.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.TryGetValues"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd.TryGetValues(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.TryGetValues"/> returns <see langword="false"/> and an empty list when the key is absent.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenKeyAbsent_ShouldReturnFalseAndEmptyList()
    {
        var mvd = new MultiValueDictionary<string, int>();

        var found = mvd.TryGetValues("missing", out IReadOnlyList<int> values);

        Assert.IsFalse(found);
        Assert.IsNotNull(values);
        Assert.AreEqual(0, values.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.TryGetValues"/> returns <see langword="true"/> and the correct values when the key is present.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenKeyPresent_ShouldReturnTrueAndValues()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 5);
        mvd.Add("k", 10);

        var found = mvd.TryGetValues("k", out IReadOnlyList<int> values);

        Assert.IsTrue(found);
        Assert.AreEqual(2, values.Count);
    }

    /// <summary>
    /// Verifies that the list returned by <see cref="MultiValueDictionary{TKey,TValue}.TryGetValues"/> reflects values added to the same key after the call.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenReturned_ShouldReflectSubsequentAdditions()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 10);

        var found = mvd.TryGetValues("k", out IReadOnlyList<int> view);
        Assert.IsTrue(found);

        mvd.Add("k", 20);

        Assert.AreEqual(2, view.Count);
        Assert.AreEqual(20, view[1]);
    }

    /// <summary>
    /// Verifies that a value view becomes empty when the dictionary is cleared.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenClearAfterViewReturned_ShouldBecomeEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);

        Assert.IsTrue(mvd.TryGetValues("a", out IReadOnlyList<int> values));

        mvd.Clear();

        Assert.AreEqual(0, values.Count);
        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.KeyCount);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.TryGetValues" /> uses the configured key comparer.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenCustomComparerUsed_ShouldResolveEquivalentKey()
    {
        var mvd = new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        mvd.Add("Alpha", 1);

        var found = mvd.TryGetValues("alpha", out IReadOnlyList<int> values);

        Assert.IsTrue(found);
        Assert.AreEqual(1, values.Count);
        Assert.AreEqual(1, values[0]);
    }
}
