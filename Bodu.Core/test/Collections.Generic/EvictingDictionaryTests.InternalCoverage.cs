// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.InternalCoverage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" /> evicts the oldest entry
    /// under <see cref="EvictingDictionaryPolicy.FirstInFirstOut" /> when capacity is exceeded, exercising the
    /// FIFO arm of the eviction switch.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsFifoAndCapacityExceeded_ShouldEvictOldestEntry()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        Assert.IsFalse(dictionary.ContainsKey("A"));
        Assert.IsTrue(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" /> updates an existing
    /// entry's value in place under the <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" /> policy
    /// without changing its frequency bucket prematurely, exercising the in-place update branch and the
    /// frequency-tracking <c>TouchInternal</c> path.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsLfuAndKeyExists_ShouldUpdateValueAndIncrementFrequency()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("k", 1);

        dictionary.Add("k", 99);

        Assert.AreEqual(99, dictionary["k"]);
        Assert.AreEqual(1, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" /> evicts the most recently
    /// used entry under <see cref="EvictingDictionaryPolicy.MostRecentlyUsed" /> when capacity is exceeded,
    /// exercising the MRU arm of the eviction switch.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsMruAndCapacityExceeded_ShouldEvictMostRecentEntry()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.MostRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3); // evicts "B" (most recently inserted)

        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" /> evicts a random entry
    /// under <see cref="EvictingDictionaryPolicy.RandomReplacement" /> when capacity is exceeded, exercising
    /// the random arm of the eviction switch.
    /// </summary>
    [TestMethod]
    public void Add_WhenPolicyIsRandomAndCapacityExceeded_ShouldEvictExactlyOneEntry()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.RandomReplacement);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        dictionary.Add("C", 3);

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.DictionaryEnumerator" /> rejects a
    /// <see langword="null" /> dictionary, exercising the constructor's <c>?? throw</c> branch.
    /// </summary>
    [TestMethod]
    public void DictionaryEnumeratorCtor_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new EvictingDictionary<string, int>.DictionaryEnumerator(null!);
        });
    }

    /// <summary>
    /// Verifies that mutating the dictionary mid-enumeration triggers <c>ThrowIfVersionChanged</c> for every
    /// non-default eviction policy, exercising both the throw branch and each per-policy <c>GetOrderedItems</c>
    /// loop.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    public void Enumerator_WhenDictionaryMutatedDuringEnumeration_ShouldThrowExactly(EvictingDictionaryPolicy policy)
    {
        var dictionary = new EvictingDictionary<string, int>(5, policy);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        using IEnumerator<KeyValuePair<string, int>> enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        dictionary.Add("D", 4);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that enumeration yields each entry under every supported eviction policy, exercising every
    /// per-policy code path inside the <c>GetOrderedItems</c> iterator.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    public void Enumerator_WhenIteratingFullDictionary_ShouldYieldEveryEntryForAllPolicies(EvictingDictionaryPolicy policy)
    {
        var dictionary = new EvictingDictionary<string, int>(5, policy);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        var keys = dictionary.Select(kvp => kvp.Key).OrderBy(k => k).ToArray();

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, keys);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns the lowest
    /// frequency bucket's key for the <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" /> policy.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsLeastFrequentlyUsed_ShouldReturnLowestFrequencyKey()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        // Touch a and b so c remains at the lowest frequency.
        dictionary.Touch("a");
        dictionary.Touch("b");

        Assert.AreEqual("c", dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns the most
    /// recently inserted key for the <see cref="EvictingDictionaryPolicy.MostRecentlyUsed" /> policy, exercising
    /// the MRU arm of the candidate switch.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsMostRecentlyUsed_ShouldReturnMostRecent()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        Assert.AreEqual("c", dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> returns an arbitrary
    /// existing key under the <see cref="EvictingDictionaryPolicy.RandomReplacement" /> policy.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenPolicyIsRandomReplacement_ShouldReturnAnyContainedKey()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.RandomReplacement);
        dictionary.Add("alpha", 1);
        dictionary.Add("beta", 2);

        var candidate = dictionary.PeekEvictionCandidate();

        Assert.IsNotNull(candidate);
        Assert.IsTrue(dictionary.ContainsKey(candidate!));
    }

}
