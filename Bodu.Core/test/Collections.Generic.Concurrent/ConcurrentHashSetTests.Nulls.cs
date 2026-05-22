// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Nulls.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Add" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void Add_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.Add(null!);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Remove" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void Remove_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.Remove(null!);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Contains" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> element.
    /// </summary>
    [TestMethod]
    public void Contains_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        var set = new ConcurrentHashSet<string>();

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.Contains(null!);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the <see cref="ICollection{T}.Add(T)" /> explicit implementation rejects a <see langword="null" />
    /// element.
    /// </summary>
    [TestMethod]
    public void ICollectionAdd_WhenItemIsNull_ShouldThrowArgumentNullException()
    {
        ICollection<string> set = new ConcurrentHashSet<string>();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.Add(null!);
        });
    }
}
