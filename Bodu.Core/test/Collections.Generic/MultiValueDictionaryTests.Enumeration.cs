// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Enumeration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that accessing <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Current"/> after the
    /// enumerator is exhausted throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedAfterExhaustion_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        MultiValueDictionary<string, int>.Enumerator en = mvd.GetEnumerator();
        while (en.MoveNext()) { }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    /// <summary>
    /// Verifies that accessing <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Current"/> before the first
    /// call to <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.MoveNext"/> throws
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenCurrentAccessedBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        MultiValueDictionary<string, int>.Enumerator en = mvd.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    /// <summary>
    /// Verifies that enumerating the dictionary yields one key–value-list pair per distinct key.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEnumerated_ShouldYieldOneEntryPerKey()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("b", 3);

        var entries = mvd.ToList();

        Assert.HasCount(2, entries);
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the dictionary is modified
    /// during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in mvd)
                mvd.Add("b", 2);
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the dictionary is cleared
    /// during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedViaClear_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in mvd)
                mvd.Clear();
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when a single value is removed
    /// from an existing key during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedViaRemove_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in mvd)
                mvd.Remove("a", 1);
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the dictionary is modified
    /// via <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedViaRemoveAll_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in mvd)
                mvd.RemoveAll("a");
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Reset"/> repositions the enumerator
    /// to before the first element, allowing the dictionary to be enumerated again from the beginning.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenReset_ShouldAllowReEnumeration()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        MultiValueDictionary<string, int>.Enumerator en = mvd.GetEnumerator();
        var firstPass = new List<string>();
        while (en.MoveNext())
            firstPass.Add(en.Current.Key);

        en.Reset();

        var secondPass = new List<string>();
        while (en.MoveNext())
            secondPass.Add(en.Current.Key);

        firstPass.Sort();
        secondPass.Sort();
        CollectionAssert.AreEqual(firstPass, secondPass);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Reset"/> throws
    /// <see cref="InvalidOperationException"/> when the dictionary was modified after the enumerator was created.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetAfterModification_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        MultiValueDictionary<string, int>.Enumerator en = mvd.GetEnumerator();
        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            en.Reset();
        });
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> produces one pair per value entry.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenCalled_ShouldYieldOnePairPerValueEntry()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);
        mvd.Add("b", 3);

        var flat = mvd.Flatten().ToList();

        Assert.HasCount(3, flat);

        var sorted = flat.OrderBy(p => p.Key).ThenBy(p => p.Value).ToList();
        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), sorted[0]);
        Assert.AreEqual(new KeyValuePair<string, int>("a", 2), sorted[1]);
        Assert.AreEqual(new KeyValuePair<string, int>("b", 3), sorted[2]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.Flatten" /> throws
    /// <see cref="InvalidOperationException" /> when the dictionary is cleared after enumeration has begun.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenClearedAfterEnumerationBegins_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        using IEnumerator<KeyValuePair<string, int>> enumerator = mvd.Flatten().GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext(), "Expected Flatten to yield at least one item before mutation.");

        mvd.Clear();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = enumerator.MoveNext();
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> returns no pairs when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenEmpty_ShouldReturnNoPairs()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.IsEmpty(mvd.Flatten());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> throws <see cref="InvalidOperationException"/>
    /// when the dictionary is modified during enumeration.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in mvd.Flatten())
                mvd.Add("c", 3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> throws <see cref="InvalidOperationException"/>
    /// when a value is removed during enumeration.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenModifiedViaRemove_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in mvd.Flatten())
                mvd.Remove("a", 1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> throws <see cref="InvalidOperationException"/>
    /// when the dictionary is modified via <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenModifiedViaRemoveAll_ShouldThrowInvalidOperationException()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in mvd.Flatten())
                mvd.RemoveAll("a");
        });
    }

    /// <summary>
    /// Verifies that read-only operations do not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void ReadOperations_WhenCalledDuringEnumeration_ShouldNotInvalidateActiveEnumerator()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        _ = mvd.Count;
        _ = mvd.KeyCount;
        _ = mvd.Keys.Count;
        _ = mvd.ContainsKey("a");
        _ = mvd.ContainsValue("a", 1);
        _ = mvd.TryGetValues("a", out _);
        _ = mvd["a"];

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
        Assert.IsFalse(enumerator.MoveNext());
    }

}
