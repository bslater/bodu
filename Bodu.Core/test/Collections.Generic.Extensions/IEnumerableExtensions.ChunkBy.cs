// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ChunkBy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_ChunkBy
{
    /// <summary>
    /// Verifies that <c>ChunkBy</c> returns an empty sequence when the source is empty.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenSourceIsEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().ChunkBy(x => x));

    /// <summary>
    /// Verifies that <c>ChunkBy</c> returns a single keyed chunk for a single-element source.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenSourceHasSingleElement_ShouldReturnOneKeyedChunk()
    {
        (int Key, IReadOnlyList<int> Items)[] result = new[] { 5 }.ChunkBy(x => x % 2).ToArray();

        Assert.HasCount(1, result);
        Assert.AreEqual(1, result[0].Key);
        CollectionAssert.AreEqual(new[] { 5 }, result[0].Items.ToArray());
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> merges adjacent elements that produce equal keys.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenAdjacentKeysEqual_ShouldMergeIntoOneChunk()
    {
        (bool Key, IReadOnlyList<int> Items)[] result = new[] { 2, 4, 6 }.ChunkBy(x => x % 2 == 0).ToArray();

        Assert.HasCount(1, result);
        Assert.IsTrue(result[0].Key);
        CollectionAssert.AreEqual(new[] { 2, 4, 6 }, result[0].Items.ToArray());
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> groups by adjacency, so the same key separated by a different key produces separate
    /// chunks.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenSameKeySeparatedByAnother_ShouldProduceSeparateChunks()
    {
        char[] source = { 'a', 'a', 'b', 'a' };

        (char Key, IReadOnlyList<char> Items)[] result = source.ChunkBy(c => c).ToArray();

        Assert.HasCount(3, result);
        Assert.AreEqual('a', result[0].Key);
        CollectionAssert.AreEqual(new[] { 'a', 'a' }, result[0].Items.ToArray());
        Assert.AreEqual('b', result[1].Key);
        CollectionAssert.AreEqual(new[] { 'b' }, result[1].Items.ToArray());
        Assert.AreEqual('a', result[2].Key);
        CollectionAssert.AreEqual(new[] { 'a' }, result[2].Items.ToArray());
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> honours a supplied comparer when comparing keys.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenComparerSupplied_ShouldUseComparerForKeys()
    {
        string[] source = { "apple", "avocado", "banana", "apricot" };

        (string Key, IReadOnlyList<string> Items)[] result = source
            .ChunkBy(s => s[..1], StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.HasCount(3, result);
        CollectionAssert.AreEqual(new[] { "apple", "avocado" }, result[0].Items.ToArray());
        CollectionAssert.AreEqual(new[] { "banana" }, result[1].Items.ToArray());
        CollectionAssert.AreEqual(new[] { "apricot" }, result[2].Items.ToArray());
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> invokes the key selector exactly once per source element.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenEnumerated_ShouldInvokeKeySelectorOncePerElement()
    {
        int calls = 0;

        _ = new[] { 1, 1, 2, 3 }.ChunkBy(x =>
        {
            calls++;
            return x;
        }).ToArray();

        Assert.AreEqual(4, calls);
    }

    /// <summary>
    /// Verifies that a chunk yielded by <c>ChunkBy</c> remains stable after the enumerator advances.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenEnumeratorAdvances_ShouldKeepPreviousChunksStable()
    {
        using IEnumerator<(int Key, IReadOnlyList<int> Items)> enumerator =
            new[] { 1, 1, 2 }.ChunkBy(x => x).GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        IReadOnlyList<int> first = enumerator.Current.Items;

        Assert.IsTrue(enumerator.MoveNext());

        CollectionAssert.AreEqual(new[] { 1, 1 }, first.ToArray());
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().ChunkBy(x => x);

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 1, 2, 3 });

        _ = source.ChunkBy(x => x).ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.ChunkBy(x => x);
        });
    }

    /// <summary>
    /// Verifies that <c>ChunkBy</c> throws <see cref="ArgumentNullException" /> when the key selector is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ChunkBy_WhenKeySelectorIsNull_ShouldThrowExactly()
    {
        Func<int, int> keySelector = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1, 2 }.ChunkBy(keySelector);
        });
    }
}
