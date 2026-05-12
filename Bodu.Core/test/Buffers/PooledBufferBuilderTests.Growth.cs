// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Growth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    private const int PoolBucketCeiling = 1024 * 1024;

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity" /> grows geometrically (current * 2) for buffers below the
    /// pool-bucket ceiling, where doubling aligns with the pool's power-of-two bucket round-up and is effectively free.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_BelowBucketCeiling_ShouldGrowGeometrically()
    {
        using var builder = new PooledBufferBuilder<byte>(64);
        int initial = builder.Capacity;

        builder.EnsureCapacity(initial + 1);

        Assert.IsTrue(builder.Capacity >= initial * 2,
            $"Expected at least {initial * 2}, got {builder.Capacity}.");
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity" /> grows linearly by one pool-bucket-ceiling step once the buffer
    /// crosses the ceiling — switching from geometric to linear growth so that above the pool's bucket boundary (where every rental is
    /// a fresh heap allocation) the builder does not waste up to 50% of memory on each grow.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void EnsureCapacity_AboveBucketCeiling_ShouldGrowLinearlyNotGeometrically()
    {
        using var builder = new PooledBufferBuilder<byte>(64);

        // Bring the buffer above the bucket ceiling in a single jump so any pool rounding has settled.
        int target = (PoolBucketCeiling * 2) + 1;
        builder.EnsureCapacity(target);
        int aboveCeiling = builder.Capacity;
        Assert.IsTrue(aboveCeiling >= PoolBucketCeiling, "Setup failed: buffer should be above the bucket ceiling.");

        // Force another growth and assert the step is the linear ceiling, not a geometric doubling.
        builder.EnsureCapacity(aboveCeiling + 1);

        int delta = builder.Capacity - aboveCeiling;
        Assert.IsTrue(delta >= PoolBucketCeiling,
            $"Expected linear step of at least {PoolBucketCeiling} bytes, got {delta}.");
        Assert.IsTrue(builder.Capacity < aboveCeiling * 2,
            $"Expected linear growth above the bucket ceiling, got geometric doubling: {builder.Capacity} >= {aboveCeiling * 2}.");
    }
}
