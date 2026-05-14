// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCollectionIsEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> contract matrix with explicit
    /// ParamName assertions: null collection → <see cref="ArgumentNullException" />, empty collection /
    /// empty array → <see cref="ArgumentException" />, non-empty collection → no throw.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw, or <see langword="null" />.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfCollectionIsEmptyContractData))]
    public void ThrowIfCollectionIsEmpty_WhenInvokedWithVariousCollections_ShouldFollowContract(
        string testName, ICollection<int>? collection, Type? expectedExceptionType, string? expectedParamName)
    {
        AssertGuard(testName, () =>
        {
            ThrowHelper.ThrowIfCollectionIsEmpty(collection!, nameof(collection));
        }, expectedExceptionType, expectedParamName);
    }

    private static IEnumerable<object?[]> ThrowIfCollectionIsEmptyContractData()
    {
        yield return new object?[] { "null collection → ArgumentNullException", null, typeof(ArgumentNullException), "collection" };
        yield return new object?[] { "empty List<int> → ArgumentException", new List<int>(), typeof(ArgumentException), "collection" };
        yield return new object?[] { "empty array (ICollection<int>) → ArgumentException", Array.Empty<int>(), typeof(ArgumentException), "collection" };
        yield return new object?[] { "single-element list → no throw", new List<int> { 1 }, null, null };
        yield return new object?[] { "multi-element list → no throw", new List<int> { 1, 2, 3 }, null, null };
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> throws
    /// <see cref="ArgumentNullException" /> when the collection is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenCollectionIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfCollectionIsEmpty<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> throws
    /// <see cref="ArgumentException" /> when the collection has no elements.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenCollectionIsEmpty_ShouldThrowArgumentException()
    {
        ICollection<int> collection = new List<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfCollectionIsEmpty(collection);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> throws
    /// <see cref="ArgumentException" /> for an empty array.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenArrayIsEmpty_ShouldThrowArgumentException()
    {
        ICollection<int> collection = Array.Empty<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfCollectionIsEmpty(collection);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> does not throw when the
    /// collection contains at least one element.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenCollectionHasElements_ShouldNotThrow()
    {
        ICollection<int> collection = new List<int> { 1 };

        ThrowHelper.ThrowIfCollectionIsEmpty(collection);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> does not throw when the
    /// collection contains multiple elements.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenCollectionHasMultipleElements_ShouldNotThrow()
    {
        ICollection<int> collection = [1, 2, 3];

        ThrowHelper.ThrowIfCollectionIsEmpty(collection);
    }
}
