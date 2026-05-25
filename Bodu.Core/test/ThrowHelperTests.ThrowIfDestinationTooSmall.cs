// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDestinationTooSmall.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

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
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationTooSmall" />, Array, when DestinationTooSmall, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 3)] // destination too small
    [DataRow(4, 2)]
    public void ThrowIfDestinationTooSmall_Array_WhenDestinationTooSmall_ShouldThrowExactly(int sourceLength, int destinationLength)
    {
        var source = new int[sourceLength];
        var destination = new byte[destinationLength];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfDestinationTooSmall(source, destination);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationTooSmall{TSource, TDestination}(TSource[], TDestination[], string)" />
    /// does not throw — and on the ParamName-asserting overload reports nothing — when the destination
    /// array is at least as long as the source.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="sourceLength">Source length.</param>
    /// <param name="destinationLength">Destination length.</param>
    [TestMethod]
    [DataRow("equal lengths", 5, 5)]
    [DataRow("destination larger", 3, 5)]
    [DataRow("both empty", 0, 0)]
    public void ThrowIfDestinationTooSmall_Array_WhenDestinationFits_ShouldNotThrowAndReportNothing(
        string testName, int sourceLength, int destinationLength)
    {
        var source = new int[sourceLength];
        var destination = new byte[destinationLength];

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfDestinationTooSmall(source, destination, "destination"),
            null,
            null);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDestinationTooSmall{TSource, TDestination}(TSource[], TDestination[], string)" />
    /// throws the expected exception type with the expected <c>ParamName</c> when either array is
    /// <see langword="null" /> or the destination is too short for the source.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="sourceLength">Source length, or <c>-1</c> to pass null.</param>
    /// <param name="destinationLength">Destination length, or <c>-1</c> to pass null.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name.</param>
    /// <param name="expectedParamName">The expected ParamName.</param>
    [TestMethod]
    [DataRow("null source → ANE on source", -1, 5, "ArgumentNullException", "source")]
    [DataRow("null destination → ANE on destination", 5, -1, "ArgumentNullException", "destination")]
    [DataRow("destination shorter than source → AE on destination", 5, 3, "ArgumentException", "destination")]
    public void ThrowIfDestinationTooSmall_Array_WhenInputIsRejected_ShouldThrowOnExpectedParam(
        string testName, int sourceLength, int destinationLength, string expectedExceptionTypeName, string expectedParamName)
    {
        var source = sourceLength < 0 ? null : new int[sourceLength];
        var destination = destinationLength < 0 ? null : new byte[destinationLength];
        Type expected = Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
            ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfDestinationTooSmall(source!, destination!, "destination"),
            expected,
            expectedParamName);
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

}
