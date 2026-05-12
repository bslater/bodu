// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDestinationTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationTooSmall" />, Array, when DestinationTooSmall, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 3)] // destination too small
    [DataRow(4, 2)]
    public void ThrowIfDestinationTooSmall_Array_WhenDestinationTooSmall_ShouldThrowArgumentException(int sourceLength, int destinationLength)
    {
        var source = new int[sourceLength];
        var destination = new byte[destinationLength];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfDestinationTooSmall(source, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationTooSmall" />, Array, when DestinationSufficient, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(3, 5)]
    [DataRow(0, 0)]
    public void ThrowIfDestinationTooSmall_Array_WhenDestinationSufficient_ShouldNotThrow(int sourceLength, int destinationLength)
    {
        var source = new int[sourceLength];
        var destination = new byte[destinationLength];

        ThrowHelper.ThrowIfDestinationTooSmall(source, destination);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationTooSmall{TSource, TDestination}(TSource[], TDestination[], string)" /> throws
    /// <see cref="ArgumentNullException" /> when the source array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfDestinationTooSmall_Array_WhenSourceIsNull_ShouldThrowExactly()
    {
        int[]? source = null;
        var destination = new byte[5];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfDestinationTooSmall(source!, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationTooSmall{TSource, TDestination}(TSource[], TDestination[], string)" /> throws
    /// <see cref="ArgumentNullException" /> when the destination array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfDestinationTooSmall_Array_WhenDestinationIsNull_ShouldThrowExactly()
    {
        var source = new int[5];
        byte[]? destination = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfDestinationTooSmall(source, destination!);
        });
    }
}
