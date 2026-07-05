// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that reading the indexer for a present key returns the associated value.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsPresent_ShouldReturnAssociatedValue()
    {
        var sut = CreateDictionary(10, 20, 30);

        Assert.AreEqual(2000, sut[20]);
    }

    /// <summary>
    /// Verifies that reading the indexer for an absent key throws <see cref="KeyNotFoundException" /> carrying the
    /// key in the message.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsAbsent_ShouldThrowKeyNotFoundException()
    {
        var sut = CreateDictionary(10, 20, 30);

        var ex = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = sut[99];
        });

        Assert.IsTrue(ex.Message.Contains("99", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that assigning a new key through the indexer inserts the entry at its comparer position.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsAbsent_ShouldInsertEntryAtComparerPosition()
    {
        var sut = CreateDictionary(10, 30);

        sut[20] = 2000;

        Assert.AreEqual(3, sut.Count);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.Select(e => e.Key).ToList());
        Assert.AreEqual(2000, sut[20]);
    }

    /// <summary>
    /// Verifies that assigning an existing key through the indexer overwrites the value in place without changing
    /// the count or key order.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenKeyIsPresent_ShouldOverwriteValueInPlace()
    {
        var sut = CreateDictionary(10, 20, 30);

        sut[20] = -1;

        Assert.AreEqual(3, sut.Count);
        Assert.AreEqual(-1, sut[20]);
        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, sut.Select(e => e.Key).ToList());
    }

    /// <summary>
    /// Verifies that overwriting a value through the indexer is not a structural mutation and does not invalidate an
    /// in-flight enumerator.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenValueOverwrittenDuringEnumeration_ShouldNotInvalidateEnumerator()
    {
        var sut = CreateDictionary(1, 2, 3);
        var seenKeys = new List<int>();

        foreach (KeyValuePair<int, int> entry in sut)
        {
            sut[2] = -1;
            seenKeys.Add(entry.Key);
        }

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, seenKeys);
        Assert.AreEqual(-1, sut[2]);
    }

    /// <summary>
    /// Verifies that reading the indexer with a <see langword="null" /> key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenGetKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = sut[null!];
        }, "key");
    }

    /// <summary>
    /// Verifies that assigning through the indexer with a <see langword="null" /> key throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenSetKeyIsNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableDictionary<string, int>();

        ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            sut[null!] = 1;
        }, "key");
    }
}
