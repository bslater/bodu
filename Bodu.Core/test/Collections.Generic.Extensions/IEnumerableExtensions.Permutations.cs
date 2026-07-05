// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.Permutations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Infrastructure;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_Permutations
{
    /// <summary>
    /// Verifies that <c>Permutations</c> emits all 3! orderings of a three-element source in lexicographic position
    /// order.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSourceHasThreeElements_ShouldReturnAllSixRowsInLexicographicOrder()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3 }.Permutations().ToArray();

        int[][] expected =
        [
            [1, 2, 3],
            [1, 3, 2],
            [2, 1, 3],
            [2, 3, 1],
            [3, 1, 2],
            [3, 2, 1],
        ];

        AssertRows(expected, rows);
    }

    /// <summary>
    /// Verifies that the sized overload emits all P(4, 2) = 12 ordered selections in lexicographic position order.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSizeIsTwo_ForFourElements_ShouldReturnTwelveRowsInLexicographicOrder()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3, 4 }.Permutations(2).ToArray();

        int[][] expected =
        [
            [1, 2], [1, 3], [1, 4],
            [2, 1], [2, 3], [2, 4],
            [3, 1], [3, 2], [3, 4],
            [4, 1], [4, 2], [4, 3],
        ];

        AssertRows(expected, rows);
    }

    /// <summary>
    /// Verifies that a size of zero yields exactly one empty row, per the P(n, 0) = 1 convention.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSizeIsZero_ShouldReturnSingleEmptyRow()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3 }.Permutations(0).ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.IsEmpty(rows[0]);
    }

    /// <summary>
    /// Verifies that an empty source yields a single empty row from the full-permutations overload (the empty
    /// permutation, 0! = 1).
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSourceIsEmpty_ShouldReturnSingleEmptyRow()
    {
        IReadOnlyList<int>[] rows = Array.Empty<int>().Permutations().ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.IsEmpty(rows[0]);
    }

    /// <summary>
    /// Verifies that a size greater than the source count yields an empty sequence rather than throwing.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSizeExceedsSourceCount_ShouldReturnEmpty() =>
        Assert.IsEmpty(new[] { 1, 2 }.Permutations(3));

    /// <summary>
    /// Verifies that a negative size throws <see cref="ArgumentOutOfRangeException" /> eagerly at the call site.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSizeIsNegative_ShouldThrowExactly()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new[] { 1, 2 }.Permutations(-1);
        });

        Assert.AreEqual("size", ex.ParamName);
    }

    /// <summary>
    /// Verifies that each emitted row is an independent snapshot: retained rows are distinct instances and keep their
    /// content after enumeration completes.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenRowsAreRetained_ShouldBeIndependentSnapshots()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 2, 3 }.Permutations().ToArray();

        Assert.AreNotSame(rows[0], rows[1]);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows[0].ToArray());
        CollectionAssert.AreEqual(new[] { 1, 3, 2 }, rows[1].ToArray());
    }

    /// <summary>
    /// Verifies that duplicate source values are treated positionally, producing duplicate rows rather than being
    /// de-duplicated.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSourceContainsDuplicates_ShouldTreatElementsPositionally()
    {
        IReadOnlyList<int>[] rows = new[] { 1, 1 }.Permutations().ToArray();

        AssertRows([[1, 1], [1, 1]], rows);
    }

    /// <summary>
    /// Verifies that <c>Permutations</c> defers enumeration of the source until the result is iterated.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenResultNotEnumerated_ShouldNotEnumerateSource()
    {
        bool enumerated = false;

        IEnumerable<int> Source()
        {
            enumerated = true;
            yield return 1;
        }

        _ = Source().Permutations();
        _ = Source().Permutations(1);

        Assert.IsFalse(enumerated);
    }

    /// <summary>
    /// Verifies that <c>Permutations</c> enumerates the source exactly once and disposes its enumerator.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenEnumerated_ShouldEnumerateSourceOnceAndDispose()
    {
        var source = new DisposeTrackingEnumerable<int>(new[] { 1, 2, 3 });

        _ = source.Permutations().ToArray();

        Assert.AreEqual(1, source.GetEnumeratorCallCount);
        Assert.AreEqual(1, source.DisposeCallCount);
    }

    /// <summary>
    /// Verifies that <c>Permutations</c> throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Permutations();
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the sized overload throws <see cref="ArgumentNullException" /> when the source is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Permutations_WhenSourceIsNull_ForSizedOverload_ShouldThrowExactly()
    {
        IEnumerable<int> source = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.Permutations(2);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a six-element source produces exactly 6! = 720 distinct full-permutation rows.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Permutations_WhenSourceHasSixElements_ShouldReturnAllDistinctRows()
    {
        IReadOnlyList<int>[] rows = Enumerable.Range(1, 6).Permutations().ToArray();

        Assert.AreEqual(720, rows.Length);
        Assert.AreEqual(720, rows.Select(row => string.Join(",", row)).Distinct().Count());
    }

    private static void AssertRows(int[][] expected, IReadOnlyList<int>[] actual)
    {
        Assert.AreEqual(expected.Length, actual.Length);

        for (int i = 0; i < expected.Length; i++)
            CollectionAssert.AreEqual(expected[i], actual[i].ToArray(), $"Row {i} differs.");
    }
}
