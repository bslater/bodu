// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDestinationSpanTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the contract for <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall{TSource, TDestination}" />:
    /// when the destination is shorter than the source, ParamName must reference the <c>destination</c>
    /// parameter, never the <c>source</c>. Spans are value types so there is no null branch.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="sourceLength">The source span length.</param>
    /// <param name="destinationLength">The destination span length.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("destination shorter than source → throw on destination", 10, 5, true)]
    [DataRow("equal lengths → pass", 5, 5, false)]
    [DataRow("destination larger → pass", 3, 5, false)]
    [DataRow("both empty → pass", 0, 0, false)]
    public void ThrowIfDestinationSpanTooSmall_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int sourceLength, int destinationLength, bool expectsException)
    {
        var source = new byte[sourceLength];
        var destination = new byte[destinationLength];
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "destination" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfDestinationSpanTooSmall<byte, byte>(source.AsSpan(), destination.AsSpan(), "destination"),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall" />, when DestinationTooSmall, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(10, 5)] // destination too small
    [DataRow(5, 4)]
    public void ThrowIfDestinationSpanTooSmall_WhenDestinationTooSmall_ShouldThrowArgumentException(int sourceLength, int destinationLength)
    {
        var source = new byte[sourceLength];
        var destination = new byte[destinationLength];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfDestinationSpanTooSmall<byte, byte>(source.AsSpan(), destination.AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall" />, when DestinationSufficient, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(3, 5)]
    [DataRow(0, 0)]
    public void ThrowIfDestinationSpanTooSmall_WhenDestinationSufficient_ShouldNotThrow(int sourceLength, int destinationLength)
    {
        var source = new int[sourceLength];
        var destination = new int[destinationLength];

        ThrowHelper.ThrowIfDestinationSpanTooSmall<int, int>(source.AsSpan(), destination.AsSpan());
    }
}
