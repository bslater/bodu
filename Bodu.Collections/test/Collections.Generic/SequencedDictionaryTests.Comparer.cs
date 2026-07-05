// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that a custom comparer treats keys differing only by case as the same key for lookups.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenOrdinalIgnoreCase_ShouldTreatKeysAsEqualForLookup()
    {
        var dictionary = new SequencedDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Key"] = 1,
        };

        Assert.IsTrue(dictionary.ContainsKey("KEY"));
        Assert.AreEqual(1, dictionary["key"]);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Add(TKey, TValue)" /> detects a duplicate key under the custom comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenOrdinalIgnoreCaseAndCaseVariantAdded_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Key"] = 1,
        };

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("KEY", 2);
        });
    }

    /// <summary>
    /// Verifies that the indexer updates an existing entry in place when a case-variant key resolves under the custom comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenOrdinalIgnoreCaseAndCaseVariantAssigned_ShouldUpdateInPlace()
    {
        var dictionary = new SequencedDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Key"] = 1,
            ["Other"] = 2,
        };

        dictionary["KEY"] = 99;

        Assert.AreEqual(99, dictionary["key"]);
        Assert.AreEqual(2, dictionary.Count);
        CollectionAssert.AreEqual(new[] { "Key", "Other" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Remove(TKey)" /> resolves a case-variant key under the custom comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenOrdinalIgnoreCaseAndCaseVariantRemoved_ShouldRemoveEntry()
    {
        var dictionary = new SequencedDictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Key"] = 1,
        };

        bool removed = dictionary.Remove("KEY");

        Assert.IsTrue(removed);
        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Comparer" /> exposes the comparer supplied at construction.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCustomComparerProvided_ShouldBeExposed()
    {
        var dictionary = new SequencedDictionary<string, int>(StringComparer.Ordinal);

        Assert.AreSame(StringComparer.Ordinal, dictionary.Comparer);
    }
}
