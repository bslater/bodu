// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
        var set = new ConcurrentHashSet<int>(Array.Empty<int>());

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
    /// Verifies that the internal lock-striping constructor throws <see cref="ArgumentOutOfRangeException" /> when
    /// <c>concurrencyLevel</c> is zero or negative, naming the offending parameter rather than failing late inside
    /// the lock-array allocation with an opaque message.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-1000)]
    public void Ctor_WhenConcurrencyLevelIsZeroOrNegative_ShouldThrowArgumentOutOfRangeException(int concurrencyLevel)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ConcurrentHashSet<int>(concurrencyLevel, capacity: 16, comparer: null);
        });

        Assert.AreEqual("concurrencyLevel", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the internal lock-striping constructor succeeds for valid <c>concurrencyLevel</c> values and
    /// allocates exactly the requested number of lock stripes.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(4)]
    [DataRow(64)]
    public void Ctor_WhenConcurrencyLevelIsPositive_ShouldAllocateExactlyThatManyLocks(int concurrencyLevel)
    {
        var set = new ConcurrentHashSet<int>(concurrencyLevel, capacity: 16, comparer: null);

        Assert.AreEqual(concurrencyLevel, set.LockCount);
    }

    /// <summary>
    /// Verifies that the default constructor clamps the lock striping level to
    /// <see cref="ConcurrentHashSet{T}.MaxDefaultConcurrencyLevel" /> so that high-core machines do not allocate an
    /// excessive lock array for small sets.
    /// </summary>
    [TestMethod]
    public void Ctor_Default_ShouldClampLockCountToMaxDefaultConcurrencyLevel()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.LockCount >= 1, $"Expected at least one lock, got {set.LockCount}.");
        Assert.IsTrue(
            set.LockCount <= ConcurrentHashSet<int>.MaxDefaultConcurrencyLevel,
            $"Expected default lock count <= {ConcurrentHashSet<int>.MaxDefaultConcurrencyLevel}, got {set.LockCount}.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ClampDefaultConcurrencyLevel(int)" /> clamps a synthetic
    /// processor count into the inclusive range <c>[1, MaxDefaultConcurrencyLevel]</c>. Driving the clamp with
    /// scripted values means the regression net does not depend on the host's actual core count.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 1)]
    [DataRow(0, 1)]
    [DataRow(1, 1)]
    [DataRow(4, 4)]
    [DataRow(32, 32)]
    [DataRow(33, 32)]
    [DataRow(128, 32)]
    [DataRow(int.MaxValue, 32)]
    public void ClampDefaultConcurrencyLevel_ShouldClampInputToValidRange(int processorCount, int expected)
    {
        Assert.AreEqual(expected, ConcurrentHashSet<int>.ClampDefaultConcurrencyLevel(processorCount));
    }
}
