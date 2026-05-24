// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.IsEmpty.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsEmpty" /> is <see langword="true" /> for a newly created set.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenSetIsNew_ShouldBeTrue()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsEmpty" /> is <see langword="false" /> once an element is added.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenSetHasElements_ShouldBeFalse()
    {
        var set = new ConcurrentHashSet<int>();
        set.Add(1);

        Assert.IsFalse(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsEmpty" /> returns <see langword="true" /> again after the last
    /// element is removed.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenLastElementRemoved_ShouldBeTrue()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1 });
        set.Remove(1);

        Assert.IsTrue(set.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsEmpty" /> returns <see langword="true" /> after the set is
    /// cleared.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenSetCleared_ShouldBeTrue()
    {
        var set = new ConcurrentHashSet<int>(new[] { 1, 2, 3 });
        set.Clear();

        Assert.IsTrue(set.IsEmpty);
    }
}
