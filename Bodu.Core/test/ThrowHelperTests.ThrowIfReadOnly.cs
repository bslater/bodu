// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfReadOnly.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> throws <see cref="ArgumentException" />
    /// when an array is passed, since arrays expose a read-only <c>ICollection&lt;T&gt;</c> view.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsArray_ShouldThrowExactly()
    {
        int[] array = [1, 2, 3];
        ICollection<int> collection = array;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfReadOnly(collection);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> does not throw when the collection is
    /// an empty writable list.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsEmptyList_ShouldNotThrow()
    {
        ICollection<int> collection = new List<int>();

        ThrowHelper.ThrowIfReadOnly(collection);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> throws <see cref="ArgumentNullException" />
    /// when the collection is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfReadOnly<int>(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> throws <see cref="ArgumentException" />
    /// when the collection's <c>IsReadOnly</c> property is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsReadOnly_ShouldThrowExactly()
    {
        ICollection<int> collection = new ReadOnlyCollection<int>([1, 2, 3]);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfReadOnly(collection);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> does not throw when the collection is
    /// writable.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsWritable_ShouldNotThrow()
    {
        ICollection<int> collection = new List<int> { 1, 2, 3 };

        ThrowHelper.ThrowIfReadOnly(collection);
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for writable collections.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfReadOnlyValidContractData))]
    public void ThrowIfReadOnly_WhenCollectionIsAccepted_ShouldNotThrowAndReportNothing(string testName, ICollection<int> collection) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfReadOnly(collection, nameof(collection)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> throws the expected exception type with
    /// <c>ParamName == "collection"</c> for null collections, read-only collections, and arrays (whose
    /// <c>ICollection&lt;T&gt;</c> view is read-only).
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfReadOnlyInvalidContractData))]
    public void ThrowIfReadOnly_WhenCollectionIsRejected_ShouldThrowExpected(
        string testName, ICollection<int>? collection, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfReadOnly(collection!, nameof(collection)), expectedExceptionType, expectedParamName);

    private static IEnumerable<object?[]> ThrowIfReadOnlyValidContractData()
    {
        yield return new object?[] { "writable List<int>", new List<int> { 1, 2, 3 } };
        yield return new object?[] { "empty writable List<int>", new List<int>() };
    }

    private static IEnumerable<object?[]> ThrowIfReadOnlyInvalidContractData()
    {
        yield return new object?[] { "null → ArgumentNullException", null, typeof(ArgumentNullException), "collection" };
        yield return new object?[] { "ReadOnlyCollection → ArgumentException", new ReadOnlyCollection<int>([1, 2]), typeof(ArgumentException), "collection" };
        yield return new object?[] { "array (read-only ICollection view) → ArgumentException", new[] { 1, 2, 3 }, typeof(ArgumentException), "collection" };
    }

}
