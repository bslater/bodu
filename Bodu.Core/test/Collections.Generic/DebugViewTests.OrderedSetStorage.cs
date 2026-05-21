// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DebugViewTests.OrderedSetStorage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public sealed class DebugViewTests_OrderedSetStorage
{

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorageDebugView{T}" /> constructed from an <see cref="IndexedSet{T}" />
    /// exposes the set's items via its <c>Items</c> property.
    /// </summary>
    [TestMethod]
    public void OrderedSetStorageDebugView_WhenConstructedFromIndexedSet_ShouldExposeItems()
    {
        var set = new IndexedSet<string>(["a", "b", "c"]);

        var view = new OrderedSetStorageDebugView<string>(set);

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, view.Items);
    }
    /// <summary>
    /// Verifies that <see cref="OrderedSetStorageDebugView{T}" /> constructed from an <see cref="OrderedSet{T}" />
    /// exposes the set's items via its <c>Items</c> property.
    /// </summary>
    [TestMethod]
    public void OrderedSetStorageDebugView_WhenConstructedFromOrderedSet_ShouldExposeItems()
    {
        var set = new OrderedSet<int>([10, 20, 30]);

        var view = new OrderedSetStorageDebugView<int>(set);

        CollectionAssert.AreEqual(new[] { 10, 20, 30 }, view.Items);
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorageDebugView{T}" /> rejects a <see langword="null" />
    /// <see cref="IndexedSet{T}" />.
    /// </summary>
    [TestMethod]
    public void OrderedSetStorageDebugView_WhenIndexedSetIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new OrderedSetStorageDebugView<int>((IndexedSet<int>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="OrderedSetStorageDebugView{T}" /> rejects a <see langword="null" />
    /// <see cref="OrderedSet{T}" />.
    /// </summary>
    [TestMethod]
    public void OrderedSetStorageDebugView_WhenOrderedSetIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new OrderedSetStorageDebugView<int>((OrderedSet<int>)null!);
        });
    }

}
