// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.GetEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.GetEnumerator" /> returns the public
    /// <see cref="SequencedDictionary{TKey, TValue}.Enumerator" /> struct rather than a boxed interface enumerator.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCalled_ShouldReturnStructEnumerator()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();

        Assert.IsInstanceOfType<SequencedDictionary<string, int>.Enumerator>(enumerator);
    }

    /// <summary>
    /// Verifies that walking the struct enumerator directly produces the same insertion-ordered sequence as a
    /// <see langword="foreach" /> statement.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenWalkedDirectly_ShouldMatchForeachSequence()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        var direct = new List<KeyValuePair<string, int>>();
        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
            direct.Add(enumerator.Current);

        var viaForeach = new List<KeyValuePair<string, int>>();
        foreach (KeyValuePair<string, int> kvp in dictionary)
            viaForeach.Add(kvp);

        CollectionAssert.AreEqual(viaForeach, direct);
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, direct.Select(kvp => kvp.Key).ToArray());
    }

    /// <summary>
    /// Verifies that the struct enumerator captures the dictionary version at creation, so a mutation before the first
    /// <c>MoveNext</c> call causes it to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenDictionaryMutatedBeforeFirstMoveNext_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        dictionary.Add("d", 4);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that an access-order read that repositions an entry mid-walk invalidates the struct enumerator and
    /// causes the next <c>MoveNext</c> call to throw <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenAccessOrderReadDuringDirectWalk_ShouldThrowExactly()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true)
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3,
        };

        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        _ = dictionary[enumerator.Current.Key];

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Enumerator.Reset" /> restarts the walk from the
    /// beginning, replaying the same sequence when the dictionary is unmodified.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenResetAfterPartialWalk_ShouldRestartFromBeginning()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());

        enumerator.Reset();

        var replay = new List<string>();
        while (enumerator.MoveNext())
            replay.Add(enumerator.Current.Key);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, replay);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.Enumerator.Reset" /> throws
    /// <see cref="InvalidOperationException" /> after the dictionary has been mutated.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenResetAfterMutation_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        dictionary.Remove("a");

        Assert.ThrowsExactly<InvalidOperationException>(enumerator.Reset);
    }

    /// <summary>
    /// Verifies that reading <see cref="SequencedDictionary{TKey, TValue}.Enumerator.Current" /> before the first
    /// <c>MoveNext</c> call throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCurrentReadBeforeFirstMoveNext_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that reading <see cref="SequencedDictionary{TKey, TValue}.Enumerator.Current" /> after enumeration has
    /// completed throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCurrentReadAfterEnd_ShouldThrowExactly()
    {
        SequencedDictionary<string, int> dictionary = CreatePopulated();

        SequencedDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        while (enumerator.MoveNext())
        {
            // Drain the enumerator.
        }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }
}
