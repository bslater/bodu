// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    /// <summary>
    /// Verifies that the generic <see cref="IEnumerable{T}" /> implementation enumerates one row per key.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEnumeratedViaInterface_ShouldYieldOneEntryPerKey()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("b", 3);

        IEnumerable<KeyValuePair<string, IReadOnlyList<int>>> enumerable = mvd;

        List<KeyValuePair<string, IReadOnlyList<int>>> entries = enumerable.ToList();

        Assert.AreEqual(2, entries.Count);
        Assert.IsTrue(entries.Any(entry => entry.Key == "a" && entry.Value.SequenceEqual([1, 2])));
        Assert.IsTrue(entries.Any(entry => entry.Key == "b" && entry.Value.SequenceEqual([3])));
    }

    /// <summary>
    /// Verifies that the generic interface enumerator throws when accessed before it is positioned on an item.
    /// </summary>
    [TestMethod]
    public void CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IEnumerable<KeyValuePair<string, IReadOnlyList<int>>> enumerable = mvd;
        using IEnumerator<KeyValuePair<string, IReadOnlyList<int>>> enumerator = enumerable.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that the generic interface enumerator throws when accessed after enumeration is exhausted.
    /// </summary>
    [TestMethod]
    public void CurrentAfterExhaustion_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IEnumerable<KeyValuePair<string, IReadOnlyList<int>>> enumerable = mvd;
        using IEnumerator<KeyValuePair<string, IReadOnlyList<int>>> enumerator = enumerable.GetEnumerator();

        while (enumerator.MoveNext())
        {
        }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that the generic interface enumerator detects structural modification.
    /// </summary>
    [TestMethod]
    public void MoveNext_WhenDictionaryModified_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IEnumerable<KeyValuePair<string, IReadOnlyList<int>>> enumerable = mvd;
        using IEnumerator<KeyValuePair<string, IReadOnlyList<int>>> enumerator = enumerable.GetEnumerator();

        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that the generic interface enumerator's current value does not expose the mutable backing list.
    /// </summary>
    [TestMethod]
    public void CurrentValue_ShouldNotExposeMutableBackingList()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IEnumerable<KeyValuePair<string, IReadOnlyList<int>>> enumerable = mvd;
        using IEnumerator<KeyValuePair<string, IReadOnlyList<int>>> enumerator = enumerable.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());

        AssertReadOnlyValueViewCannotBeMutatedForInterfaceTests(enumerator.Current.Value);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IEnumerable" /> implementation enumerates boxed key-value-list entries.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenEnumeratedViaInterface_ShouldYieldBoxedEntries()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IEnumerable enumerable = mvd;
        IEnumerator enumerator = enumerable.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());

        var entry = (KeyValuePair<string, IReadOnlyList<int>>)enumerator.Current;

        Assert.AreEqual("a", entry.Key);
        CollectionAssert.AreEqual(new[] { 1 }, entry.Value.ToList());
    }

    /// <summary>
    /// Verifies that the non-generic interface enumerator throws when accessed before it is positioned on an item.
    /// </summary>
    [TestMethod]
    public void CurrentBeforeMoveNext_ShouldThrowInvalidOperationException2()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IEnumerator enumerator = ((IEnumerable)mvd).GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.Current;
        });
    }

    /// <summary>
    /// Verifies that the non-generic interface enumerator supports reset before mutation.
    /// </summary>
    [TestMethod]
    public void Reset_WhenNotModified_ShouldAllowReEnumeration()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        IEnumerator enumerator = ((IEnumerable)mvd).GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());

        enumerator.Reset();

        var keys = new List<string>();
        while (enumerator.MoveNext())
        {
            var entry = (KeyValuePair<string, IReadOnlyList<int>>)enumerator.Current;
            keys.Add(entry.Key);
        }

        keys.Sort();
        CollectionAssert.AreEqual(new[] { "a", "b" }, keys);
    }

    /// <summary>
    /// Verifies that the non-generic interface enumerator detects structural modification during reset.
    /// </summary>
    [TestMethod]
    public void Reset_WhenDictionaryModified_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IEnumerator enumerator = ((IEnumerable)mvd).GetEnumerator();

        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            enumerator.Reset();
        });
    }

    private static void AssertReadOnlyValueViewCannotBeMutatedForInterfaceTests(IReadOnlyList<int> values)
    {
        Assert.IsFalse(values is List<int>, "The returned value view must not be the mutable backing List<T>.");

        if (values is ICollection<int> collection)
        {
            Assert.IsTrue(
                collection.IsReadOnly,
                "The returned value view must not expose a mutable ICollection<T>.");

            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                collection.Add(999);
            });
        }
    }
}
