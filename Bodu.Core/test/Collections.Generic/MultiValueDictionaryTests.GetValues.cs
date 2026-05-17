// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.GetValues.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.GetValues" /> uses the configured key comparer.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenCustomComparerUsed_ShouldResolveEquivalentKey()
    {
        var mvd = new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        mvd.Add("Alpha", 1);

        IReadOnlyList<int> values = mvd.GetValues("ALPHA");

        Assert.HasCount(1, values);
        Assert.AreEqual(1, values[0]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> throws <see cref="KeyNotFoundException"/> when the key is absent.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenKeyAbsent_ShouldThrowKeyNotFoundException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = mvd.GetValues("missing");
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> throws <see cref="ArgumentNullException"/> for a null key.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenKeyIsNull_ShouldThrowArgumentNullException()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd.GetValues(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> returns the values for an existing key.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenKeyPresent_ShouldReturnAssociatedValues()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 100);
        mvd.Add("k", 200);

        IReadOnlyList<int> result = mvd.GetValues("k");

        Assert.HasCount(2, result);
        Assert.AreEqual(100, result[0]);
        Assert.AreEqual(200, result[1]);
    }

    /// <summary>
    /// Verifies that a value view becomes empty when the last value for the key is removed.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenLastValueRemovedAfterViewReturned_ShouldBecomeEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> values = mvd.GetValues("a");

        mvd.Remove("a", 1);

        Assert.IsEmpty(values);
        Assert.IsFalse(mvd.ContainsKey("a"));
    }

    /// <summary>
    /// Verifies that a value view becomes empty when all values for the key are removed.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenRemoveAllAfterViewReturned_ShouldBecomeEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);

        IReadOnlyList<int> values = mvd.GetValues("a");

        mvd.RemoveAll("a");

        Assert.IsEmpty(values);
        Assert.IsFalse(mvd.ContainsKey("a"));
    }
    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> always returns values in
    /// insertion order, for a range of value counts.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(20)]
    public void GetValues_WhenRetrieved_ShouldPreserveInsertionOrder(int valueCount)
    {
        var mvd = new MultiValueDictionary<string, int>();
        var expected = Enumerable.Range(0, valueCount).Select(i => i * 7).ToArray();
        foreach (var v in expected)
            mvd.Add("k", v);

        System.Collections.Generic.IReadOnlyList<int> actual = mvd.GetValues("k");

        CollectionAssert.AreEqual(expected, actual.ToList());
    }

    /// <summary>
    /// Verifies that the list returned by <see cref="MultiValueDictionary{TKey,TValue}.GetValues"/> reflects values added to the same key after the call.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenReturned_ShouldReflectSubsequentAdditions()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("k", 1);

        IReadOnlyList<int> view = mvd.GetValues("k");
        mvd.Add("k", 2);

        Assert.HasCount(2, view);
        Assert.AreEqual(2, view[1]);
    }

    /// <summary>
    /// Verifies that a value view reflects removal of a single value from the same key.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenValueRemovedAfterViewReturned_ShouldReflectRemoval()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);
        mvd.Add("a", 2);

        IReadOnlyList<int> values = mvd.GetValues("a");

        mvd.Remove("a", 1);

        CollectionAssert.AreEqual(new[] { 2 }, values.ToList());
    }

}
