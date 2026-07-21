// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.Enumerator.VersionCheck.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{

    /// <summary>
    /// Verifies that mutating an <see cref="EvictingDictionary{TKey, TValue}" /> after starting enumeration causes the next
    /// <c>MoveNext</c> call to throw <see cref="InvalidOperationException" />, exercising the internal
    /// <c>ThrowIfVersionChanged</c> guard.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDictionaryIsMutatedDuringEnumeration_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        using IEnumerator<KeyValuePair<string, int>> enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        // Mutate the dictionary while the enumerator is still in flight.
        dictionary.Add("D", 4);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that a read that repositions the current entry under MostRecentlyUsed fails the in-flight enumeration
    /// fast with <see cref="InvalidOperationException" /> rather than silently truncating the manually walked order.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEntryTouchedDuringEnumerationUnderMRU_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.MostRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var seen = new List<string>();
            foreach (KeyValuePair<string, int> kvp in dictionary)
            {
                seen.Add(kvp.Key);
                _ = dictionary[kvp.Key]; // repositions the current node in the order list
            }

            // Reaching here means the walk terminated silently instead of failing fast.
            Assert.HasCount(3, seen, "Enumeration truncated silently instead of throwing.");
        });
    }

    /// <summary>
    /// Verifies that a read that repositions an entry under LeastRecentlyUsed fails the in-flight enumeration fast
    /// with <see cref="InvalidOperationException" />, consistently with the MostRecentlyUsed policy.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEntryTouchedDuringEnumerationUnderLRU_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.LeastRecentlyUsed);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> kvp in dictionary)
                _ = dictionary[kvp.Key];
        });
    }

    /// <summary>
    /// Verifies that a read under SecondChance, which only flips the entry's reference flag without repositioning it,
    /// leaves in-flight enumeration valid.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEntryTouchedDuringEnumerationUnderSecondChance_ShouldNotThrow()
    {
        var dictionary = new EvictingDictionary<string, int>(3, EvictingDictionaryPolicy.SecondChance);
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);
        dictionary.Add("c", 3);

        var seen = new List<string>();
        foreach (KeyValuePair<string, int> kvp in dictionary)
        {
            seen.Add(kvp.Key);
            _ = dictionary[kvp.Key];
        }

        Assert.HasCount(3, seen);
    }

}
