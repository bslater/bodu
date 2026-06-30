// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.References.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Adds a freshly allocated value under the specified key and returns a weak reference to it without retaining a
    /// strong reference on the caller's stack.
    /// </summary>
    /// <param name="dictionary">The dictionary to populate.</param>
    /// <param name="key">The key to associate with the new value.</param>
    /// <returns>A weak reference to the stored value.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference AddValueAndGetWeakReference(SequencedDictionary<string, object> dictionary, string key)
    {
        object value = new();
        dictionary[key] = value;
        return new WeakReference(value);
    }

    /// <summary>
    /// Removes and discards the first entry without retaining the returned value on the caller's stack.
    /// </summary>
    /// <param name="dictionary">The dictionary to remove the first entry from.</param>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RemoveFirstAndDiscard(SequencedDictionary<string, object> dictionary) =>
        dictionary.TryRemoveFirst(out _);

    /// <summary>
    /// Verifies that removing an entry releases the dictionary's reference to its value, allowing it to be collected.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void References_WhenEntryRemoved_ShouldReleaseValueReference()
    {
        var dictionary = new SequencedDictionary<string, object>();
        WeakReference weak = AddValueAndGetWeakReference(dictionary, "k");

        dictionary.Remove("k");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(weak.IsAlive);
    }

    /// <summary>
    /// Verifies that clearing the dictionary releases its references to all values, allowing them to be collected.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void References_WhenCleared_ShouldReleaseValueReferences()
    {
        var dictionary = new SequencedDictionary<string, object>();
        WeakReference weak = AddValueAndGetWeakReference(dictionary, "k");

        dictionary.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(weak.IsAlive);
    }

    /// <summary>
    /// Verifies that <see cref="SequencedDictionary{TKey, TValue}.TryRemoveFirst" /> releases the dictionary's reference to the removed value.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void References_WhenFirstRemoved_ShouldReleaseValueReference()
    {
        var dictionary = new SequencedDictionary<string, object>();
        WeakReference weak = AddValueAndGetWeakReference(dictionary, "k");

        RemoveFirstAndDiscard(dictionary);
        Assert.AreEqual(0, dictionary.Count);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.IsFalse(weak.IsAlive);
    }
}
