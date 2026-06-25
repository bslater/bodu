// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Scale.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that a large number of insertions enumerate in insertion order.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Scale_WhenManyEntriesInserted_ShouldEnumerateInInsertionOrder()
    {
        const int count = 10_000;
        var dictionary = new SequencedDictionary<int, int>();

        for (int i = 0; i < count; i++)
            dictionary.Add(i, i * 2);

        Assert.AreEqual(count, dictionary.Count);
        CollectionAssert.AreEqual(Enumerable.Range(0, count).ToArray(), dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that removing every other entry from a large dictionary preserves the insertion order of the remainder.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Scale_WhenEveryOtherEntryRemoved_ShouldPreserveOrderOfRemainder()
    {
        const int count = 10_000;
        var dictionary = new SequencedDictionary<int, int>();

        for (int i = 0; i < count; i++)
            dictionary.Add(i, i);

        for (int i = 0; i < count; i += 2)
            Assert.IsTrue(dictionary.Remove(i));

        var expected = Enumerable.Range(0, count).Where(i => i % 2 == 1).ToArray();
        CollectionAssert.AreEqual(expected, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that, under sustained access-order churn, the most recently accessed key is the last entry and the
    /// least recently accessed key is the first entry.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Scale_WhenAccessOrderChurn_ShouldKeepMostRecentlyUsedLast()
    {
        const int count = 1_000;
        var dictionary = new SequencedDictionary<int, int>(accessOrder: true);

        for (int i = 0; i < count; i++)
            dictionary.Add(i, i);

        // Access keys in ascending order; each read promotes the key to the tail.
        for (int i = 0; i < count; i++)
            _ = dictionary[i];

        // After touching 0..count-1 in order, the order is exactly 0..count-1 again.
        Assert.AreEqual(0, dictionary.First.Key);
        Assert.AreEqual(count - 1, dictionary.Last.Key);

        // Touch a single key in the middle; it must move to the tail.
        _ = dictionary[500];
        Assert.AreEqual(500, dictionary.Last.Key);
        Assert.AreEqual(0, dictionary.First.Key);
    }
}
