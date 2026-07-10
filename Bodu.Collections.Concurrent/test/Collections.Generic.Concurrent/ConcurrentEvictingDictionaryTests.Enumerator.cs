// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentEvictingDictionaryTests.Enumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentEvictingDictionaryTests
{
    /// <summary>
    /// Verifies that the struct enumerator walks every snapshot entry and then reports completion.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenAdvanced_ShouldYieldEveryEntryThenComplete()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary.Add("b", 2);

        ConcurrentEvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        var seen = new List<string>();
        while (enumerator.MoveNext())
            seen.Add(enumerator.Current.Key);

        Assert.HasCount(2, seen);
        Assert.IsFalse(enumerator.MoveNext(), "MoveNext must keep returning false after completion.");
    }

    /// <summary>
    /// Verifies that a default-constructed enumerator behaves as an empty sequence instead of throwing.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDefaultValued_ShouldBehaveAsEmptySequence()
    {
        ConcurrentEvictingDictionary<string, int>.Enumerator enumerator = default;

        Assert.IsFalse(enumerator.MoveNext());
        Assert.AreEqual(default, enumerator.Current);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentEvictingDictionary{TKey, TValue}.Enumerator.Reset" /> rewinds the enumerator
    /// to before the first snapshot entry.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenReset_ShouldRewindToStart()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);

        ConcurrentEvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());

        enumerator.Reset();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
    }

    /// <summary>
    /// Verifies that disposing the enumerator is a no-op and leaves it usable.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenDisposed_ShouldBeNoOp()
    {
        var dictionary = new ConcurrentEvictingDictionary<string, int>();
        dictionary.Add("a", 1);

        ConcurrentEvictingDictionary<string, int>.Enumerator enumerator = dictionary.GetEnumerator();
        enumerator.Dispose();

        Assert.IsTrue(enumerator.MoveNext());
    }
}
