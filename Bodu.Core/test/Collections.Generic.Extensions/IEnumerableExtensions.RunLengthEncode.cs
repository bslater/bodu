// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.RunLengthEncode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_RunLengthEncode
{
    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> returns an empty sequence when the source is empty.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenSourceIsEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().RunLengthEncode());

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> returns a single run of count one for a single-element source.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenSourceHasSingleElement_ShouldReturnCountOne()
    {
        (char Value, int Count)[] result = new[] { 'a' }.RunLengthEncode().ToArray();

        CollectionAssert.AreEqual(new[] { ('a', 1) }, result);
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> compresses adjacent equal elements into runs with correct counts.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenSourceHasRuns_ShouldReturnValueCountPairs()
    {
        (char Value, int Count)[] result = new[] { 'a', 'a', 'b', 'b', 'b', 'a' }.RunLengthEncode().ToArray();

        CollectionAssert.AreEqual(new[] { ('a', 2), ('b', 3), ('a', 1) }, result);
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> yields single-count runs for a sequence of alternating values.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenValuesAlternate_ShouldReturnSingleCountRuns()
    {
        (int Value, int Count)[] result = new[] { 1, 2, 1, 2 }.RunLengthEncode().ToArray();

        CollectionAssert.AreEqual(new[] { (1, 1), (2, 1), (1, 1), (2, 1) }, result);
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> starts a new run when an earlier value reappears after a different value.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenValueReappearsAfterDifferentValue_ShouldStartNewRun()
    {
        (int Value, int Count)[] result = new[] { 1, 1, 2, 1 }.RunLengthEncode().ToArray();

        CollectionAssert.AreEqual(new[] { (1, 2), (2, 1), (1, 1) }, result);
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> honours a supplied comparer, grouping case-insensitive strings.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenComparerSupplied_ShouldUseComparerForRuns()
    {
        (string Value, int Count)[] result = new[] { "a", "A", "b" }
            .RunLengthEncode(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.HasCount(2, result);
        Assert.AreEqual("a", result[0].Value);
        Assert.AreEqual(2, result[0].Count);
        Assert.AreEqual("b", result[1].Value);
        Assert.AreEqual(1, result[1].Count);
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> throws <see cref="OverflowException" /> when a single run exceeds
    /// <see cref="int.MaxValue" /> elements.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void RunLengthEncode_WhenRunExceedsInt32MaxValue_ShouldThrowOverflowException()
    {
        // A synthetic source that yields the same value more than int.MaxValue times without materializing them.
        IEnumerable<byte> source = new RepeatingSource((long)int.MaxValue + 1);

        Assert.ThrowsExactly<OverflowException>(() =>
        {
            _ = source.RunLengthEncode().ToArray();
        });
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().RunLengthEncode();

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 1, 2 });

        _ = source.RunLengthEncode().ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>RunLengthEncode</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void RunLengthEncode_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.RunLengthEncode();
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// A source that reports the same value a specified number of times without allocating the elements, used to drive
    /// the overflow path without materializing more than <see cref="int.MaxValue" /> items.
    /// </summary>
    private sealed class RepeatingSource
        : IEnumerable<byte>
    {
        private readonly long _count;

        public RepeatingSource(long count) => _count = count;

        public IEnumerator<byte> GetEnumerator()
        {
            for (long i = 0; i < _count; i++)
                yield return 0;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
