// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Comparer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Comparer" /> returns the default comparer when none was supplied.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenNoComparerSupplied_ShouldReturnDefaultComparer()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.AreSame(EqualityComparer<int>.Default, set.Comparer);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Comparer" /> returns the exact comparer instance supplied at
    /// construction.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCustomComparerSupplied_ShouldReturnThatInstance()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.Ordinal);

        Assert.AreSame(StringComparer.Ordinal, set.Comparer);
    }

    /// <summary>
    /// Verifies that a custom comparer governs membership testing across <see cref="ConcurrentHashSet{T}.Contains" />
    /// and <see cref="ConcurrentHashSet{T}.Remove" />.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCustomComparerUsed_ShouldGovernContainsAndRemove()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Key");

        Assert.IsTrue(set.Contains("KEY"));
        Assert.IsTrue(set.Contains("key"));
        Assert.IsTrue(set.Remove("kEy"));
        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Verifies that a custom comparer governs duplicate detection so that equal elements collapse to a single entry.
    /// </summary>
    [TestMethod]
    public void Comparer_WhenCustomComparerUsed_ShouldGovernDuplicateDetection()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

        set.Add("one");
        set.Add("ONE");
        set.Add("One");

        Assert.AreEqual(1, set.Count);
    }
}
