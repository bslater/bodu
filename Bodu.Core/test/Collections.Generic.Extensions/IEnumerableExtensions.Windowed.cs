// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Windowed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Windowed
{
    /// <summary>
    /// Verifies that <c>Windowed</c> throws <see cref="ArgumentOutOfRangeException" /> when the window size is less
    /// than one.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void Windowed_WhenSizeIsLessThanOne_ShouldThrowExactly(int size)
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new[] { 1, 2, 3 }.Windowed(size);
        });

        Assert.AreEqual("size", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> returns an empty sequence when the source is empty.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSourceIsEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().Windowed(3));

    /// <summary>
    /// Verifies that <c>Windowed</c> returns an empty sequence when the source is shorter than the window size.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSourceShorterThanSize_ShouldReturnEmpty() =>
        Assert.IsEmpty(new[] { 1, 2 }.Windowed(3));

    /// <summary>
    /// Verifies that <c>Windowed</c> returns a single window when the source length equals the window size.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSourceEqualsSize_ShouldReturnOneWindow()
    {
        IReadOnlyList<int>[] result = new[] { 1, 2, 3 }.Windowed(3).ToArray();

        Assert.HasCount(1, result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result[0].ToArray());
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> returns overlapping complete windows when the source is longer than the window
    /// size.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSourceLongerThanSize_ShouldReturnOverlappingWindows()
    {
        IReadOnlyList<int>[] result = new[] { 1, 2, 3, 4, 5 }.Windowed(3).ToArray();

        Assert.HasCount(3, result);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result[0].ToArray());
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, result[1].ToArray());
        CollectionAssert.AreEqual(new[] { 3, 4, 5 }, result[2].ToArray());
    }

    /// <summary>
    /// Verifies that windows of size one enumerate each element individually.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSizeIsOne_ShouldReturnSingletonWindows()
    {
        IReadOnlyList<int>[] result = new[] { 1, 2, 3 }.Windowed(1).ToArray();

        Assert.HasCount(3, result);
        CollectionAssert.AreEqual(new[] { 1 }, result[0].ToArray());
        CollectionAssert.AreEqual(new[] { 2 }, result[1].ToArray());
        CollectionAssert.AreEqual(new[] { 3 }, result[2].ToArray());
    }

    /// <summary>
    /// Verifies that a window yielded by <c>Windowed</c> remains stable after the enumerator advances, confirming the
    /// internal ring buffer is snapshotted rather than aliased.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenEnumeratorAdvances_ShouldKeepPreviousWindowsStable()
    {
        using IEnumerator<IReadOnlyList<int>> enumerator = new[] { 1, 2, 3, 4, 5 }.Windowed(3).GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        IReadOnlyList<int> first = enumerator.Current;

        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, first.ToArray());
    }

    /// <summary>
    /// Verifies that the projection overload projects each complete window.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenProjecting_ShouldProjectEachWindow()
    {
        int[] sums = new[] { 1, 2, 3, 4 }.Windowed(2, window => window[0] + window[1]).ToArray();

        CollectionAssert.AreEqual(new[] { 3, 5, 7 }, sums);
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> does not yield the first window until exactly <c>size</c> source elements have
    /// been consumed.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenFirstWindowRequested_ShouldConsumeExactlySizeElements()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3, 4 });

        using IEnumerator<IReadOnlyList<int>> enumerator = source.Windowed(3).GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());

        // Three MoveNext calls fill the first window; nothing beyond size is consumed before the first yield.
        Assert.AreEqual(3, source.MoveNextCallCount);
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().Windowed(2);

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3, 4 });

        _ = source.Windowed(2).ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> can be bounded over an infinite source.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSourceIsInfinite_ShouldBoundWithTake()
    {
        IReadOnlyList<int>[] result = Naturals().Windowed(2).Take(3).ToArray();

        Assert.HasCount(3, result);
        CollectionAssert.AreEqual(new[] { 0, 1 }, result[0].ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2 }, result[1].ToArray());
        CollectionAssert.AreEqual(new[] { 2, 3 }, result[2].ToArray());
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Windowed(2);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Windowed</c> throws <see cref="ArgumentNullException" /> when the selector is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Windowed_WhenSelectorIsNull_ShouldThrowExactly()
    {
        Func<IReadOnlyList<int>, int> selector = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1, 2 }.Windowed(2, selector);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }

    private static IEnumerable<int> Naturals()
    {
        int i = 0;
        while (true)
            yield return i++;
    }
}
