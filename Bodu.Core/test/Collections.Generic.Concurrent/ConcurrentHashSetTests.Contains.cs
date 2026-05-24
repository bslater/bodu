// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Contains.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Contains" /> returns <see langword="true" /> for an element that
    /// is present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementPresent_ShouldReturnTrue()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.IsTrue(set.Contains(2));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Contains" /> returns <see langword="false" /> for an element that
    /// is not present.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementAbsent_ShouldReturnFalse()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });

        Assert.IsFalse(set.Contains(99));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Contains" /> returns <see langword="false" /> on an empty set.
    /// </summary>
    [TestMethod]
    public void Contains_WhenSetIsEmpty_ShouldReturnFalse()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsFalse(set.Contains(1));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Contains" /> returns <see langword="false" /> after the element
    /// has been removed.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementRemoved_ShouldReturnFalse()
    {
        var set = new ConcurrentHashSet<int>(new[] { 5 });
        set.Remove(5);

        Assert.IsFalse(set.Contains(5));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Contains" /> uses reference identity when the element type has no
    /// equality override and the default comparer is used.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDefaultComparerAndReferenceType_ShouldUseReferenceIdentity()
    {
        var present = new ReferenceItem(1);
        var absent = new ReferenceItem(1);
        var set = new ConcurrentHashSet<ReferenceItem>();
        set.Add(present);

        Assert.IsTrue(set.Contains(present));
        Assert.IsFalse(set.Contains(absent));
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Contains" /> uses value equality for a reference type that
    /// overrides <see cref="object.Equals(object)" />.
    /// </summary>
    [TestMethod]
    public void Contains_WhenElementTypeHasValueEquality_ShouldMatchByValue()
    {
        var set = new ConcurrentHashSet<TestItem>();
        set.Add(new TestItem(7));

        Assert.IsTrue(set.Contains(new TestItem(7)));
        Assert.IsFalse(set.Contains(new TestItem(8)));
    }
}
