// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDestinationTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// Verifies the multi-parameter contract for the
    /// <see cref="ThrowHelper.ThrowIfDestinationTooSmall{TSource, TDestination}(TSource[], TDestination[], string)" />
    /// array overload: source-null → ANE on the hardcoded <c>source</c> name; destination-null and
    /// destination-too-small → ArgumentException-derived with ParamName for <c>destination</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="sourceLength">Source length, or <c>-1</c> to pass null.</param>
    /// <param name="destinationLength">Destination length, or <c>-1</c> to pass null.</param>
    /// <param name="expectedExceptionTypeName">The thrown exception's short type name, or empty if no throw.</param>
    /// <param name="expectedParamName">The expected ParamName, or empty if not asserted.</param>
    [TestMethod]
    [DataRow("null source → ANE on source", -1, 5, "ArgumentNullException", "source")]
    [DataRow("null destination → ANE on destination", 5, -1, "ArgumentNullException", "destination")]
    [DataRow("destination shorter than source → AE on destination", 5, 3, "ArgumentException", "destination")]
    [DataRow("equal lengths → pass", 5, 5, "", "")]
    [DataRow("destination larger → pass", 3, 5, "", "")]
    [DataRow("both empty → pass", 0, 0, "", "")]
    public void ThrowIfDestinationTooSmall_Array_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int sourceLength, int destinationLength, string expectedExceptionTypeName, string expectedParamName)
    {
        var source = sourceLength < 0 ? null : new int[sourceLength];
        var destination = destinationLength < 0 ? null : new byte[destinationLength];
        Type? expected = expectedExceptionTypeName.Length == 0
            ? null
            : Type.GetType($"System.{expectedExceptionTypeName}, System.Private.CoreLib")
                ?? throw new InvalidOperationException($"Unknown exception type '{expectedExceptionTypeName}'.");
        var param = expectedParamName.Length == 0 ? null : expectedParamName;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfDestinationTooSmall(source!, destination!, "destination"),
            expected,
            param);
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
