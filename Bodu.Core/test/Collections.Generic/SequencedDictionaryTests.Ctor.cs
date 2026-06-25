// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequencedDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class SequencedDictionaryTests
{
    /// <summary>
    /// Verifies that the parameterless constructor creates an empty, insertion-order dictionary.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParameterless_ShouldCreateEmptyInsertionOrderDictionary()
    {
        var dictionary = new SequencedDictionary<string, int>();

        Assert.AreEqual(0, dictionary.Count);
        Assert.IsFalse(dictionary.AccessOrder);
    }

    /// <summary>
    /// Verifies that the constructor throws when capacity is negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsNegative_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new SequencedDictionary<string, int>(-1);
        });
    }

    /// <summary>
    /// Verifies that the constructor accepts a capacity of zero as a valid initial-size hint.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityIsZero_ShouldCreateEmptyDictionary()
    {
        var dictionary = new SequencedDictionary<string, int>(0);

        Assert.AreEqual(0, dictionary.Count);
    }

    /// <summary>
    /// Verifies that the access-order constructor flag is reflected by the <see cref="SequencedDictionary{TKey, TValue}.AccessOrder" /> property.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAccessOrderRequested_ShouldReportAccessOrder()
    {
        var dictionary = new SequencedDictionary<string, int>(accessOrder: true);

        Assert.IsTrue(dictionary.AccessOrder);
    }

    /// <summary>
    /// Verifies that the constructor uses the default equality comparer when <see langword="null" /> is supplied.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var dictionary = new SequencedDictionary<string, int>(comparer: null);
        dictionary.Add("KEY", 123);

        Assert.IsFalse(dictionary.ContainsKey("key"));
        Assert.AreSame(EqualityComparer<string>.Default, dictionary.Comparer);
    }

    /// <summary>
    /// Verifies that the constructor honors a supplied equality comparer for key lookups.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsProvided_ShouldUseComparer()
    {
        var dictionary = new SequencedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        dictionary.Add("KEY", 123);

        Assert.IsTrue(dictionary.ContainsKey("key"));
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.Comparer);
    }

    /// <summary>
    /// Verifies that the collection constructor copies all entries and preserves their source order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceProvided_ShouldCopyEntriesInOrder()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("c", 3),
        };

        var dictionary = new SequencedDictionary<string, int>(source);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dictionary.Keys.ToArray());
    }

    /// <summary>
    /// Verifies that the collection constructor throws when the source sequence is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new SequencedDictionary<string, int>((IEnumerable<KeyValuePair<string, int>>)null!);
        });
    }

    /// <summary>
    /// Verifies that the collection constructor throws when the source sequence contains duplicate keys.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceHasDuplicateKeys_ShouldThrowExactly()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("a", 2),
        };

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new SequencedDictionary<string, int>(source);
        });
    }
}
