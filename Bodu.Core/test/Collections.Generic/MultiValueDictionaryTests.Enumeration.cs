// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Enumeration.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    // --------------------------------------------------------
    // Flatten()
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> produces one pair per value entry.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenCalled_ShouldYieldOnePairPerValueEntry()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("a", 2);
        sut.Add("b", 3);

        List<KeyValuePair<string, int>> flat = sut.Flatten().ToList();

        Assert.AreEqual(3, flat.Count);

        List<KeyValuePair<string, int>> sorted = flat.OrderBy(p => p.Key).ThenBy(p => p.Value).ToList();
        Assert.AreEqual(new KeyValuePair<string, int>("a", 1), sorted[0]);
        Assert.AreEqual(new KeyValuePair<string, int>("a", 2), sorted[1]);
        Assert.AreEqual(new KeyValuePair<string, int>("b", 3), sorted[2]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> returns no pairs when the dictionary is empty.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenEmpty_ShouldReturnNoPairs()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();

        Assert.AreEqual(0, sut.Flatten().Count());
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> throws <see cref="InvalidOperationException"/> when modified during enumeration.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in sut.Flatten())
                sut.Add("c", 3);
        });
    }

    // --------------------------------------------------------
    // GetEnumerator / foreach
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that enumerating the dictionary yields one key–value-list pair per distinct key.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenEnumerated_ShouldYieldOneEntryPerKey()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("a", 2);
        sut.Add("b", 3);

        List<KeyValuePair<string, IReadOnlyList<int>>> entries = sut.ToList();

        Assert.AreEqual(2, entries.Count);
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the dictionary is modified during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedDuringEnumeration_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in sut)
                sut.Add("b", 2);
        });
    }

    // --------------------------------------------------------
    // Enumerator — Current before MoveNext
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that accessing <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Current"/> before the first call to
    /// <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.MoveNext"/> throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_CurrentBeforeMoveNext_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        MultiValueDictionary<string, int>.Enumerator en = sut.GetEnumerator();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    // --------------------------------------------------------
    // Enumerator — Current after exhaustion
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that accessing <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Current"/> after the enumerator is exhausted throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [TestMethod]
    public void Enumerator_AfterExhaustion_CurrentShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        MultiValueDictionary<string, int>.Enumerator en = sut.GetEnumerator();
        while (en.MoveNext()) { }

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = en.Current;
        });
    }

    // --------------------------------------------------------
    // Enumerator — Reset
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Reset"/> repositions the enumerator to before the first element,
    /// allowing the dictionary to be enumerated again from the beginning.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenReset_ShouldAllowReEnumeration()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        MultiValueDictionary<string, int>.Enumerator en = sut.GetEnumerator();
        List<string> firstPass = new List<string>();
        while (en.MoveNext())
            firstPass.Add(en.Current.Key);

        en.Reset();

        List<string> secondPass = new List<string>();
        while (en.MoveNext())
            secondPass.Add(en.Current.Key);

        firstPass.Sort();
        secondPass.Sort();
        CollectionAssert.AreEqual(firstPass, secondPass);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Enumerator.Reset"/> throws <see cref="InvalidOperationException"/> when the dictionary was modified after the enumerator was created.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenResetAfterModification_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        MultiValueDictionary<string, int>.Enumerator en = sut.GetEnumerator();
        sut.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            en.Reset();
        });
    }

    // --------------------------------------------------------
    // Enumerator — additional mutation invalidation paths
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the dictionary is modified via <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedViaRemoveAll_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in sut)
                sut.RemoveAll("a");
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when the dictionary is cleared during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedViaClear_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in sut)
                sut.Clear();
        });
    }

    /// <summary>
    /// Verifies that the enumerator throws <see cref="InvalidOperationException"/> when a single value is removed from an existing key during enumeration.
    /// </summary>
    [TestMethod]
    public void Enumerator_WhenModifiedViaRemove_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, IReadOnlyList<int>> _ in sut)
                sut.Remove("a", 1);
        });
    }

    // --------------------------------------------------------
    // Flatten — additional mutation invalidation paths
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> throws <see cref="InvalidOperationException"/> when the dictionary is modified via <see cref="MultiValueDictionary{TKey,TValue}.RemoveAll"/> during enumeration.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenModifiedViaRemoveAll_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in sut.Flatten())
                sut.RemoveAll("a");
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> throws <see cref="InvalidOperationException"/> when the dictionary is cleared during enumeration.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenModifiedViaClear_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("b", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in sut.Flatten())
                sut.Clear();
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.Flatten"/> throws <see cref="InvalidOperationException"/> when a value is removed during enumeration.
    /// </summary>
    [TestMethod]
    public void Flatten_WhenModifiedViaRemove_ShouldThrowInvalidOperationException()
    {
        MultiValueDictionary<string, int> sut = new MultiValueDictionary<string, int>();
        sut.Add("a", 1);
        sut.Add("a", 2);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            foreach (KeyValuePair<string, int> _ in sut.Flatten())
                sut.Remove("a", 1);
        });
    }

    // --------------------------------------------------------
    // Regression: round-trip fidelity
    // --------------------------------------------------------

    /// <summary>
    /// Verifies that a large number of keys and values are stored and retrieved correctly.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Add_WhenManyKeysAndValuesAdded_ShouldRetrieveAllCorrectly()
    {
        MultiValueDictionary<int, int> sut = new MultiValueDictionary<int, int>();
        var keyCount = 200;
        var valuesPerKey = 10;

        for (var k = 0; k < keyCount; k++)
        {
            for (var v = 0; v < valuesPerKey; v++)
                sut.Add(k, v);
        }

        Assert.AreEqual(keyCount * valuesPerKey, sut.Count);
        Assert.AreEqual(keyCount, sut.KeyCount);

        for (var k = 0; k < keyCount; k++)
        {
            IReadOnlyList<int> values = sut[k];
            Assert.AreEqual(valuesPerKey, values.Count);
            for (var v = 0; v < valuesPerKey; v++)
                Assert.AreEqual(v, values[v]);
        }
    }
}
