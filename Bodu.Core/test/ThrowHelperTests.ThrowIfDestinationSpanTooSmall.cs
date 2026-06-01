// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDestinationSpanTooSmall.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

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

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall" />, when DestinationTooSmall, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(10, 5)] // destination too small
    [DataRow(5, 4)]
    public void ThrowIfDestinationSpanTooSmall_WhenDestinationTooSmall_ShouldThrowExactly(int sourceLength, int destinationLength)
    {
        var source = new byte[sourceLength];
        var destination = new byte[destinationLength];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfDestinationSpanTooSmall<byte, byte>(source.AsSpan(), destination.AsSpan());
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall{TSource, TDestination}" /> does
    /// not throw — and on the ParamName-asserting overload reports nothing — when the destination span is
    /// at least as long as the source.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="sourceLength">The source span length.</param>
    /// <param name="destinationLength">The destination span length.</param>
    [TestMethod]
    [DataRow("equal lengths", 5, 5)]
    [DataRow("destination larger", 3, 5)]
    [DataRow("both empty", 0, 0)]
    public void ThrowIfDestinationSpanTooSmall_WhenDestinationFits_ShouldNotThrowAndReportNothing(
        string testName, int sourceLength, int destinationLength)
    {
        var source = new byte[sourceLength];
        var destination = new byte[destinationLength];

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfDestinationSpanTooSmall<byte, byte>(source.AsSpan(), destination.AsSpan(), "destination"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationSpanTooSmall{TSource, TDestination}" /> throws
    /// <see cref="ArgumentException" /> with <c>ParamName == "destination"</c> (never <c>"source"</c>) when
    /// the destination is shorter than the source.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="sourceLength">The source span length.</param>
    /// <param name="destinationLength">The destination span length.</param>
    [TestMethod]
    [DataRow("destination shorter than source", 10, 5)]
    public void ThrowIfDestinationSpanTooSmall_WhenDestinationTooShort_ShouldThrowOnDestination(
        string testName, int sourceLength, int destinationLength)
    {
        var source = new byte[sourceLength];
        var destination = new byte[destinationLength];

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfDestinationSpanTooSmall<byte, byte>(source.AsSpan(), destination.AsSpan(), "destination"),
            typeof(ArgumentException),
            "destination");
    }

}
