// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCollectionTooSmall.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionTooSmall" />, when CollectionIsSufficient, NotThrow.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetSufficientCollectionTestData))]
    public void ThrowIfCollectionTooSmall_WhenCollectionIsSufficient_ShouldNotThrow(ICollection<int> collection, int minimumCount) => ThrowHelper.ThrowIfCollectionTooSmall<int>(collection, minimumCount);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionTooSmall" />, when CollectionTooSmall, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetTooSmallCollectionTestData))]
    public void ThrowIfCollectionTooSmall_WhenCollectionTooSmall_ShouldThrowExactly(ICollection<int> collection, int minimumCount)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfCollectionTooSmall<int>(collection, minimumCount);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionTooSmall{T}" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for collections that meet or exceed the required
    /// minimum count.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard.</param>
    /// <param name="minimum">The required minimum count.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfCollectionTooSmallValidContractData))]
    public void ThrowIfCollectionTooSmall_WhenCollectionIsAccepted_ShouldNotThrowAndReportNothing(
        string testName, ICollection<int> collection, int minimum) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfCollectionTooSmall(collection, minimum, nameof(collection)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCollectionTooSmall{T}" /> throws the expected exception
    /// type with <c>ParamName == "collection"</c> for null collections and collections shorter than the
    /// required minimum.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="collection">The collection passed to the guard, or <see langword="null" />.</param>
    /// <param name="minimum">The required minimum count.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfCollectionTooSmallInvalidContractData))]
    public void ThrowIfCollectionTooSmall_WhenCollectionIsRejected_ShouldThrowOnCollection(
        string testName, ICollection<int>? collection, int minimum, string expectedExceptionTypeName)
    {
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfCollectionTooSmall(collection!, minimum, nameof(collection)),
            expected,
            "collection");
    }

    private static IEnumerable<object[]> GetSufficientCollectionTestData()
    {
        yield return new object[] { new List<int> { 1 }, 1 };
        yield return new object[] { new List<int> { 1, 2 }, 2 };
        yield return new object[] { new int[] { 1, 2, 3 }, 2 };
        yield return new object[] { Array.Empty<int>(), 0 };
    }

    private static IEnumerable<object[]> GetTooSmallCollectionTestData()
    {
        yield return new object[] { new List<int>(), 1 };
        yield return new object[] { new List<int> { 1 }, 2 };
        yield return new object[] { new int[] { 1, 2 }, 3 };
    }

    private static IEnumerable<object?[]> ThrowIfCollectionTooSmallValidContractData()
    {
        yield return new object?[] { "exactly minimum", new List<int> { 1 }, 1 };
        yield return new object?[] { "more than minimum", (ICollection<int>)[1, 2, 3], 2 };
        yield return new object?[] { "empty with minimum 0", new List<int>(), 0 };
    }

    private static IEnumerable<object?[]> ThrowIfCollectionTooSmallInvalidContractData()
    {
        yield return new object?[] { "null collection → ANE", null, 1, "ArgumentNullException" };
        yield return new object?[] { "empty vs minimum 1 → AE", new List<int>(), 1, "ArgumentException" };
        yield return new object?[] { "one element vs minimum 2 → AE", new List<int> { 1 }, 2, "ArgumentException" };
        yield return new object?[] { "two-element array vs minimum 3 → AE", (ICollection<int>)[1, 2], 3, "ArgumentException" };
    }

}
