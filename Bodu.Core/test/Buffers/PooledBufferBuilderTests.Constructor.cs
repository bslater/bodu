// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PooledBufferBuilderTests.Constructor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Buffers;

public partial class PooledBufferBuilderTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="PooledBufferBuilder{T}"/> with an <c>initialCapacity</c> of zero
    /// throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInitialCapacityIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new PooledBufferBuilder<int>(0);
        });

        Assert.AreEqual("initialCapacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="PooledBufferBuilder{T}"/> with a negative <c>initialCapacity</c>
    /// throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInitialCapacityIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new PooledBufferBuilder<int>(-1);
        });

        Assert.AreEqual("initialCapacity", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="PooledBufferBuilder{T}"/> with a valid capacity succeeds and
    /// yields a builder with zero elements and a capacity at least as large as the requested value.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInitialCapacityIsValid_ShouldInitialiseEmptyBuilderWithSufficientCapacity()
    {
        using var builder = new PooledBufferBuilder<int>(16);

        Assert.AreEqual(0, builder.WrittenCount);
        Assert.IsTrue(builder.Capacity >= 16);
    }

    /// <summary>
    /// Verifies that the default constructor (initialCapacity = 256) produces a builder with a capacity of at
    /// least 256 and an initial count of zero.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenUsingDefaultCapacity_ShouldInitialiseWithCapacityAtLeast256()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.AreEqual(0, builder.WrittenCount);
        Assert.IsTrue(builder.Capacity >= 256);
    }

    /// <summary>
    /// Verifies that a freshly constructed <see cref="PooledBufferBuilder{T}"/> is assignable to
    /// <see cref="System.Buffers.IBufferWriter{T}"/>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenConstructed_ShouldBeAssignableToIBufferWriter()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.IsInstanceOfType<System.Buffers.IBufferWriter<int>>(builder);
    }

    /// <summary>
    /// Verifies that a freshly constructed <see cref="PooledBufferBuilder{T}"/> is assignable to
    /// <see cref="System.Buffers.IMemoryOwner{T}"/>.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenConstructed_ShouldBeAssignableToIMemoryOwner()
    {
        using var builder = new PooledBufferBuilder<int>();

        Assert.IsInstanceOfType<System.Buffers.IMemoryOwner<int>>(builder);
    }
}
