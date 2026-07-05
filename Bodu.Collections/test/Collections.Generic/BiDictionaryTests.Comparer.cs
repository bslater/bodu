// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiDictionaryTests.Comparer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class BiDictionaryTests
{
    /// <summary>
    /// Creates a dictionary using <see cref="StringComparer.OrdinalIgnoreCase" /> on both the key and value sides.
    /// </summary>
    /// <param name="policy">The duplicate-value policy to construct with.</param>
    /// <returns>A case-insensitive <see cref="BiDictionary{TKey, TValue}" />.</returns>
    private static BiDictionary<string, string> CreateCaseInsensitive(
        BiDictionaryDuplicateValuePolicy policy = BiDictionaryDuplicateValuePolicy.Throw) =>
        new(policy, StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Verifies that key lookups honor a case-insensitive key comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenKeyCasingDiffers_ShouldResolveSameKey()
    {
        BiDictionary<string, string> dictionary = CreateCaseInsensitive();
        dictionary.Add("Alpha", "One");

        Assert.IsTrue(dictionary.ContainsKey("ALPHA"));
        Assert.AreEqual("One", dictionary["alpha"]);
    }

    /// <summary>
    /// Verifies that value lookups honor a case-insensitive value comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenValueCasingDiffers_ShouldResolveSameValue()
    {
        BiDictionary<string, string> dictionary = CreateCaseInsensitive();
        dictionary.Add("Alpha", "One");

        Assert.IsTrue(dictionary.ContainsValue("ONE"));
        Assert.IsTrue(dictionary.TryGetKey("one", out string? key));
        Assert.AreEqual("Alpha", key);
    }

    /// <summary>
    /// Verifies that adding a key differing only in casing throws under a case-insensitive key comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenConflictingCasedKeyAdded_ShouldThrowArgumentException()
    {
        BiDictionary<string, string> dictionary = CreateCaseInsensitive();
        dictionary.Add("Alpha", "One");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("ALPHA", "Two");
        });

        Assert.AreEqual("key", ex.ParamName);
    }

    /// <summary>
    /// Verifies that adding a value differing only in casing is a duplicate-value conflict under a case-insensitive
    /// value comparer and the <see cref="BiDictionaryDuplicateValuePolicy.Throw" /> policy.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenConflictingCasedValueAdded_ForThrowPolicy_ShouldThrowArgumentException()
    {
        BiDictionary<string, string> dictionary = CreateCaseInsensitive();
        dictionary.Add("Alpha", "One");

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            dictionary.Add("Beta", "ONE");
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a conflicting-casing value evicts the previous binding under a case-insensitive value comparer
    /// and the <see cref="BiDictionaryDuplicateValuePolicy.Replace" /> policy.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenConflictingCasedValueAdded_ForReplacePolicy_ShouldEvictPreviousBinding()
    {
        BiDictionary<string, string> dictionary = CreateCaseInsensitive(BiDictionaryDuplicateValuePolicy.Replace);
        dictionary.Add("Alpha", "One");

        dictionary.Add("Beta", "ONE");

        Assert.AreEqual(1, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey("Alpha"));
        Assert.AreEqual("ONE", dictionary["Beta"]);
    }

    /// <summary>
    /// Verifies that removing by value honors the case-insensitive value comparer.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenValueRemovedWithDifferentCasing_ShouldRemovePair()
    {
        BiDictionary<string, string> dictionary = CreateCaseInsensitive();
        dictionary.Add("Alpha", "One");

        bool removed = dictionary.RemoveValue("oNe");

        Assert.IsTrue(removed);
        Assert.AreEqual(0, dictionary.Count);
    }
}
