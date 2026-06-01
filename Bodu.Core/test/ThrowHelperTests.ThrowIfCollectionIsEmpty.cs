// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCollectionIsEmpty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> throws
    /// <see cref="ArgumentException" /> for an empty array.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenArrayIsEmpty_ShouldThrowExactly()
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

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> throws
    /// <see cref="ArgumentException" /> when the collection has no elements.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenCollectionIsEmpty_ShouldThrowExactly()
    {
        ICollection<int> collection = new List<int>();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfCollectionIsEmpty(collection);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> throws
    /// <see cref="ArgumentNullException" /> when the collection is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfCollectionIsEmpty_WhenCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfCollectionIsEmpty<int>(null!);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for non-empty collections.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfCollectionIsEmptyValidContractData))]
    public void ThrowIfCollectionIsEmpty_WhenCollectionIsAccepted_ShouldNotThrowAndReportNothing(string testName, ICollection<int> collection) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfCollectionIsEmpty(collection, nameof(collection)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionIsEmpty{T}" /> throws the expected exception
    /// type with <c>ParamName == "collection"</c> for null collections and empty collections/arrays.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfCollectionIsEmptyInvalidContractData))]
    public void ThrowIfCollectionIsEmpty_WhenCollectionIsRejected_ShouldThrowExpected(
        string testName, ICollection<int>? collection, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfCollectionIsEmpty(collection!, nameof(collection)), expectedExceptionType, expectedParamName);

    private static IEnumerable<object?[]> ThrowIfCollectionIsEmptyValidContractData()
    {
        yield return new object?[] { "single-element list", new List<int> { 1 } };
        yield return new object?[] { "multi-element list", new List<int> { 1, 2, 3 } };
    }

    private static IEnumerable<object?[]> ThrowIfCollectionIsEmptyInvalidContractData()
    {
        yield return new object?[] { "null collection → ArgumentNullException", null, typeof(ArgumentNullException), "collection" };
        yield return new object?[] { "empty List<int> → ArgumentException", new List<int>(), typeof(ArgumentException), "collection" };
        yield return new object?[] { "empty array (ICollection<int>) → ArgumentException", Array.Empty<int>(), typeof(ArgumentException), "collection" };
    }

}
