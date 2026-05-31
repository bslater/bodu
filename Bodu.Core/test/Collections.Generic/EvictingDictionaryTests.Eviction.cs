// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Eviction.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Add(TKey, TValue)" /> evicts the expected candidate before
    /// inserting the new entry when the dictionary is at capacity. Validates that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" />
    /// matches the actually-evicted key, so the peek is consistent with the evict.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut, "A")]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed, "A")]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed, "B")]
    public void Add_WhenCapacityReached_ShouldEvictExpectedCandidateBeforeAddingNewItem(EvictingDictionaryPolicy policy, string expectedEvicted)
    {
        var dictionary = new EvictingDictionary<string, int>(2, policy);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        var predicted = dictionary.PeekEvictionCandidate();
        Assert.AreEqual(expectedEvicted, predicted);

        dictionary.Add("C", 3);

        Assert.AreEqual(2, dictionary.Count);
        Assert.IsFalse(dictionary.ContainsKey(expectedEvicted));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that the Second-Chance algorithm requeues an item once before eviction. After the first capacity-exceed Add the
    /// touched item gets a second chance, so a subsequent Add evicts the untouched item.
    /// </summary>
    [TestMethod]
    public void EvictOne_WhenSecondChanceCandidateExists_ShouldSkipAndRequeueCandidate()
    {
        var dictionary = new EvictingDictionary<string, int>(2, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Touch("A"); // A gets second chance

        dictionary.Add("C", 3); // B should be evicted because A had second chance

        Assert.IsTrue(dictionary.ContainsKey("A"));
        Assert.IsFalse(dictionary.ContainsKey("B"));
        Assert.IsTrue(dictionary.ContainsKey("C"));
    }

    /// <summary>
    /// Verifies that enumeration order under the <see cref="EvictingDictionaryPolicy.FirstInFirstOut" /> policy matches insertion
    /// order — the eviction-priority order for FIFO.
    /// </summary>
    [TestMethod]
    public void GetOrderedItems_WhenPolicyIsFifo_ShouldReturnItemsInInsertionOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("first", 1);
        dictionary.Add("second", 2);
        dictionary.Add("third", 3);

        var keys = dictionary.Select(kvp => kvp.Key).ToArray();

        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, keys);
    }

    /// <summary>
    /// Verifies that enumeration order under the <see cref="EvictingDictionaryPolicy.LeastFrequentlyUsed" /> policy returns items
    /// grouped by ascending frequency.
    /// </summary>
    [TestMethod]
    public void GetOrderedItems_WhenPolicyIsLfu_ShouldReturnItemsInFrequencyOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.LeastFrequentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);
        dictionary.Touch("B"); // B reaches frequency 2
        dictionary.Touch("C"); // C reaches frequency 2

        IList<string> keys = dictionary.Select(kvp => kvp.Key).ToList();

        // A is at the lowest frequency; B and C share the next.
        Assert.AreEqual("A", keys[0]);
        CollectionAssert.AreEquivalent(new[] { "B", "C" }, keys.Skip(1).ToArray());
    }

    /// <summary>
    /// Verifies that enumeration order under the <see cref="EvictingDictionaryPolicy.LeastRecentlyUsed" /> policy reflects access
    /// order — least-recently used first.
    /// </summary>
    [TestMethod]
    public void GetOrderedItems_WhenPolicyIsLru_ShouldReturnItemsInAccessOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);
        dictionary.Touch("A"); // A becomes most recent

        var keys = dictionary.Select(kvp => kvp.Key).ToArray();

        CollectionAssert.AreEqual(new[] { "B", "C", "A" }, keys);
    }

    /// <summary>
    /// Verifies that enumeration order under the <see cref="EvictingDictionaryPolicy.MostRecentlyUsed" /> policy walks the order
    /// list in reverse — most recently used first.
    /// </summary>
    [TestMethod]
    public void GetOrderedItems_WhenPolicyIsMru_ShouldReturnItemsInReverseAccessOrder()
    {
        var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.MostRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        var keys = dictionary.Select(kvp => kvp.Key).ToArray();

        CollectionAssert.AreEqual(new[] { "C", "B", "A" }, keys);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> does not mutate dictionary state when
    /// repeatedly called — the count, eviction count, total touches, and key ordering remain identical before and after.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenCalled_ShouldNotMutateDictionaryState()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        var beforeKeys = dictionary.Keys.ToArray();
        var beforeTouches = dictionary.TotalTouches;
        var beforeEvictions = dictionary.EvictionCount;

        var candidate1 = dictionary.PeekEvictionCandidate();
        var candidate2 = dictionary.PeekEvictionCandidate();

        Assert.AreEqual(candidate1, candidate2);
        CollectionAssert.AreEqual(beforeKeys, dictionary.Keys.ToArray());
        Assert.AreEqual(beforeTouches, dictionary.TotalTouches);
        Assert.AreEqual(beforeEvictions, dictionary.EvictionCount);
        Assert.AreEqual(3, dictionary.Count);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> on an empty Least-Frequently-Used dictionary
    /// returns <see langword="null" />, exercising the LFU switch arm's <c>_frequencyList?.Count &gt; 0</c> guard's false branch.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenLeastFrequentlyUsedAndDictionaryIsEmpty_ShouldReturnNull()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastFrequentlyUsed);

        Assert.IsNull(dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> on an empty Most-Recently-Used dictionary
    /// returns <see langword="null" />, exercising the MRU switch arm's <c>_order?.Last is not null</c> guard's false branch.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenMostRecentlyUsedAndDictionaryIsEmpty_ShouldReturnNull()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);

        Assert.IsNull(dictionary.PeekEvictionCandidate());
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> on an empty Random-Replacement dictionary
    /// returns <see langword="null" />, exercising the random switch arm's <c>_store.Count &gt; 0</c> guard's false branch.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenRandomReplacementAndDictionaryIsEmpty_ShouldReturnNull()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.RandomReplacement);

        Assert.IsNull(dictionary.PeekEvictionCandidate());
    }
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.PeekEvictionCandidate" /> on an empty Second-Chance dictionary
    /// returns <see langword="null" />, exercising the <c>PeekSecondChanceCandidate</c> fallback path for an empty order list.
    /// </summary>
    [TestMethod]
    public void PeekEvictionCandidate_WhenSecondChanceAndDictionaryIsEmpty_ShouldReturnNull()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);

        Assert.IsNull(dictionary.PeekEvictionCandidate());
    }

}
