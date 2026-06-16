// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Growth.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity" /> grows geometrically (at least
    /// <c>current * 2</c>), aligning with the pool's power-of-two bucket round-up so each grow lands on the next
    /// bucket and the count of grow events stays amortized O(1).
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenExceeded_ShouldGrowAtLeastGeometrically()
    {
        using var builder = new PooledBufferBuilder<byte>(64);
        int initial = builder.Capacity;

        builder.EnsureCapacity(initial + 1);

        Assert.IsGreaterThanOrEqualTo(initial * 2, builder.Capacity,
            $"Expected at least {initial * 2}, got {builder.Capacity}.");
    }

    /// <summary>
    /// Verifies that <see cref="PooledBufferBuilder{T}.EnsureCapacity" /> jumps directly to the requested minimum
    /// when it exceeds twice the current capacity, so a single large request does not trigger a sequence of
    /// doublings.
    /// </summary>
    [TestMethod]
    public void EnsureCapacity_WhenMinimumExceedsDoubling_ShouldJumpToMinimum()
    {
        using var builder = new PooledBufferBuilder<byte>(64);
        int initial = builder.Capacity;
        int requested = initial * 10;

        builder.EnsureCapacity(requested);

        Assert.IsGreaterThanOrEqualTo(requested, builder.Capacity,
            $"Expected at least {requested}, got {builder.Capacity}.");
    }

}
