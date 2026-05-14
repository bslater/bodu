// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DebugViewTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public sealed class DebugViewTests
{
    /// <summary>
    /// Verifies that <see cref="EvictingDictionaryDebugView{TKey, TValue}.Items" /> returns the underlying dictionary's key-value pairs
    /// in current eviction order.
    /// </summary>
    [TestMethod]
    public void EvictingDictionaryDebugView_Items_ShouldReturnDictionaryEntries()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        var view = new EvictingDictionaryDebugView<string, int>(dictionary);
        KeyValuePair<string, int>[] items = view.Items;

        Assert.AreEqual(3, items.Length);
        CollectionAssert.AreEquivalent(
            new[] { new KeyValuePair<string, int>("A", 1), new KeyValuePair<string, int>("B", 2), new KeyValuePair<string, int>("C", 3) },
            items);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionaryDebugView{TKey, TValue}" /> throws <see cref="ArgumentNullException" /> when the
    /// dictionary is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EvictingDictionaryDebugView_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new EvictingDictionaryDebugView<string, int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionaryDebugView{TKey, TValue}.Entries" /> returns the underlying dictionary's key/value-list
    /// pairs.
    /// </summary>
    [TestMethod]
    public void MultiValueDictionaryDebugView_Items_ShouldReturnDictionaryItems()
    {
        var dictionary = new MultiValueDictionary<string, int>();
        dictionary.Add("A", 1);
        dictionary.Add("A", 2);
        dictionary.Add("B", 3);

        var view = new MultiValueDictionaryDebugView<string, int>(dictionary);
        KeyValuePair<string, IReadOnlyList<int>>[] items = view.Items;

        Assert.AreEqual(2, items.Length);

        Dictionary<string, IReadOnlyList<int>> byKey = items.ToDictionary(e => e.Key, e => e.Value);
        CollectionAssert.AreEquivalent(new[] { 1, 2 }, byKey["A"].ToArray());
        CollectionAssert.AreEqual(new[] { 3 }, byKey["B"].ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionaryDebugView{TKey, TValue}" /> throws <see cref="ArgumentNullException" /> when the
    /// dictionary is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void MultiValueDictionaryDebugView_WhenDictionaryIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new MultiValueDictionaryDebugView<string, int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultisetDebugView{T}.Frequencies" /> returns each distinct element alongside its multiplicity.
    /// </summary>
    [TestMethod]
    public void MultisetDebugView_Frequencies_ShouldReturnElementCounts()
    {
        var multiset = new Multiset<string>(["A", "B", "A", "C", "B", "A"]);

        var view = new MultisetDebugView<string>(multiset);
        KeyValuePair<string, int>[] frequencies = view.Frequencies;

        Assert.AreEqual(3, frequencies.Length);
        Dictionary<string, int> asDict = frequencies.ToDictionary(p => p.Key, p => p.Value);
        Assert.AreEqual(3, asDict["A"]);
        Assert.AreEqual(2, asDict["B"]);
        Assert.AreEqual(1, asDict["C"]);
    }

    /// <summary>
    /// Verifies that <see cref="MultisetDebugView{T}" /> throws <see cref="ArgumentNullException" /> when the multiset is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void MultisetDebugView_WhenMultisetIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new MultisetDebugView<string>(null!);
        });
    }
}
