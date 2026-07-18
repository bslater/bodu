// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that the default constructor produces an empty set that uses the default equality comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldCreateEmptySetWithDefaultComparer()
    {
        var set = new ConcurrentHashSet<string>();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
        Assert.AreSame(EqualityComparer<string>.Default, set.Comparer);
    }

    /// <summary>
    /// Verifies that the comparer constructor stores the supplied comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerSupplied_ShouldUseThatComparer()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.AreSame(StringComparer.OrdinalIgnoreCase, set.Comparer);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer falls back to the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldUseDefaultComparer()
    {
        var set = new ConcurrentHashSet<string>(comparer: null);

        Assert.AreSame(EqualityComparer<string>.Default, set.Comparer);
    }

    /// <summary>
    /// Verifies that the capacity constructor accepts a zero or positive capacity hint and produces an empty set.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(1000)]
    public void Ctor_WhenCapacityIsNonNegative_ShouldCreateEmptySet(int capacity)
    {
        var set = new ConcurrentHashSet<int>(capacity);

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that a negative capacity throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-1000)]
    public void Ctor_WhenCapacityIsNegative_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentHashSet<int>(capacity);
        });

        Assert.AreEqual("capacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the collection constructor copies every distinct element from the source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceSupplied_ShouldContainDistinctElements()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3, 4]);

        AssertContainsExactly(set, 1, 2, 3, 4);
    }

    /// <summary>
    /// Verifies that the collection constructor collapses duplicate source elements into a single entry.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceContainsDuplicates_ShouldCollapseToDistinctElements()
    {
        var set = new ConcurrentHashSet<int>([7, 7, 7, 8, 8, 9]);

        AssertContainsExactly(set, 7, 8, 9);
    }

    /// <summary>
    /// Verifies that the collection constructor applies the supplied comparer when de-duplicating the source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceSuppliedWithComparer_ShouldDeduplicateUsingComparer()
    {
        var set = new ConcurrentHashSet<string>(
            ["alpha", "ALPHA", "Alpha", "beta"],
            StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(2, set.Count);
        Assert.IsTrue(set.Contains("ALPHA"));
        Assert.IsTrue(set.Contains("BETA"));
    }

    /// <summary>
    /// Verifies that the collection constructor throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new ConcurrentHashSet<int>(collection: null!);
        });

        Assert.AreEqual("collection", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing many instances concurrently produces independent, correctly seeded sets.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenUsedInParallel_ShouldProduceIndependentSets()
    {
        var results = new ConcurrentBag<ConcurrentHashSet<int>>();

        Parallel.For(0, 50, i =>
        {
            var set = new ConcurrentHashSet<int>([i]);
            results.Add(set);
        });

        Assert.HasCount(50, results);
        Assert.IsTrue(results.All(s => s.Count == 1));
    }

    /// <summary>
    /// Verifies that the collection constructor produces an empty set when the source collection is empty.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsEmpty_ShouldCreateEmptySet()
    {
        var set = new ConcurrentHashSet<int>([]);

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that the collection constructor copies a single-element source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceHasSingleElement_ShouldContainThatElement()
    {
        var set = new ConcurrentHashSet<int>([42]);

        AssertContainsExactly(set, 42);
    }

    /// <summary>
    /// Verifies that the collection constructor copies every element from an <see cref="ICollection{T}" /> source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsGenericCollection_ShouldCopyEveryElement()
    {
        var set = new ConcurrentHashSet<int>(new List<int> { 1, 2, 3, 4 });

        AssertContainsExactly(set, 1, 2, 3, 4);
    }

    /// <summary>
    /// Verifies that the collection constructor copies every element from a source that exposes
    /// <see cref="IReadOnlyCollection{T}" /> but not <see cref="ICollection{T}" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsReadOnlyCollection_ShouldCopyEveryElement()
    {
        var set = new ConcurrentHashSet<int>(new Queue<int>([5, 6, 7]));

        AssertContainsExactly(set, 5, 6, 7);
    }

    /// <summary>
    /// Verifies that the collection constructor copies every element from a lazily evaluated, non-counted source.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsLazyEnumerable_ShouldCopyEveryElement()
    {
        IEnumerable<int> source = Enumerable.Range(0, 10).Where(value => value % 2 == 0);

        var set = new ConcurrentHashSet<int>(source);

        AssertContainsExactly(set, 0, 2, 4, 6, 8);
    }

    /// <summary>
    /// Verifies that the capacity constructor interprets its argument as the expected number of elements (BCL
    /// semantics), converting it to <c>ceil(capacity / MaxLoadFactor)</c> buckets — with <c>MaxLoadFactor == 2</c> —
    /// clamped into the sizing envelope and rounded up to a power of two, so the split-ordered bucket mask is always
    /// valid and the table can absorb the expected population without an immediate resize.
    /// </summary>
    [TestMethod]
    [DataRow(0, 8)]
    [DataRow(1, 8)]
    [DataRow(7, 8)]
    [DataRow(8, 8)]
    [DataRow(9, 8)]
    [DataRow(16, 8)]
    [DataRow(17, 16)]
    [DataRow(32, 16)]
    [DataRow(33, 32)]
    [DataRow(64, 32)]
    [DataRow(1000, 512)]
    [DataRow(2048, 1024)]
    public void Ctor_WhenCapacityProvided_ShouldSizeBucketsForExpectedElementCount(int capacity, int expectedBucketCount)
    {
        var set = new ConcurrentHashSet<int>(capacity);

        Assert.AreEqual(expectedBucketCount, set.BucketCount);
        Assert.AreEqual(0, set.BucketCount & (set.BucketCount - 1), "Bucket count must be a power of two.");
    }

    /// <summary>
    /// Verifies that a capacity-constructed set, treated as an expected-element-count hint, holds exactly the expected
    /// elements after that many adds without losing or duplicating any.
    /// </summary>
    [TestMethod]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(1000)]
    public void Ctor_WhenCapacityProvided_ShouldHoldExpectedElementsAfterAdds(int capacity)
    {
        var set = new ConcurrentHashSet<int>(capacity);

        for (int i = 0; i < capacity; i++)
            Assert.IsTrue(set.Add(i), $"Element {i} must be added exactly once.");

        Assert.AreEqual(capacity, set.Count);
        for (int i = 0; i < capacity; i++)
            Assert.IsTrue(set.Contains(i), $"Element {i} must be present.");
    }

    /// <summary>
    /// Verifies that the collection constructor, which forwards the source's element count as the expected-element-count
    /// hint, produces a set containing the source's distinct elements even when the source carries duplicates.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceHasDuplicates_ShouldContainDistinctSourceElements()
    {
        var source = new List<int> { 1, 1, 2, 2, 2, 3, 4, 4, 5 };

        var set = new ConcurrentHashSet<int>(source);

        AssertContainsExactly(set, 1, 2, 3, 4, 5);
        Assert.AreEqual(5, set.Count);
    }
}
