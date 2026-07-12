// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentHashSetTests.SplitOrder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

public partial class ConcurrentHashSetTests
{
    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ReverseBits" /> maps known values to their exact bit-reversed
    /// counterparts.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U, 0x00000000U)]
    [DataRow(0x00000001U, 0x80000000U)]
    [DataRow(0x80000000U, 0x00000001U)]
    [DataRow(0xFFFFFFFFU, 0xFFFFFFFFU)]
    [DataRow(0x00000002U, 0x40000000U)]
    [DataRow(0x0000000FU, 0xF0000000U)]
    [DataRow(0x12345678U, 0x1E6A2C48U)]
    [DataRow(0xAAAAAAAAU, 0x55555555U)]
    [DataRow(0x0F0F0F0FU, 0xF0F0F0F0U)]
    public void ReverseBits_WhenKnownValue_ShouldReturnExactReversal(uint value, uint expected) =>
        Assert.AreEqual(expected, ConcurrentHashSet<int>.ReverseBits(value));

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.ReverseBits" /> is an involution — reversing twice returns the
    /// original value — across a scripted sweep of bit patterns.
    /// </summary>
    [TestMethod]
    public void ReverseBits_WhenAppliedTwice_ShouldReturnOriginalValue()
    {
        for (int bit = 0; bit < 32; bit++)
        {
            uint value = 1U << bit;
            Assert.AreEqual(value, ConcurrentHashSet<int>.ReverseBits(ConcurrentHashSet<int>.ReverseBits(value)));
        }

        for (uint value = 0; value < 4096; value++)
        {
            uint scrambled = (value * 2654435761U) ^ (value << 13);
            Assert.AreEqual(
                scrambled, ConcurrentHashSet<int>.ReverseBits(ConcurrentHashSet<int>.ReverseBits(scrambled)));
        }
    }

    /// <summary>
    /// Verifies that <see cref="ConcurrentHashSet{T}.MakeDataKey" /> always produces an odd key and
    /// <see cref="ConcurrentHashSet{T}.MakeSentinelKey" /> always produces an even key, so the low bit cleanly
    /// discriminates data nodes from bucket sentinels.
    /// </summary>
    [TestMethod]
    public void MakeKeys_ForAnyInput_ShouldDiscriminateDataFromSentinelByLowBit()
    {
        int[] samples = [0, 1, 2, 17, 255, 4096, -1, -12345, int.MinValue, int.MaxValue];

        foreach (int value in samples)
        {
            Assert.AreEqual(1UL, ConcurrentHashSet<int>.MakeDataKey(value) & 1UL, $"Data key for {value} must be odd.");
        }

        foreach (int bucket in new[] { 0, 1, 2, 3, 15, 16, 255, 1 << 29 })
        {
            Assert.AreEqual(
                0UL, ConcurrentHashSet<int>.MakeSentinelKey(bucket) & 1UL, $"Sentinel key for {bucket} must be even.");
        }
    }

    /// <summary>
    /// Verifies that a bucket's sentinel key is a strict lower bound of the data key of every hash code that maps
    /// into that bucket, for several table sizes — the ordering invariant that makes each bucket a contiguous run of
    /// the split-ordered list.
    /// </summary>
    [TestMethod]
    public void MakeKeys_ForHashesInBucket_ShouldOrderSentinelBeforeEveryMember()
    {
        foreach (int bucketCount in new[] { 8, 16, 64, 1024 })
        {
            for (int i = 0; i < 2048; i++)
            {
                int hash = unchecked((i * 486187739) ^ (i << 7));
                int bucket = hash & (bucketCount - 1);

                Assert.IsTrue(
                    ConcurrentHashSet<int>.MakeSentinelKey(bucket) < ConcurrentHashSet<int>.MakeDataKey(hash),
                    $"Sentinel of bucket {bucket} (size {bucketCount}) must precede data key of hash {hash}.");
            }
        }
    }

    /// <summary>
    /// Verifies that every bucket's parent (the index with its most significant set bit cleared) has a sentinel key
    /// strictly below the child's, so recursive bucket initialization always inserts forward from the parent.
    /// </summary>
    [TestMethod]
    public void MakeSentinelKey_ForEveryBucket_ShouldOrderParentBeforeChild()
    {
        for (int bucket = 1; bucket < 65536; bucket++)
        {
            int parent = bucket & ~(1 << (31 - System.Numerics.BitOperations.LeadingZeroCount((uint)bucket)));

            Assert.IsTrue(
                ConcurrentHashSet<int>.MakeSentinelKey(parent) < ConcurrentHashSet<int>.MakeSentinelKey(bucket),
                $"Parent sentinel {parent} must precede child sentinel {bucket}.");
        }
    }
}
