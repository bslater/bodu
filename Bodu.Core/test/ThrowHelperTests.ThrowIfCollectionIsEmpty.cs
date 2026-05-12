// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCollectionIsEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
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
