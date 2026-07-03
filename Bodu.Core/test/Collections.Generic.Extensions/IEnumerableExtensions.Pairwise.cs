// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Pairwise.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Pairwise
{
    /// <summary>
    /// Verifies that <c>Pairwise</c> returns an empty sequence when the source is empty.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenSourceIsEmpty_ShouldReturnEmpty() =>
        Assert.IsEmpty(Array.Empty<int>().Pairwise());

    /// <summary>
    /// Verifies that <c>Pairwise</c> returns an empty sequence when the source contains a single element.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenSourceHasSingleElement_ShouldReturnEmpty() =>
        Assert.IsEmpty(new[] { 1 }.Pairwise());

    /// <summary>
    /// Verifies that <c>Pairwise</c> returns a single pair when the source contains exactly two elements.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenSourceHasTwoElements_ShouldReturnOnePair()
    {
        (int Previous, int Current)[] result = new[] { 1, 2 }.Pairwise().ToArray();

        CollectionAssert.AreEqual(new[] { (1, 2) }, result);
    }

    /// <summary>
    /// Verifies that <c>Pairwise</c> returns overlapping adjacent pairs in source order.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenSourceHasManyElements_ShouldReturnOverlappingPairs()
    {
        (int Previous, int Current)[] result = new[] { 1, 2, 3, 4 }.Pairwise().ToArray();

        CollectionAssert.AreEqual(new[] { (1, 2), (2, 3), (3, 4) }, result);
    }

    /// <summary>
    /// Verifies that the projection overload receives the previous and current elements in that order.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenProjecting_ShouldPassPreviousThenCurrent()
    {
        int[] deltas = new[] { 10, 13, 9 }.Pairwise((previous, current) => current - previous).ToArray();

        CollectionAssert.AreEqual(new[] { 3, -4 }, deltas);
    }

    /// <summary>
    /// Verifies that <c>Pairwise</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().Pairwise();

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>Pairwise</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });

        _ = source.Pairwise().ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>Pairwise</c> can be bounded over an infinite source.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenSourceIsInfinite_ShouldBoundWithTake()
    {
        (int Previous, int Current)[] result = Naturals().Pairwise().Take(3).ToArray();

        CollectionAssert.AreEqual(new[] { (0, 1), (1, 2), (2, 3) }, result);
    }

    /// <summary>
    /// Verifies that <c>Pairwise</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Pairwise();
        });
    }

    /// <summary>
    /// Verifies that <c>Pairwise</c> throws <see cref="ArgumentNullException" /> when the selector is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Pairwise_WhenSelectorIsNull_ShouldThrowExactly()
    {
        Func<int, int, int> selector = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new[] { 1, 2 }.Pairwise(selector);
        });
    }

    private static IEnumerable<int> Naturals()
    {
        int i = 0;
        while (true)
            yield return i++;
    }
}
