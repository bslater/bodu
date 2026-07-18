// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryTests.GetEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class EvictingDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.GetEnumerator" /> returns the public
    /// <see cref="EvictingDictionary{TKey, TValue}.Enumerator" /> struct rather than a boxed interface enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCalled_ShouldReturnStructEnumerator()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();

        Assert.IsInstanceOfType<EvictingDictionary<string, int>.Enumerator>(enumerator);
    }

    /// <summary>
    /// Verifies that walking the struct enumerator directly produces the same sequence as a
    /// <see langword="foreach" /> statement for every eviction policy.
    /// </summary>
    [TestMethod]
    [DataRow(EvictingDictionaryPolicy.FirstInFirstOut)]
    [DataRow(EvictingDictionaryPolicy.LeastRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.MostRecentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.LeastFrequentlyUsed)]
    [DataRow(EvictingDictionaryPolicy.SecondChance)]
    [DataRow(EvictingDictionaryPolicy.RandomReplacement)]
    public void GetEnumerator_WhenWalkedDirectly_ShouldMatchForeachSequence(EvictingDictionaryPolicy policy)
    {
        var dictionary = new EvictingDictionary<string, int>(5, policy);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        var direct = new List<KeyValuePair<string, int>>();
        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
            direct.Add(enumerator.Current);

        var viaForeach = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> kvp in dictionary)
            viaForeach.Add(kvp);

        CollectionAssert.AreEqual(viaForeach, direct);
        Assert.HasCount(3, direct);
    }

    /// <summary>
    /// Verifies that the struct enumerator captures the dictionary version at creation, so a mutation before the first
    /// <c>MoveNext</c> call causes it to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryMutatedBeforeFirstMoveNext_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);

        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        dictionary.Add("B", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that mutating the dictionary mid-walk invalidates the struct enumerator and causes the next
    /// <c>MoveNext</c> call to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryMutatedDuringDirectWalk_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);

        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        dictionary.Remove("B");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Enumerator.Reset" /> restarts the walk from the
    /// beginning, replaying the same sequence when the dictionary is unmodified.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenResetAfterPartialWalk_ShouldRestartFromBeginning()
    {
        var dictionary = new EvictingDictionary<string, int>(5, EvictingDictionaryPolicy.FirstInFirstOut);
        dictionary.Add("A", 1);
        dictionary.Add("B", 2);
        dictionary.Add("C", 3);

        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());

        enumerator.Reset();

        var replay = new List<string>();
        while (enumerator.MoveNext())
            replay.Add(enumerator.Current.Key);

        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, replay);
    }

    /// <summary>
    /// Verifies that <see cref="EvictingDictionary{TKey, TValue}.Enumerator.Reset" /> throws
    /// <see cref="InvalidOperationException" /> after the dictionary has been mutated.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenResetAfterMutation_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(5);
        dictionary.Add("A", 1);

        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        dictionary.Add("B", 2);

        Assert.ThrowsExactly<InvalidOperationException>(enumerator.Reset);
    }

    /// <summary>
    /// Verifies that reading <see cref="EvictingDictionary{TKey, TValue}.Enumerator.Current" /> before the first
    /// <c>MoveNext</c> call throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCurrentReadBeforeFirstMoveNext_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="EvictingDictionary{TKey, TValue}.Enumerator.Current" /> after enumeration has
    /// completed throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCurrentReadAfterEnd_ShouldThrowExactly()
    {
        var dictionary = new EvictingDictionary<string, int>(3);
        dictionary.Add("A", 1);

        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
        {
            // Drain the enumerator.
        }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that the struct enumerator filters expired entries against the clock snapshot captured at creation
    /// without removing them from storage.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEntriesExpired_ShouldFilterExpiredEntriesWithoutPurging()
    {
        var time = new ManualTimeProvider();
        var dictionary = new EvictingDictionary<string, int>(
            4, new EvictingDictionaryExpiration(TimeSpan.FromMinutes(10), EvictingDictionaryExpirationKind.Absolute, time));
        dictionary.Add("A", 1, TimeSpan.FromMinutes(2));
        dictionary.Add("B", 2, TimeSpan.FromMinutes(30));

        time.Advance(TimeSpan.FromMinutes(5));

        var seen = new List<string>();
        EvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
            seen.Add(enumerator.Current.Key);

        CollectionAssert.AreEqual(new[] { "B" }, seen);
        Assert.AreEqual(2, dictionary.Count, "Enumeration must not purge expired entries.");
    }
}
