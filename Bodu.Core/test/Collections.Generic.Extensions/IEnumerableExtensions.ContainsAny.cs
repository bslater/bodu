// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAny.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_ContainsAny
{

    /// <summary>
    /// Provides scenarios for <c>ContainsAny</c> covering partial overlap, no overlap, empty sequences, and
    /// duplicate-item handling.
    /// </summary>
    public static IEnumerable<object[]> GetContainsAnyTestCases() =>
    [
        [new[] { 1, 2, 3, 4, 5 }, new[] { 2 }, true],
        [new[] { 1, 2, 3, 4, 5 }, new[] { 6, 7, 8 }, false],
        [new[] { 1, 2, 3, 4, 5 }, new[] { 9, 3, 7 }, true],
        [new[] { 1, 2, 3, 4, 5 }, Array.Empty<int>(), false],
        [Array.Empty<int>(), new[] { 1, 2, 3 }, false],
        [Array.Empty<int>(), Array.Empty<int>(), false],
        [new[] { 1, 1, 1 }, new[] { 1 }, true],
    ];

    /// <summary>
    /// Verifies that <c>ContainsAny</c> correctly reports whether at least one query item appears in the source
    /// sequence across overlap, non-overlap, empty, and duplicate cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetContainsAnyTestCases))]
    public void ContainsAny_WhenEvaluatingIntegerSequences_ShouldReturnExpectedResult(
        int[] source,
        int[] items,
        bool expected) => Assert.AreEqual(expected, source.ContainsAny(items));

    /// <summary>
    /// Verifies the items-built-set branch when item sequence size is unknown by supplying a non-
    /// <see cref="ICollection{T}" /> enumerable (a lazily-yielded sequence).
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenItemsHasUnknownSize_ShouldReturnExpectedResult()
    {
        int[] source = [1, 2, 3, 4, 5];
        IEnumerable<int> items = LazyRange(10, 3);

        Assert.IsFalse(source.ContainsAny(items));
        Assert.IsTrue(source.ContainsAny(LazyRange(3, 3)));

        static IEnumerable<int> LazyRange(int start, int count)
        {
            for (int i = 0; i < count; i++)
                yield return start + i;
        }
    }

    /// <summary>
    /// Verifies that a null items sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenItemsIsNull_ShouldThrowExactly()
    {
        int[] source = [1, 2, 3];
        IEnumerable<int>? items = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.ContainsAny(items!);
        });
    }

    /// <summary>
    /// Verifies that a null source sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenSourceIsNull_ShouldThrowExactly()
    {
        IEnumerable<int>? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.ContainsAny([1]);
        });
    }

    /// <summary>
    /// Verifies the smaller-source optimisation branch: when <paramref name="source" /> is a known
    /// <see cref="ICollection{T}" /> with fewer elements than <paramref name="items" />, the set is built from the
    /// source and query items are probed against it.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenSourceIsSmallerThanItems_ShouldReturnExpectedResult()
    {
        int[] source = [42, 99];
        int[] items = Enumerable.Range(0, 100).ToArray();

        Assert.IsTrue(source.ContainsAny(items));
    }

    /// <summary>
    /// Verifies that a custom case-insensitive comparer affects membership for string element sequences.
    /// </summary>
    [TestMethod]
    public void ContainsAny_WhenUsingCaseInsensitiveComparer_ShouldReturnTrue()
    {
        string[] source = ["Apple", "Banana"];
        string[] items = ["CHERRY", "BANANA"];

        Assert.IsTrue(source.ContainsAny(items, StringComparer.OrdinalIgnoreCase));
    }

}
