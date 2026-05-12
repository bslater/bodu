// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ContainsAll.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu.Collections.Generic.Extensions;

[TestClass]
public sealed partial class IEnumerableExtensionsTests_ContainsAll
{
    /// <summary>
    /// Provides scenarios for <c>ContainsAll</c> covering full-membership, partial-membership,
    /// empty-items, empty-source, and duplicate-handling cases.
    /// </summary>
    public static IEnumerable<object[]> GetContainsAllTestCases() =>
    [
        new object[] { new[] { 1, 2, 3, 4, 5 }, new[] { 2, 4 }, true },
        [new[] { 1, 2, 3, 4, 5 }, new[] { 1, 2, 3, 4, 5 }, true],
        [new[] { 1, 2, 3, 4, 5 }, new[] { 6 }, false],
        [new[] { 1, 2, 3, 4, 5 }, new[] { 5, 6 }, false],
        [new[] { 1, 2, 3, 4, 5 }, Array.Empty<int>(), true],
        [Array.Empty<int>(), new[] { 1 }, false],
        [Array.Empty<int>(), Array.Empty<int>(), true],
        [new[] { 1, 2, 3 }, new[] { 2, 2, 2 }, true],
    ];

    /// <summary>
    /// Verifies that <c>ContainsAll</c> correctly determines whether every item in the query
    /// sequence is present in the source sequence across full, partial, empty, and duplicate cases.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetContainsAllTestCases))]
    public void ContainsAll_WhenEvaluatingIntegerSequences_ShouldReturnExpectedResult(
        int[] source,
        int[] items,
        bool expected) => Assert.AreEqual(expected, source.ContainsAll(items));

    /// <summary>
    /// Verifies that a custom equality comparer (case-insensitive) is used to compare string elements.
    /// </summary>
    [TestMethod]
    public void ContainsAll_WhenUsingCaseInsensitiveComparer_ShouldReturnTrue()
    {
        string[] source = ["Apple", "Banana", "Cherry"];
        string[] items = ["APPLE", "banana"];

        Assert.IsTrue(source.ContainsAll(items, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that <c>ContainsAll</c> honours the default comparer when no custom comparer is supplied.
    /// </summary>
    [TestMethod]
    public void ContainsAll_WhenUsingDefaultStringComparer_ShouldDistinguishCase()
    {
        string[] source = ["Apple", "Banana", "Cherry"];
        string[] items = ["apple"];

        Assert.IsFalse(source.ContainsAll(items));
    }

    /// <summary>
    /// Verifies that a null source sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ContainsAll_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        IEnumerable<int>? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.ContainsAll([1]);
        });
    }

    /// <summary>
    /// Verifies that a null items sequence throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ContainsAll_WhenItemsIsNull_ShouldThrowArgumentNullException()
    {
        int[] source = [1, 2, 3];
        IEnumerable<int>? items = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source.ContainsAll(items!);
        });
    }
}
