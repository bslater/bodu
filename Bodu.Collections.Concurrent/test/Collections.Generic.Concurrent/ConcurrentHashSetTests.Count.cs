// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.Count.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> is zero for a newly created set.
    /// </summary>
    [TestMethod]
    public void Count_WhenSetIsEmpty_ShouldBeZero()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> reflects each distinct element added.
    /// </summary>
    [TestMethod]
    public void Count_WhenElementsAdded_ShouldReflectDistinctElementCount()
    {
        var set = new ConcurrentHashSet<int>();

        set.Add(1);
        set.Add(2);
        set.Add(2);
        set.Add(3);

        Assert.AreEqual(3, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> decreases as elements are removed.
    /// </summary>
    [TestMethod]
    public void Count_WhenElementsRemoved_ShouldDecrease()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        set.Remove(2);

        Assert.AreEqual(2, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> agrees with the length of the
    /// <see cref="ConcurrentHashSet{T}.ToArray" /> snapshot after the table has grown.
    /// </summary>
    [TestMethod]
    public void Count_WhenTableHasGrown_ShouldAgreeWithSnapshotLength()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 2000));

        Assert.AreEqual(set.ToArray().Length, set.Count);
        Assert.AreEqual(2000, set.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.Count" /> stays within the inclusive range <c>[0, expected]</c>
    /// under concurrent writers, and settles on the exact total once the writers finish. Intermediate exact agreement
    /// is not asserted because the lock-free counter read may transiently lag in-flight mutations.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void Count_WhenAccessedDuringConcurrentAdds_ShouldStayWithinSeenRange()
    {
        var set = new ConcurrentHashSet<int>();
        const int writers = 4;
        const int perWriter = 5000;

        var observed = new List<int>();
        bool done = false;

        var reader = Task.Run(() =>
        {
            while (!Volatile.Read(ref done))
            {
                observed.Add(set.Count);
            }
        });

        Parallel.For(0, writers, w =>
        {
            for (int i = 0; i < perWriter; i++)
                set.Add((w * perWriter) + i);
        });

        Volatile.Write(ref done, true);
        reader.Wait();

        int max = writers * perWriter;

        foreach (int value in observed)
        {
            Assert.IsTrue(value >= 0 && value <= max, $"Count {value} outside expected range [0, {max}].");
        }

        Assert.AreEqual(max, set.Count);
    }
}
