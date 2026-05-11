// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{
    /// <summary>
    /// Verifies that the indexer does not expose a mutable backing list that can bypass dictionary accounting.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenValuesReturned_ShouldNotExposeMutableBackingList()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> values = mvd["a"];

        AssertReadOnlyValueViewCannotBeMutated(values);

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd["a"].Count);
        Assert.AreEqual(1, mvd["a"][0]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.GetValues" /> does not expose a mutable backing list.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenValuesReturned_ShouldNotExposeMutableBackingList()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> values = mvd.GetValues("a");

        AssertReadOnlyValueViewCannotBeMutated(values);

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.GetValues("a").Count);
        Assert.AreEqual(1, mvd.GetValues("a")[0]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.TryGetValues" /> does not expose a mutable backing list.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenValuesReturned_ShouldNotExposeMutableBackingList()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        bool found = mvd.TryGetValues("a", out IReadOnlyList<int> values);

        Assert.IsTrue(found);
        AssertReadOnlyValueViewCannotBeMutated(values);

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd["a"].Count);
        Assert.AreEqual(1, mvd["a"][0]);
    }

    /// <summary>
    /// Verifies that the dictionary enumerator does not expose a mutable backing list through its current value.
    /// </summary>
    [TestMethod]
    public void GetEnumerator_WhenCurrentValueReturned_ShouldNotExposeMutableBackingList()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());

        IReadOnlyList<int> values = enumerator.Current.Value;

        AssertReadOnlyValueViewCannotBeMutated(values);

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd["a"].Count);
        Assert.AreEqual(1, mvd["a"][0]);
    }

    /// <summary>
    /// Verifies that mutating a returned value view cannot leave <see cref="MultiValueDictionary{TKey, TValue}.Count" />
    /// inconsistent with the actual number of stored values.
    /// </summary>
    [TestMethod]
    public void ReturnedValueView_WhenMutationAttempted_ShouldNotBypassCountOrVersion()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> values = mvd.GetValues("a");

        if (values is ICollection<int> collection)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                collection.Add(2);
            });
        }

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.GetValues("a").Count);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.AddRange" /> leaves the dictionary unchanged
    /// when source enumeration fails before the add operation completes.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenSourceThrowsDuringEnumeration_ShouldLeaveDictionaryUnchanged()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            mvd.AddRange("a", EnumerateOneThenThrow());
        });

        Assert.AreEqual(0, mvd.Count);
        Assert.AreEqual(0, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.AddRange" /> preserves existing state when
    /// source enumeration fails before the add operation completes.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenSourceThrowsDuringEnumeration_ShouldPreserveExistingDictionaryState()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("existing", 42);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            mvd.AddRange("new", EnumerateOneThenThrow());
        });

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.IsTrue(mvd.ContainsKey("existing"));
        Assert.IsFalse(mvd.ContainsKey("new"));
        Assert.AreEqual(42, mvd.GetValues("existing")[0]);
    }

    /// <summary>
    /// Verifies that adding an empty range for a missing key is a no-op and does not invalidate an active enumerator.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenEmptySourceForMissingKey_ShouldNotInvalidateActiveEnumerator()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        using MultiValueDictionary<string, int>.Enumerator enumerator = mvd.GetEnumerator();

        mvd.AddRange("b", Array.Empty<int>());

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual("a", enumerator.Current.Key);
        Assert.IsFalse(enumerator.MoveNext());

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.KeyCount);
        Assert.IsFalse(mvd.ContainsKey("b"));
    }

    /// <summary>
    /// Verifies that the indexer returns an empty list for an absent key rather than throwing.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyAbsent_ShouldReturnEmptyList()
    {
        var mvd = new MultiValueDictionary<string, int>();

        IReadOnlyList<int> result = mvd["missing"];

        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    /// <summary>
    /// Verifies that the indexer returns the values for an existing key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyPresent_ShouldReturnAssociatedValues()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 10);
        mvd.Add("a", 20);

        IReadOnlyList<int> result = mvd["a"];

        Assert.AreEqual(2, result.Count);
        CollectionAssert.AreEqual(new[] { 10, 20 }, result.ToList());
    }

    /// <summary>
    /// Verifies that the list returned by the indexer reflects values added to the same key after the call.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyPresent_ShouldReflectSubsequentAdditions()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 100);

        IReadOnlyList<int> view = mvd["k"];
        mvd.Add("k", 200);

        Assert.AreEqual(2, view.Count);
        Assert.AreEqual(200, view[1]);
    }

    /// <summary>
    /// Verifies that the absent-key indexer result is a read-only value view.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyAbsent_ShouldReturnReadOnlyEmptyValueView()
    {
        var mvd = new MultiValueDictionary<string, int>();

        IReadOnlyList<int> values = mvd["missing"];

        AssertReadOnlyValueViewCannotBeMutatedForValueViewTests(values);
        Assert.AreEqual(0, values.Count);
    }

    /// <summary>
    /// Verifies that the absent-key empty value view is not converted into a live view when the key is later added.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAbsentViewCapturedThenKeyAdded_ShouldKeepCapturedViewEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();

        IReadOnlyList<int> missingView = mvd["a"];

        mvd.Add("a", 1);

        Assert.AreEqual(0, missingView.Count);
        Assert.AreEqual(1, mvd["a"].Count);
    }
    /// <summary>
    /// Verifies that the indexer throws <see cref="ArgumentNullException" /> for a null key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd[null!];
        });
    }

    /// <summary>
    /// Verifies that the indexer uses the configured key comparer.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenCustomComparerUsed_ShouldResolveEquivalentKey()
    {
        var mvd = new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        mvd.Add("Alpha", 1);

        IReadOnlyList<int> values = mvd["alpha"];

        Assert.AreEqual(1, values.Count);
        Assert.AreEqual(1, values[0]);
    }

    private static void AssertReadOnlyValueViewCannotBeMutated(IReadOnlyList<int> values)
    {
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

    private static IEnumerable<int> EnumerateOneThenThrow()
    {
        yield return 1;

        throw new InvalidOperationException("Simulated source enumeration failure.");
    }
}
