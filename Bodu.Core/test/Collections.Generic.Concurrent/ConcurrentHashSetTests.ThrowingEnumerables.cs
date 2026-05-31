// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.ThrowingEnumerables.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Returns a lazy sequence that yields <paramref name="yieldCount" /> ascending integers and then throws an
    /// <see cref="InvalidOperationException" />, simulating a source collection that faults mid-enumeration.
    /// </summary>
    private static IEnumerable<int> EnumerableThatThrowsAfter(int yieldCount)
    {
        for (var i = 0; i < yieldCount; i++)
            yield return i;

        throw new InvalidOperationException("Enumerable fault.");
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.UnionWith" /> surfaces an exception thrown while enumerating
    /// <c>other</c>.
    /// </summary>
    [TestMethod]
    public void UnionWith_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.UnionWith(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IntersectWith" /> surfaces an exception thrown while enumerating
    /// <c>other</c>.
    /// </summary>
    [TestMethod]
    public void IntersectWith_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.IntersectWith(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ExceptWith" /> surfaces an exception thrown while enumerating
    /// <c>other</c>.
    /// </summary>
    [TestMethod]
    public void ExceptWith_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.ExceptWith(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SymmetricExceptWith" /> surfaces an exception thrown while
    /// enumerating <c>other</c>.
    /// </summary>
    [TestMethod]
    public void SymmetricExceptWith_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.SymmetricExceptWith(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsSubsetOf" /> surfaces an exception thrown while enumerating
    /// <c>other</c>.
    /// </summary>
    [TestMethod]
    public void IsSubsetOf_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>([1]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.IsSubsetOf(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsSupersetOf" /> surfaces an exception thrown while enumerating
    /// <c>other</c>.
    /// </summary>
    [TestMethod]
    public void IsSupersetOf_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>([0, 1]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.IsSupersetOf(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSubsetOf" /> surfaces an exception thrown while
    /// enumerating <c>other</c>.
    /// </summary>
    [TestMethod]
    public void IsProperSubsetOf_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>([1]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.IsProperSubsetOf(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsProperSupersetOf" /> surfaces an exception thrown while
    /// enumerating <c>other</c>.
    /// </summary>
    [TestMethod]
    public void IsProperSupersetOf_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>([1]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.IsProperSupersetOf(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Overlaps" /> surfaces an exception thrown while enumerating
    /// <c>other</c>.
    /// </summary>
    [TestMethod]
    public void Overlaps_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.Overlaps(EnumerableThatThrowsAfter(2));
        });
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.SetEquals" /> surfaces an exception thrown while enumerating
    /// <c>other</c>.
    /// </summary>
    [TestMethod]
    public void SetEquals_WhenOtherThrowsDuringEnumeration_ShouldPropagateException()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            set.SetEquals(EnumerableThatThrowsAfter(2));
        });
    }
}
