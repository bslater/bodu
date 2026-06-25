// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.AccessOrder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Creates a populated access-order dictionary with keys <c>"a"</c>, <c>"b"</c>, <c>"c"</c>.
    /// </summary>
    /// <returns>A populated access-order <see cref="SequencedDictionary{TKey, TValue}" />.</returns>
    private static SequencedDictionary<string, int> CreateAccessOrdered() =>
        new(accessOrder: true) { ["a"] = 1, ["b"] = 2, ["c"] = 3 };

    /// <summary>
    /// Verifies that, in access-order mode, <see cref="SequencedDictionary{TKey, TValue}.TryGetValue" /> moves the entry to the tail.
    /// </summary>
    [TestMethod]
    public void AccessOrder_WhenTryGetValue_ShouldMoveEntryToEnd()
    {
        var dictionary = CreateAccessOrdered();

        _ = dictionary.TryGetValue("a", out _);

        CollectionAssert.AreEqual(new[] { "b", "c", "a" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, reading the only entry leaves the order unchanged.
    /// </summary>
    [TestMethod]
    public void AccessOrder_WhenSingleEntryRead_ShouldLeaveOrderUnchanged()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true) { ["a"] = 1 };

        _ = dictionary["a"];

        CollectionAssert.AreEqual(new[] { "a" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, <see cref="SequencedDictionary{TKey, TValue}.TryGetFirst" /> and
    /// <see cref="SequencedDictionary{TKey, TValue}.TryGetLast" /> do not reorder entries.
    /// </summary>
    [TestMethod]
    public void AccessOrder_WhenPeekingEnds_ShouldNotReorder()
    {
        var dictionary = CreateAccessOrdered();

        _ = dictionary.TryGetFirst(out _);
        _ = dictionary.TryGetLast(out _);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, reading the entry already at the tail does not invalidate an in-progress enumeration.
    /// </summary>
    [TestMethod]
    public void AccessOrder_WhenTailReadDuringEnumeration_ShouldNotInvalidateEnumerator()
    {
        var dictionary = CreateAccessOrdered();

        var seen = new List<string>();
        foreach (KeyValuePair<string, int> kvp in dictionary)
        {
            seen.Add(kvp.Key);
            _ = dictionary["c"]; // "c" is already the tail — a no-op reorder.
        }

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, seen);
    }

    /// <summary>
    /// Verifies that enumerating an access-order dictionary is not treated as an access and does not reorder entries.
    /// </summary>
    /// <remarks>
    /// Mirrors the OpenJDK <c>LinkedHashMap</c> contract that iteration over an access-ordered map does not promote the
    /// visited entries.
    /// </remarks>
    [TestMethod]
    public void AccessOrder_WhenEnumerated_ShouldNotReorderEntries()
    {
        var dictionary = CreateAccessOrdered();

        foreach (KeyValuePair<string, int> _ in dictionary)
        {
            // Full enumeration must not promote any entry.
        }

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, the values view's <see cref="ICollection{T}.Contains" /> does not reorder entries.
    /// </summary>
    [TestMethod]
    public void AccessOrder_WhenValuesContains_ShouldNotReorder()
    {
        var dictionary = CreateAccessOrdered();

        _ = dictionary.Values.Contains(1);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, in access-order mode, <see cref="SequencedDictionary{TKey, TValue}.Contains" /> over a key/value pair does not reorder entries.
    /// </summary>
    [TestMethod]
    public void AccessOrder_WhenContainsPair_ShouldNotReorder()
    {
        var dictionary = CreateAccessOrdered();

        _ = dictionary.Contains(new KeyValuePair<string, int>("a", 1));

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }
}
