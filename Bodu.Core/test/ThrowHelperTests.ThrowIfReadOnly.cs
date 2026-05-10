// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfReadOnly.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> throws <see cref="ArgumentNullException" />
    /// when the collection is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsNull_ShouldThrowArgumentNullException()
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
    public void ThrowIfReadOnly_WhenCollectionIsReadOnly_ShouldThrowArgumentException()
    {
        ICollection<int> collection = new ReadOnlyCollection<int>(new[] { 1, 2, 3 });

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfReadOnly(collection);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> throws <see cref="ArgumentException" />
    /// when an array is passed, since arrays expose a read-only <c>ICollection&lt;T&gt;</c> view.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsArray_ShouldThrowArgumentException()
    {
        ICollection<int> array = new int[] { 1, 2, 3 };

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfReadOnly(array);
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
    /// Verifies that <see cref="ThrowHelper.ThrowIfReadOnly{T}" /> does not throw when the collection is
    /// an empty writable list.
    /// </summary>
    [TestMethod]
    public void ThrowIfReadOnly_WhenCollectionIsEmptyList_ShouldNotThrow()
    {
        ICollection<int> collection = new List<int>();

        ThrowHelper.ThrowIfReadOnly(collection);
    }
}
