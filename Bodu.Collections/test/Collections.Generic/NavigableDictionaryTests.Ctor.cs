// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableDictionaryTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableDictionaryTests
{
    /// <summary>
    /// Verifies that the default constructor creates an empty dictionary using the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldCreateEmptyDictionaryWithDefaultComparer()
    {
        var sut = new NavigableDictionary<int, string>();

        Assert.AreEqual(0, sut.Count);
        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that the comparer constructor retains the supplied comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerSupplied_ShouldUseSuppliedComparer()
    {
        var comparer = ReverseComparer<int>.Instance;

        var sut = new NavigableDictionary<int, string>(comparer);

        Assert.AreSame(comparer, sut.Comparer);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer falls back to the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldFallBackToDefaultComparer()
    {
        var sut = new NavigableDictionary<int, string>((IComparer<int>?)null);

        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor sorts the source entries into ascending key order and preserves each
    /// key's value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsUnsorted_ShouldContainEntriesInKeySortedOrder()
    {
        var source = new[]
        {
            new KeyValuePair<int, string>(5, "five"),
            new KeyValuePair<int, string>(1, "one"),
            new KeyValuePair<int, string>(4, "four"),
            new KeyValuePair<int, string>(2, "two"),
            new KeyValuePair<int, string>(3, "three"),
        };

        var sut = new NavigableDictionary<int, string>(source);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, sut.Select(e => e.Key).ToList());
        Assert.AreEqual(5, sut.Count);
        Assert.AreEqual("three", sut[3]);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor throws <see cref="ArgumentException" /> when the source contains two
    /// comparer-equal keys, matching the <see cref="Dictionary{TKey, TValue}" /> collection-constructor contract.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceContainsDuplicateKeys_ShouldThrowArgumentException()
    {
        var source = new[]
        {
            new KeyValuePair<int, string>(1, "first"),
            new KeyValuePair<int, string>(2, "second"),
            new KeyValuePair<int, string>(1, "again"),
        };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NavigableDictionary<int, string>(source);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor treats comparer-equal keys as duplicates even when they differ under
    /// the default comparison.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceContainsComparerEqualKeys_ShouldThrowArgumentException()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("Alpha", 1),
            new KeyValuePair<string, int>("ALPHA", 2),
        };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NavigableDictionary<string, int>(source, StringComparer.OrdinalIgnoreCase);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor accepts an empty source and produces an empty dictionary.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsEmpty_ShouldCreateEmptyDictionary()
    {
        var sut = new NavigableDictionary<int, string>(Array.Empty<KeyValuePair<int, string>>());

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.TryGetMinEntry(out _));
    }

    /// <summary>
    /// Verifies that the bulk-load constructor throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new NavigableDictionary<int, string>((IEnumerable<KeyValuePair<int, string>>)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor throws <see cref="ArgumentException" /> when the source contains an
    /// entry with a <see langword="null" /> key.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceContainsNullKey_ShouldThrowArgumentException()
    {
        var source = new[]
        {
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>(null!, 2),
        };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NavigableDictionary<string, int>(source);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a dictionary built from every size 0 through 64 remains fully consistent — key-sorted
    /// enumeration, count, value fidelity, and rank round-trips — and continues to accept mutations, exercising every
    /// bulk-build coloring shape.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBulkLoadingEverySmallSize_ShouldRemainConsistentAndMutable()
    {
        for (int size = 0; size <= 64; size++)
        {
            KeyValuePair<int, int>[] source = Enumerable.Range(0, size)
                .Reverse()
                .Select(key => new KeyValuePair<int, int>(key, key * 100))
                .ToArray();
            var sut = new NavigableDictionary<int, int>(source);

            Assert.AreEqual(size, sut.Count, $"Count disagrees for size {size}.");
            CollectionAssert.AreEqual(Enumerable.Range(0, size).ToArray(), sut.Select(e => e.Key).ToList(), $"Order disagrees for size {size}.");

            for (int rank = 0; rank < size; rank++)
            {
                Assert.AreEqual(rank, sut.GetAt(rank).Key, $"GetAt({rank}).Key disagrees for size {size}.");
                Assert.AreEqual(rank * 100, sut.GetAt(rank).Value, $"GetAt({rank}).Value disagrees for size {size}.");
            }

            // The bulk-built tree must remain a valid mutable dictionary.
            sut.Add(size, size * 100);
            Assert.IsTrue(sut.Remove(size));
            Assert.AreEqual(size, sut.Count);
        }
    }
}
