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

    /// <summary>
    /// Verifies that the capacity-and-access-order constructor records the access-order flag.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityAndAccessOrder_ShouldRecordAccessOrder()
    {
        var dictionary = new SequencedDictionary<string, int>(capacity: 8, accessOrder: true);

        Assert.IsTrue(dictionary.AccessOrder);
    }

    /// <summary>
    /// Verifies that the capacity argument is only an initial-size hint and does not bound the number of entries.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMoreEntriesAddedThanCapacity_ShouldNotEvict()
    {
        var dictionary = new SequencedDictionary<int, int>(capacity: 2);

        for (int i = 0; i < 10; i++)
            dictionary.Add(i, i);

        Assert.AreEqual(10, dictionary.Count);
    }

    /// <summary>
    /// Verifies that the core capacity / access-order / comparer constructor honors all three arguments.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCapacityAccessOrderAndComparer_ShouldHonorAllArguments()
    {
        var dictionary = new SequencedDictionary<string, int>(4, accessOrder: true, StringComparer.OrdinalIgnoreCase);
        dictionary.Add("KEY", 1);

        Assert.IsTrue(dictionary.AccessOrder);
        Assert.IsTrue(dictionary.ContainsKey("key"));
        Assert.AreSame(StringComparer.OrdinalIgnoreCase, dictionary.Comparer);
    }

    /// <summary>
    /// Verifies that the collection-and-comparer constructor honors the supplied comparer when copying entries.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceAndComparer_ShouldUseComparer()
    {
        var source = new[] { new KeyValuePair<string, int>("Key", 1) };

        var dictionary = new SequencedDictionary<string, int>(source, StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(dictionary.ContainsKey("key"));
    }

    /// <summary>
    /// Verifies that the collection / access-order / comparer constructor copies entries and enables access ordering.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceAccessOrderAndComparer_ShouldCopyAndReorderOnAccess()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("b", 2),
        };

        var dictionary = new SequencedDictionary<string, int>(source, accessOrder: true, comparer: null);
        _ = dictionary["a"];

        Assert.IsTrue(dictionary.AccessOrder);
        CollectionAssert.AreEqual(new[] { "b", "a" }, dictionary.Keys.ToArray());
    }
}
