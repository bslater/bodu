// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.BatchPooled.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_BatchPooled
{

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.BatchPooled{TSource}(IEnumerable{TSource}, int)" /> with a batch
    /// size larger than the source returns a single trimmed batch.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenBatchSizeExceedsSourceLength_ShouldYieldSingleTrimmedBatch()
    {
        var source = new[] { 1, 2, 3 };

        var batches = source.BatchPooled(10).Select(b => b.ToArray()).ToArray();

        Assert.HasCount(1, batches);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, batches[0]);
    }

    /// <summary>
    /// Verifies that
    /// <see cref="IEnumerableExtensions.BatchPooled{TSource,TResult}(IEnumerable{TSource}, int, Func{TSource, int, TResult})" />
    /// defers execution until the returned sequence is enumerated.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenCalled_ForProjectionOverload_ShouldDeferExecution()
    {
        var wasEnumerated = false;
        IEnumerable<int> Source()
        {
            wasEnumerated = true;
            yield return 1;
        }

        _ = Source().BatchPooled(2, (x, _) => x);

        Assert.IsFalse(wasEnumerated, "BatchPooled (projection) should defer enumeration until the sequence is iterated.");
    }

    /// <summary>
    /// Verifies that <see cref="IEnumerableExtensions.BatchPooled{TSource}(IEnumerable{TSource}, int)" /> defers
    /// execution until the returned sequence is enumerated.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenCalled_ShouldDeferExecution()
    {
        var wasEnumerated = false;
        IEnumerable<int> Source()
        {
            wasEnumerated = true;
            yield return 1;
        }

        _ = Source().BatchPooled(2);

        Assert.IsFalse(wasEnumerated, "BatchPooled should defer enumeration until the sequence is iterated.");
    }
    /// <summary>
    /// Verifies that the unprojected overload of
    /// <see cref="IEnumerableExtensions.BatchPooled{TSource}(IEnumerable{TSource}, int)" /> yields batches with the
    /// expected contents for an even split.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenEvenSplit_ShouldReturnExpectedBatches()
    {
        IEnumerable<int> source = Enumerable.Range(1, 9);

        var batches = source.BatchPooled(3).Select(b => b.ToArray()).ToArray();

        Assert.HasCount(3, batches);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, batches[0]);
        CollectionAssert.AreEqual(new[] { 4, 5, 6 }, batches[1]);
        CollectionAssert.AreEqual(new[] { 7, 8, 9 }, batches[2]);
    }

    /// <summary>
    /// Verifies that the projection overload applies the supplied selector with the correct zero-based index for every
    /// emitted element.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenIndexedSelectorIsSupplied_ShouldApplyProjectionWithIndex()
    {
        IEnumerable<int> source = Enumerable.Range(1, 5);

        var batches = source.BatchPooled(2, (x, i) => $"{i}:{x}").Select(b => b.ToArray()).ToArray();

        Assert.HasCount(3, batches);
        CollectionAssert.AreEqual(new[] { "0:1", "1:2" }, batches[0]);
        CollectionAssert.AreEqual(new[] { "2:3", "3:4" }, batches[1]);
        CollectionAssert.AreEqual(new[] { "4:5" }, batches[2]);
    }

    /// <summary>
    /// Verifies that the indexed selector receives a contiguous, monotonically increasing zero-based index across
    /// batches.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenIndexedSelectorIsSupplied_ShouldPassMonotonicIndexAcrossBatches()
    {
        IEnumerable<int> source = Enumerable.Range(0, 6);
        var capturedIndices = new List<int>();

        _ = source.BatchPooled(2, (x, i) => { capturedIndices.Add(i); return x; })
                  .Select(b => b.ToArray()).ToArray();

        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, capturedIndices);
    }

    /// <summary>
    /// Verifies that the projection overload throws <see cref="ArgumentNullException" /> when the selector is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenSelectorIsNull_ShouldThrowExactly()
    {
        var source = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.BatchPooled(2, (Func<int, int, int>)null!).ToList();
        });
    }

    /// <summary>
    /// Verifies that a non-positive batch size for the projection overload throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void BatchPooled_WhenSizeIsInvalid_ForProjectionOverload_ShouldThrowExactly(int size)
    {
        var source = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.BatchPooled(size, (x, _) => x).ToList();
        });
    }

    /// <summary>
    /// Verifies that a non-positive batch size throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    public void BatchPooled_WhenSizeIsInvalid_ShouldThrowExactly(int size)
    {
        var source = new[] { 1, 2, 3 };
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.BatchPooled(size).ToList();
        });
    }

    /// <summary>
    /// Verifies that batching an empty source yields no batches.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenSourceIsEmpty_ShouldYieldNoBatches()
    {
        ReadOnlyMemory<int>[] batches = Array.Empty<int>().BatchPooled(4).ToArray();
        Assert.IsEmpty(batches);
    }

    /// <summary>
    /// Verifies that the projection overload throws <see cref="ArgumentNullException" /> when the source sequence is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenSourceIsNull_ForProjectionOverload_ShouldThrowExactly()
    {
        IEnumerable<int>? source = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.BatchPooled(2, (x, i) => x + i).ToList();
        });
    }

    /// <summary>
    /// Verifies that the unprojected overload throws <see cref="ArgumentNullException" /> when the source sequence is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int>? source = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.BatchPooled(2).ToList();
        });
    }

    /// <summary>
    /// Verifies that the final batch is trimmed to its actual element count when the source size is not a multiple of
    /// the batch size.
    /// </summary>
    [TestMethod]
    public void BatchPooled_WhenUnevenSplit_ShouldYieldTrimmedFinalBatch()
    {
        IEnumerable<int> source = Enumerable.Range(1, 10);

        var batches = source.BatchPooled(3).Select(b => b.ToArray()).ToArray();

        Assert.HasCount(4, batches);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, batches[0]);
        CollectionAssert.AreEqual(new[] { 4, 5, 6 }, batches[1]);
        CollectionAssert.AreEqual(new[] { 7, 8, 9 }, batches[2]);
        CollectionAssert.AreEqual(new[] { 10 }, batches[3]);
    }

}
