// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies that the default constructor creates an empty set using the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ShouldCreateEmptySetWithDefaultComparer()
    {
        var sut = new NavigableSet<int>();

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

        var sut = new NavigableSet<int>(comparer);

        Assert.AreSame(comparer, sut.Comparer);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> comparer falls back to the default comparer.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerIsNull_ShouldFallBackToDefaultComparer()
    {
        var sut = new NavigableSet<int>((IComparer<int>?)null);

        Assert.AreSame(Comparer<int>.Default, sut.Comparer);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor sorts the source elements into ascending comparer order.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsUnsorted_ShouldContainElementsInSortedOrder()
    {
        var sut = new NavigableSet<int>(new[] { 5, 1, 4, 2, 3 });

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5 }, sut.ToList());
        Assert.AreEqual(5, sut.Count);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor collapses comparer-equal duplicates, keeping a single element.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceContainsDuplicates_ShouldDeduplicate()
    {
        var sut = new NavigableSet<int>(new[] { 3, 1, 3, 2, 1, 1 });

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, sut.ToList());
        Assert.AreEqual(3, sut.Count);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor keeps the first occurrence of a comparer-equal duplicate run.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenComparerEqualDuplicates_ShouldKeepFirstOccurrence()
    {
        var sut = new NavigableSet<string>(new[] { "Alpha", "ALPHA", "beta" }, StringComparer.OrdinalIgnoreCase);

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.Contains(sut.ToList(), "Alpha");
    }

    /// <summary>
    /// Verifies that the bulk-load constructor accepts an empty source and produces an empty set.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceIsEmpty_ShouldCreateEmptySet()
    {
        var sut = new NavigableSet<int>(Array.Empty<int>());

        Assert.AreEqual(0, sut.Count);
        Assert.IsFalse(sut.TryGetMin(out _));
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
            _ = new NavigableSet<int>((IEnumerable<int>)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the bulk-load constructor throws <see cref="ArgumentException" /> when the source contains a
    /// <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenSourceContainsNullElement_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NavigableSet<string>(new[] { "a", null!, "b" });
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a set built from every size 0 through 64 remains fully consistent — sorted enumeration, count,
    /// and rank round-trips — and continues to accept mutations, exercising every bulk-build coloring shape.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBulkLoadingEverySmallSize_ShouldRemainConsistentAndMutable()
    {
        for (int size = 0; size <= 64; size++)
        {
            int[] source = Enumerable.Range(0, size).Reverse().ToArray();
            var sut = new NavigableSet<int>(source);

            Assert.AreEqual(size, sut.Count, $"Count disagrees for size {size}.");
            CollectionAssert.AreEqual(Enumerable.Range(0, size).ToArray(), sut.ToList(), $"Order disagrees for size {size}.");

            for (int rank = 0; rank < size; rank++)
                Assert.AreEqual(rank, sut.GetAt(rank), $"GetAt({rank}) disagrees for size {size}.");

            // The bulk-built tree must remain a valid mutable set.
            Assert.IsTrue(sut.Add(size));
            Assert.IsTrue(sut.Remove(size));
            Assert.AreEqual(size, sut.Count);
        }
    }
}
