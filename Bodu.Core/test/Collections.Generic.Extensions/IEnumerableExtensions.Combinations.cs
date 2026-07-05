// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Combinations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Combinations
{
    /// <summary>
    /// Verifies that <c>Combinations</c> emits all C(4, 2) = 6 subsets in lexicographic position order, each row in
    /// source order.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenSizeIsTwo_ForFourElements_ShouldReturnSixRowsInLexicographicOrder()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3, 4 }.Combinations(2).ToArray();

        int[][] expected =
        [
            [1, 2], [1, 3], [1, 4],
            [2, 3], [2, 4],
            [3, 4],
        ];

        AssertRows(expected, rows);
    }

    /// <summary>
    /// Verifies that a size equal to the source count yields a single row containing the whole source.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenSizeEqualsSourceCount_ShouldReturnSingleFullRow()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3 }.Combinations(3).ToArray();

        AssertRows([[1, 2, 3]], rows);
    }

    /// <summary>
    /// Verifies that a size of zero yields exactly one empty row, per the C(n, 0) = 1 convention.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenSizeIsZero_ShouldReturnSingleEmptyRow()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3 }.Combinations(0).ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.IsEmpty(rows[0]);
    }

    /// <summary>
    /// Verifies that a size greater than the source count yields an empty sequence rather than throwing.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenSizeExceedsSourceCount_ShouldReturnEmpty() =>
        Assert.IsEmpty(new[] { 1, 2 }.Combinations(3));

    /// <summary>
    /// Verifies that a negative size throws <see cref="ArgumentOutOfRangeException" /> eagerly at the call site.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenSizeIsNegative_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new[] { 1, 2 }.Combinations(-1);
        });

        Assert.AreEqual("size", ex.ParamName);
    }

    /// <summary>
    /// Verifies that each emitted row is an independent snapshot: retained rows are distinct instances and keep their
    /// content after enumeration completes.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenRowsAreRetained_ShouldBeIndependentSnapshots()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3 }.Combinations(2).ToArray();

        Assert.AreNotSame(rows[0], rows[1]);
        CollectionAssert.AreEqual(new[] { 1, 2 }, rows[0].ToArray());
        CollectionAssert.AreEqual(new[] { 1, 3 }, rows[1].ToArray());
    }

    /// <summary>
    /// Verifies that duplicate source values are treated positionally, producing duplicate rows rather than being
    /// de-duplicated.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenSourceContainsDuplicates_ShouldTreatElementsPositionally()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 1, 2 }.Combinations(2).ToArray();

        AssertRows([[1, 1], [1, 2], [1, 2]], rows);
    }

    /// <summary>
    /// Verifies that <c>Combinations</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().Combinations(1);

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>Combinations</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3, 4 });

        _ = source.Combinations(2).ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>Combinations</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Combinations_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Combinations(2);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a ten-element source produces exactly C(10, 4) = 210 distinct rows, each strictly increasing.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Combinations_WhenSourceHasTenElements_ShouldReturnAllDistinctSortedRows()
    {
        IReadOnlyList<int>[] rows = Enumerable.Range(1, 10).Combinations(4).ToArray();

        Assert.AreEqual(210, rows.Length);
        Assert.AreEqual(210, rows.Select(row => string.Join(",", row)).Distinct().Count());
        Assert.IsTrue(rows.All(row => row.Pairwise().All(pair => pair.Previous < pair.Current)));
    }

    private static void AssertRows(int[][] expected, IReadOnlyList<int>[] actual)
    {
        Assert.AreEqual(expected.Length, actual.Length);

        for (int i = 0; i < expected.Length; i++)
            CollectionAssert.AreEqual(expected[i], actual[i].ToArray(), $"Row {i} differs.");
    }
}
