// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCollectionTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfCollectionTooSmall{T}" /> contract with explicit
    /// ParamName assertions: null collection → <see cref="ArgumentNullException" /> on "collection";
    /// collection shorter than the required minimum → <see cref="ArgumentException" /> on "collection";
    /// collection with at least the required count → pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard, or <see langword="null" />.</param>
    /// <param name="minimum">The required minimum count.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfCollectionTooSmallContractData), DynamicDataSourceType.Method)]
    public void ThrowIfCollectionTooSmall_WhenInvokedWithVariousCollections_ShouldFollowContract(
        string testName, ICollection<int>? collection, int minimum, string expectedExceptionTypeName, string expectedParamName)
    {
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        string? param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfCollectionTooSmall(collection!, minimum, "collection"),
            expected,
            param);
    }

    private static IEnumerable<object?[]> ThrowIfCollectionTooSmallContractData()
    {
        yield return new object?[] { "null collection → ANE on collection", null, 1, "ArgumentNullException", "collection" };
        yield return new object?[] { "empty vs minimum 1 → AE on collection", new List<int>(), 1, "ArgumentException", "collection" };
        yield return new object?[] { "one element vs minimum 2 → AE on collection", new List<int> { 1 }, 2, "ArgumentException", "collection" };
        yield return new object?[] { "two-element array vs minimum 3 → AE on collection", (ICollection<int>)new int[] { 1, 2 }, 3, "ArgumentException", "collection" };
        yield return new object?[] { "exactly minimum → pass", new List<int> { 1 }, 1, "", "" };
        yield return new object?[] { "more than minimum → pass", (ICollection<int>)new int[] { 1, 2, 3 }, 2, "", "" };
        yield return new object?[] { "empty with minimum 0 → pass", new List<int>(), 0, "", "" };
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionTooSmall" />, when CollectionTooSmall, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetTooSmallCollectionTestData))]
    public void ThrowIfCollectionTooSmall_WhenCollectionTooSmall_ShouldThrowArgumentException(ICollection<int> collection, int minimumCount)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfCollectionTooSmall<int>(collection, minimumCount);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionTooSmall" />, when CollectionIsSufficient, NotThrow.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetSufficientCollectionTestData))]
    public void ThrowIfCollectionTooSmall_WhenCollectionIsSufficient_ShouldNotThrow(ICollection<int> collection, int minimumCount) => ThrowHelper.ThrowIfCollectionTooSmall<int>(collection, minimumCount);

    private static IEnumerable<object[]> GetTooSmallCollectionTestData()
    {
        yield return new object[] { new List<int>(), 1 };
        yield return new object[] { new List<int> { 1 }, 2 };
        yield return new object[] { new int[] { 1, 2 }, 3 };
    }

    private static IEnumerable<object[]> GetSufficientCollectionTestData()
    {
        yield return new object[] { new List<int> { 1 }, 1 };
        yield return new object[] { new List<int> { 1, 2 }, 2 };
        yield return new object[] { new int[] { 1, 2, 3 }, 2 };
        yield return new object[] { new int[] { }, 0 };
    }
}
