// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NavigableSetTests.Nulls.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

public partial class NavigableSetTests
{
    /// <summary>
    /// Verifies the family-wide null policy: every element-taking member rejects <see langword="null" /> with
    /// <see cref="ArgumentNullException" />, diverging deliberately from <see cref="SortedSet{T}" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenElementTakingMembersReceiveNull_ShouldThrowArgumentNullException()
    {
        var sut = new NavigableSet<string>();
        sut.Add("a");

        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Add(null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Remove(null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Contains(null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.IndexOf(null!); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetFloor(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetCeiling(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetHigher(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.TryGetLower(null!, out _); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.CountInRange(null!, "z"); });
        Assert.ThrowsExactly<ArgumentNullException>(() => { _ = sut.Range(null!, "z"); });
    }

    /// <summary>
    /// Verifies that a rejected <see langword="null" /> leaves the set contents untouched.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenNullRejected_ShouldLeaveSetUnchanged()
    {
        var sut = new NavigableSet<string>();
        sut.Add("a");
        sut.Add("b");

        Assert.ThrowsExactly<ArgumentNullException>(() => { sut.Add(null!); });

        Assert.AreEqual(2, sut.Count);
        CollectionAssert.AreEqual(new[] { "a", "b" }, sut.ToList());
    }

    /// <summary>
    /// Verifies that the bulk-load constructor rejects a source containing <see langword="null" /> with
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Nulls_WhenBulkSourceContainsNull_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new NavigableSet<string>(new[] { "a", null!, "b" });
        });
    }
}
