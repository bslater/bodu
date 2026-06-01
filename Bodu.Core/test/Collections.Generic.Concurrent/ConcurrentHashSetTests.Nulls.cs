// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
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

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.UnionWith" /> rejects a <see langword="null" /> element inside
    /// <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.UnionWith(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> rejects a <see langword="null" /> element inside
    /// <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.IntersectWith(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> rejects a <see langword="null" /> element inside
    /// <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.ExceptWith(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> rejects a <see langword="null" /> element
    /// inside <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.SymmetricExceptWith(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsSubsetOf" /> rejects a <see langword="null" /> element inside
    /// <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>(["x"]);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.IsSubsetOf(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsSubsetOf" /> validates <c>other</c> even when the set is empty,
    /// so a <see langword="null" /> element is rejected rather than skipped by the empty-set shortcut.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenSetIsEmptyAndOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.IsSubsetOf(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsSupersetOf" /> rejects a <see langword="null" /> element inside
    /// <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>(["a"]);

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.IsSupersetOf(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSubsetOf" /> rejects a <see langword="null" /> element
    /// inside <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.IsProperSubsetOf(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSupersetOf" /> rejects a <see langword="null" /> element
    /// inside <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.IsProperSupersetOf(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Overlaps" /> rejects a <see langword="null" /> element inside
    /// <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.Overlaps(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SetEquals" /> rejects a <see langword="null" /> element inside
    /// <c>other</c> with <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenOtherContainsNull_ShouldThrowArgumentNullExceptionForItem()
    {
        var set = new ConcurrentHashSet<string>();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            set.SetEquals(["a", null!, "b"]);
        });

        Assert.AreEqual("item", ex.ParamName);
    }
}
