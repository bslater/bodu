// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class MultiValueDictionaryTests
{

    /// <summary>
    /// Verifies that an absent-key view captured before the key is added does not become a live view
    /// of the newly created bucket.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenAbsentViewCapturedThenKeyAdded_ShouldKeepCapturedViewEmpty()
    {
        var mvd = new MultiValueDictionary<string, int>();

        IReadOnlyList<int> missingView = mvd["a"];

        mvd.Add("a", 1);

        Assert.IsEmpty(missingView);
        Assert.HasCount(1, mvd["a"]);
    }

    /// <summary>
    /// Verifies that the indexer uses the configured key comparer to resolve equivalent keys.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenCustomComparerUsed_ShouldResolveEquivalentKey()
    {
        var mvd = new MultiValueDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        mvd.Add("Alpha", 1);

        IReadOnlyList<int> values = mvd["alpha"];

        Assert.HasCount(1, values);
        Assert.AreEqual(1, values[0]);
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
        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that the absent-key indexer result is a read-only value view that cannot be mutated.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyAbsent_ShouldReturnReadOnlyEmptyValueView()
    {
        var mvd = new MultiValueDictionary<string, int>();

        IReadOnlyList<int> values = mvd["missing"];

        AssertReadOnlyValueViewCannotBeMutated(values);
        Assert.IsEmpty(values);
    }

    /// <summary>
    /// Verifies that the indexer throws <see cref="ArgumentNullException" /> for a null key.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsNull_ShouldThrowExactly()
    {
        var mvd = new MultiValueDictionary<string, int>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = mvd[null!];
        });
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

        Assert.HasCount(2, view);
        Assert.AreEqual(200, view[1]);
    }

    /// <summary>
    /// Verifies that the indexer returns the values for an existing key in insertion order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyPresent_ShouldReturnAssociatedValues()
    {
        var mvd = new MultiValueDictionary<string, int>();
        mvd.Add("a", 10);
        mvd.Add("a", 20);

        IReadOnlyList<int> result = mvd["a"];

        Assert.HasCount(2, result);
        CollectionAssert.AreEqual(new[] { 10, 20 }, result.ToList());
    }

}
