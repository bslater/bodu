// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Scan.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Scan
{
    /// <summary>
    /// Verifies that <c>Scan</c> returns an empty sequence, and not the seed, when the source is empty.
    /// </summary>
    [TestMethod]
    public void Scan_WhenSourceIsEmpty_ShouldReturnEmptyAndNotYieldSeed() =>
        Assert.IsEmpty(Array.Empty<int>().Scan(100, (sum, x) => sum + x));

    /// <summary>
    /// Verifies that <c>Scan</c> emits each running accumulator state after folding each element.
    /// </summary>
    [TestMethod]
    public void Scan_WhenComputingRunningSum_ShouldEmitIntermediateStates()
    {
        int[] result = Enumerable.Range(1, 3).Scan(0, (sum, x) => sum + x).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 3, 6 }, result);
    }

    /// <summary>
    /// Verifies that <c>Scan</c> supports an accumulator type that differs from the source element type.
    /// </summary>
    [TestMethod]
    public void Scan_WhenAccumulatorTypeDiffersFromSource_ShouldFoldCorrectly()
    {
        string[] result = new[] { 1, 2, 3 }.Scan(string.Empty, (acc, x) => acc + x).ToArray();

        CollectionAssert.AreEqual(new[] { "1", "12", "123" }, result);
    }

    /// <summary>
    /// Verifies that the projection overload projects each intermediate accumulator state.
    /// </summary>
    [TestMethod]
    public void Scan_WhenProjecting_ShouldProjectEachState()
    {
        string[] result = Enumerable.Range(1, 3)
            .Scan(0, (sum, x) => sum + x, state => $"={state}")
            .ToArray();

        CollectionAssert.AreEqual(new[] { "=1", "=3", "=6" }, result);
    }

    /// <summary>
    /// Verifies that <c>Scan</c> invokes the accumulator exactly once per source element.
    /// </summary>
    [TestMethod]
    public void Scan_WhenEnumerated_ShouldInvokeAccumulatorOncePerElement()
    {
        int calls = 0;

        _ = Enumerable.Range(1, 4).Scan(0, (sum, x) =>
        {
            calls++;
            return sum + x;
        }).ToArray();

        Assert.AreEqual(4, calls);
    }

    /// <summary>
    /// Verifies that <c>Scan</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void Scan_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().Scan(0, (sum, x) => sum + x);

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>Scan</c> propagates an accumulator exception during enumeration rather than eagerly.
    /// </summary>
    [TestMethod]
    public void Scan_WhenAccumulatorThrows_ShouldThrowDuringEnumeration()
    {
        IEnumerable<int> result = new[] { 1, 2, 3 }.Scan(0, (_, _) => throw new InvalidOperationException("boom"));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = result.ToArray();
        });
    }

    /// <summary>
    /// Verifies that <c>Scan</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void Scan_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });

        _ = source.Scan(0, (sum, x) => sum + x).ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>Scan</c> throws <see cref="ArgumentNullException" /> when the source is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Scan_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Scan(0, (sum, x) => sum + x);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Scan</c> throws <see cref="ArgumentNullException" /> when the accumulator is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Scan_WhenAccumulatorIsNull_ShouldThrowExactly()
    {
        Func<int, int, int> accumulator = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1, 2 }.Scan(0, accumulator);
        });

        Assert.AreEqual("accumulator", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Scan</c> throws <see cref="ArgumentNullException" /> when the selector is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Scan_WhenSelectorIsNull_ShouldThrowExactly()
    {
        Func<int, int> selector = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1, 2 }.Scan(0, (sum, x) => sum + x, selector);
        });

        Assert.AreEqual("selector", ex.ParamName);
    }
}
