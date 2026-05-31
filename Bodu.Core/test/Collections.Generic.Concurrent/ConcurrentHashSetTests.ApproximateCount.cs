// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.ApproximateCount.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ApproximateCount"/> returns zero for a freshly constructed
    /// set, matching the strict <see cref="ConcurrentHashSet{T}.Count"/> in the absence of concurrent mutation.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenSetIsEmpty_ShouldReturnZero()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.AreEqual(0, set.ApproximateCount);
        Assert.AreEqual(set.Count, set.ApproximateCount);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ApproximateCount"/> agrees exactly with
    /// <see cref="ConcurrentHashSet{T}.Count"/> in the absence of concurrent mutation, for a range of populations
    /// including the post-grow region.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(64)]
    [DataRow(256)]
    [DataRow(1024)]
    public void ApproximateCount_WhenNoConcurrentMutation_ShouldEqualStrictCount(int elementCount)
    {
        var set = new ConcurrentHashSet<int>();

        for (var i = 0; i < elementCount; i++)
            set.Add(i);

        Assert.AreEqual(elementCount, set.ApproximateCount);
        Assert.AreEqual(set.Count, set.ApproximateCount);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ApproximateCount"/> decreases as elements are removed and
    /// continues to agree with <see cref="ConcurrentHashSet{T}.Count"/> in the absence of concurrent mutation.
    /// </summary>
    [TestMethod]
    public void ApproximateCount_WhenElementsRemoved_ShouldTrackStrictCount()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 50));

        Assert.AreEqual(50, set.ApproximateCount);

        for (var i = 0; i < 25; i++)
            set.Remove(i);

        Assert.AreEqual(25, set.ApproximateCount);
        Assert.AreEqual(set.Count, set.ApproximateCount);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ApproximateCount"/> stays within the inclusive range
    /// <c>[0, expected]</c> under concurrent writers. Exact agreement with <see cref="ConcurrentHashSet{T}.Count"/>
    /// is not asserted because the approximate read deliberately does not lock.
    /// </summary>
    [TestMethod]
    [TestCategory("Stress")]
    public void ApproximateCount_WhenAccessedDuringConcurrentAdds_ShouldStayWithinSeenRange()
    {
        var set = new ConcurrentHashSet<int>();
        const int writers = 4;
        const int perWriter = 5000;

        var observed = new List<int>();
        var done = false;

        var reader = Task.Run(() =>
        {
            while (!Volatile.Read(ref done))
            {
                observed.Add(set.ApproximateCount);
            }
        });

        Parallel.For(0, writers, w =>
        {
            for (var i = 0; i < perWriter; i++)
                set.Add((w * perWriter) + i);
        });

        Volatile.Write(ref done, true);
        reader.Wait();

        var max = writers * perWriter;

        foreach (var value in observed)
        {
            Assert.IsTrue(value >= 0 && value <= max, $"ApproximateCount {value} outside expected range [0, {max}].");
        }

        Assert.AreEqual(max, set.ApproximateCount);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsEmptyApproximate"/> reports <see langword="true"/> for a
    /// fresh set and <see langword="false"/> after a single Add.
    /// </summary>
    [TestMethod]
    public void IsEmptyApproximate_WhenSetIsEmpty_ShouldReturnTrue()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.IsEmptyApproximate);

        set.Add(1);
        Assert.IsFalse(set.IsEmptyApproximate);
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.IsEmptyApproximate"/> agrees with
    /// <see cref="ConcurrentHashSet{T}.IsEmpty"/> after the set transitions back to empty via Remove and Clear.
    /// </summary>
    [TestMethod]
    public void IsEmptyApproximate_WhenSetEmptiedByRemoveOrClear_ShouldReturnTrue()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        Assert.IsFalse(set.IsEmptyApproximate);

        set.Remove(1);
        set.Remove(2);
        set.Remove(3);

        Assert.IsTrue(set.IsEmptyApproximate);
        Assert.AreEqual(set.IsEmpty, set.IsEmptyApproximate);

        set.Add(99);
        set.Clear();

        Assert.IsTrue(set.IsEmptyApproximate);
        Assert.AreEqual(set.IsEmpty, set.IsEmptyApproximate);
    }
}
