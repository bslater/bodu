// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Exceptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that the single-element members reject a <see langword="null" /> element with
    /// <see cref="ArgumentNullException" /> naming the <c>item</c> parameter.
    /// </summary>
    [TestMethod]
    public void Exceptions_SingleElementMembers_WhenItemIsNull_ShouldThrowForItem()
    {
        var set = new ConcurrentHashSet<string>();

        Assert.AreEqual("item", Assert.ThrowsExactly<ArgumentNullException>(() => set.Add(null!)).ParamName);
        Assert.AreEqual("item", Assert.ThrowsExactly<ArgumentNullException>(() => set.Remove(null!)).ParamName);
        Assert.AreEqual("item", Assert.ThrowsExactly<ArgumentNullException>(() => set.Contains(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that every mutating <see cref="ISet{T}" /> operation rejects a <see langword="null" /> argument with
    /// <see cref="ArgumentNullException" /> naming the <c>other</c> parameter.
    /// </summary>
    [TestMethod]
    public void Exceptions_SetMutations_WhenOtherIsNull_ShouldThrowForOther()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => set.UnionWith(null!)).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => set.IntersectWith(null!)).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => set.ExceptWith(null!)).ParamName);
        Assert.AreEqual(
            "other",
            Assert.ThrowsExactly<ArgumentNullException>(() => set.SymmetricExceptWith(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that every <see cref="ISet{T}" /> predicate rejects a <see langword="null" /> argument with
    /// <see cref="ArgumentNullException" /> naming the <c>other</c> parameter.
    /// </summary>
    [TestMethod]
    public void Exceptions_SetPredicates_WhenOtherIsNull_ShouldThrowForOther()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => set.IsSubsetOf(null!)).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => set.IsSupersetOf(null!)).ParamName);
        Assert.AreEqual(
            "other",
            Assert.ThrowsExactly<ArgumentNullException>(() => set.IsProperSubsetOf(null!)).ParamName);
        Assert.AreEqual(
            "other",
            Assert.ThrowsExactly<ArgumentNullException>(() => set.IsProperSupersetOf(null!)).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => set.Overlaps(null!)).ParamName);
        Assert.AreEqual("other", Assert.ThrowsExactly<ArgumentNullException>(() => set.SetEquals(null!)).ParamName);
    }

    /// <summary>
    /// Verifies that the constructors reject invalid arguments with the documented exception types and parameter
    /// names.
    /// </summary>
    [TestMethod]
    public void Exceptions_Constructors_WhenArgumentsInvalid_ShouldThrow()
    {
        Assert.AreEqual(
            "capacity",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ConcurrentHashSet<int>(-1)).ParamName);
        Assert.AreEqual(
            "capacity",
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new ConcurrentHashSet<int>(-1, null)).ParamName);
        Assert.AreEqual(
            "collection",
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new ConcurrentHashSet<int>((IEnumerable<int>)null!)).ParamName);
        Assert.AreEqual(
            "collection",
            Assert.ThrowsExactly<ArgumentNullException>(() => new ConcurrentHashSet<int>(null!, null)).ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.CopyTo" /> rejects invalid destinations with the documented
    /// exception types.
    /// </summary>
    [TestMethod]
    public void Exceptions_CopyTo_WhenDestinationInvalid_ShouldThrow()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        Assert.AreEqual("array", Assert.ThrowsExactly<ArgumentNullException>(() => set.CopyTo(null!, 0)).ParamName);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => set.CopyTo(new int[3], -1));
        Assert.ThrowsExactly<ArgumentException>(() => set.CopyTo(new int[2], 0));
        Assert.ThrowsExactly<ArgumentException>(() => set.CopyTo(new int[3], 2));
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.ICollection.SyncRoot" /> always throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Exceptions_SyncRoot_WhenAccessed_ShouldThrowNotSupportedException()
    {
        System.Collections.ICollection set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<NotSupportedException>(() => _ = set.SyncRoot);
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="System.Collections.ICollection.CopyTo" /> rejects null,
    /// out-of-range, multidimensional, and type-incompatible destinations.
    /// </summary>
    [TestMethod]
    public void Exceptions_NonGenericCopyTo_WhenDestinationInvalid_ShouldThrow()
    {
        System.Collections.ICollection set = new ConcurrentHashSet<int>([1, 2]);

        Assert.ThrowsExactly<ArgumentNullException>(() => set.CopyTo(null!, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => set.CopyTo(new int[2], -1));
        Assert.ThrowsExactly<ArgumentException>(() => set.CopyTo(new int[2, 2], 0));
        Assert.ThrowsExactly<ArgumentException>(() => set.CopyTo(new string[5], 0));
    }
}
