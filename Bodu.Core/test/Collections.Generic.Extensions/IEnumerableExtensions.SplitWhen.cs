// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.SplitWhen.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_SplitWhen
{
    /// <summary>
    /// Verifies that <c>SplitWhen</c> returns an empty sequence when the source is empty.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenSourceIsEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().SplitWhen((previous, current) => previous > current));

    /// <summary>
    /// Verifies that <c>SplitWhen</c> returns a single one-element chunk for a single-element source.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenSourceHasSingleElement_ShouldReturnOneChunk()
    {
        IReadOnlyList<int>[] result = new[] { 5 }.SplitWhen((previous, current) => previous > current).ToArray();

        Assert.HasCount(1, result);
        CollectionAssert.AreEqual(new[] { 5 }, result[0].ToArray());
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> returns a single chunk when the boundary predicate never returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenPredicateNeverTrue_ShouldReturnSingleChunk()
    {
        IReadOnlyList<int>[] result = new[] { 1, 2, 3 }.SplitWhen((_, _) => false).ToArray();

        Assert.HasCount(1, result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result[0].ToArray());
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> returns singleton chunks when the boundary predicate always returns
    /// <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenPredicateAlwaysTrue_ShouldReturnSingletonChunks()
    {
        IReadOnlyList<int>[] result = new[] { 1, 2, 3 }.SplitWhen((_, _) => true).ToArray();

        Assert.HasCount(3, result);
        CollectionAssert.AreEqual(new[] { 1 }, result[0].ToArray());
        CollectionAssert.AreEqual(new[] { 2 }, result[1].ToArray());
        CollectionAssert.AreEqual(new[] { 3 }, result[2].ToArray());
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> splits on a descending transition and starts the new chunk with the current
    /// element.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenSplittingOnDescendingTransition_ShouldStartNewChunkWithCurrent()
    {
        IReadOnlyList<int>[] result = new[] { 1, 2, 3, 1, 2 }
            .SplitWhen((previous, current) => previous > current)
            .ToArray();

        Assert.HasCount(2, result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result[0].ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2 }, result[1].ToArray());
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> invokes the boundary predicate exactly <c>count - 1</c> times.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenEnumerated_ShouldInvokePredicateOncePerAdjacentPair()
    {
        int calls = 0;

        _ = new[] { 1, 2, 3, 4 }.SplitWhen((_, _) =>
        {
            calls++;
            return false;
        }).ToArray();

        Assert.AreEqual(3, calls);
    }

    /// <summary>
    /// Verifies that a chunk yielded by <c>SplitWhen</c> remains stable after the enumerator advances.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenEnumeratorAdvances_ShouldKeepPreviousChunksStable()
    {
        using IEnumerator<IReadOnlyList<int>> enumerator =
            new[] { 1, 2, 3, 1, 2 }.SplitWhen((previous, current) => previous > current).GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        IReadOnlyList<int> first = enumerator.Current;

        Assert.IsTrue(enumerator.MoveNext());

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, first.ToArray());
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().SplitWhen((previous, current) => previous > current);

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3, 1 });

        _ = source.SplitWhen((previous, current) => previous > current).ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.SplitWhen((previous, current) => previous > current);
        });
    }

    /// <summary>
    /// Verifies that <c>SplitWhen</c> throws <see cref="ArgumentNullException" /> when the predicate is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void SplitWhen_WhenPredicateIsNull_ShouldThrowExactly()
    {
        Func<int, int, bool> shouldSplit = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1, 2 }.SplitWhen(shouldSplit);
        });
    }
}
