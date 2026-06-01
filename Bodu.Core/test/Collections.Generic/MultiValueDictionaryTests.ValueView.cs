// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.ValueView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that the dictionary enumerator does not expose a mutable backing list through its
    /// <see cref="MultiValueDictionary{TKey, TValue}.Enumerator.Current" /> value.
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
        Assert.HasCount(1, mvd["a"]);
        Assert.AreEqual(1, mvd["a"][0]);
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.GetValues" /> does not expose a mutable
    /// backing list that can bypass dictionary accounting.
    /// </summary>
    [TestMethod]
    public void GetValues_WhenValuesReturned_ShouldNotExposeMutableBackingList()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> values = mvd.GetValues("a");

        AssertReadOnlyValueViewCannotBeMutated(values);

        Assert.AreEqual(1, mvd.Count);
        Assert.HasCount(1, mvd.GetValues("a"));
        Assert.AreEqual(1, mvd.GetValues("a")[0]);
    }
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
        Assert.HasCount(1, mvd["a"]);
        Assert.AreEqual(1, mvd["a"][0]);
    }

    /// <summary>
    /// Verifies that clearing via <see cref="ICollection{T}.Clear" /> on a returned value view throws
    /// <see cref="NotSupportedException" /> and does not affect dictionary state.
    /// </summary>
    [TestMethod]
    public void ReturnedValueView_WhenClearedViaICollection_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> view = mvd.GetValues("a");

        if (view is ICollection<int> collection)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                collection.Clear();
            });
        }

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.GetValues("a")[0]);
    }

    /// <summary>
    /// Verifies that assigning via the <see cref="IList{T}" /> indexer setter on a returned value view throws
    /// <see cref="NotSupportedException" /> and does not affect dictionary state.
    /// </summary>
    [TestMethod]
    public void ReturnedValueView_WhenItemAssignedViaIList_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> view = mvd.GetValues("a");

        if (view is IList<int> list)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                list[0] = 99;
            });
        }

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.GetValues("a")[0]);
    }

    /// <summary>
    /// Verifies that inserting via <see cref="IList{T}.Insert" /> on a returned value view throws
    /// <see cref="NotSupportedException" /> and does not affect dictionary state.
    /// </summary>
    [TestMethod]
    public void ReturnedValueView_WhenItemInsertedViaIList_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> view = mvd.GetValues("a");

        if (view is IList<int> list)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                list.Insert(0, 99);
            });
        }

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.GetValues("a")[0]);
    }

    /// <summary>
    /// Verifies that removing at an index via <see cref="IList{T}.RemoveAt" /> on a returned value view throws
    /// <see cref="NotSupportedException" /> and does not affect dictionary state.
    /// </summary>
    [TestMethod]
    public void ReturnedValueView_WhenItemRemovedAtViaIList_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> view = mvd.GetValues("a");

        if (view is IList<int> list)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                list.RemoveAt(0);
            });
        }

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.GetValues("a")[0]);
    }

    /// <summary>
    /// Verifies that removing a value via <see cref="ICollection{T}.Remove" /> on a returned value view
    /// throws <see cref="NotSupportedException" /> and does not affect dictionary state.
    /// </summary>
    [TestMethod]
    public void ReturnedValueView_WhenItemRemovedViaICollection_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        IReadOnlyList<int> view = mvd.GetValues("a");

        if (view is ICollection<int> collection)
        {
            Assert.ThrowsExactly<NotSupportedException>(() =>
            {
                collection.Remove(1);
            });
        }

        Assert.AreEqual(1, mvd.Count);
        Assert.AreEqual(1, mvd.GetValues("a")[0]);
    }

    /// <summary>
    /// Verifies that mutating a returned value view via <see cref="ICollection{T}.Add" /> cannot leave
    /// <see cref="MultiValueDictionary{TKey, TValue}.Count" /> inconsistent with the stored values.
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
        Assert.HasCount(1, mvd.GetValues("a"));
    }

    /// <summary>
    /// Verifies that <see cref="MultiValueDictionary{TKey, TValue}.TryGetValues" /> does not expose a mutable
    /// backing list that can bypass dictionary accounting.
    /// </summary>
    [TestMethod]
    public void TryGetValues_WhenValuesReturned_ShouldNotExposeMutableBackingList()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 1);

        var found = mvd.TryGetValues("a", out IReadOnlyList<int> values);

        Assert.IsTrue(found);
        AssertReadOnlyValueViewCannotBeMutated(values);

        Assert.AreEqual(1, mvd.Count);
        Assert.HasCount(1, mvd["a"]);
        Assert.AreEqual(1, mvd["a"][0]);
    }

}
