// ---------------------------------------------------------------------------------------------------------------
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthIsZero.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsZero" />, when ArrayLengthIsNonZero, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(100)]
    public void ThrowIfArrayLengthIsZero_WhenArrayLengthIsNonZero_ShouldNotThrow(int length)
    {
        var array = new int[length];
        ThrowHelper.ThrowIfArrayLengthIsZero(array);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsZero" />, when ArrayLengthIsZero, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    public void ThrowIfArrayLengthIsZero_WhenArrayLengthIsZero_ShouldThrowExactly(int length)
    {
        var array = new int[length];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthIsZero(array);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsZero" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for non-empty arrays.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayLengthIsZeroValidContractData))]
    public void ThrowIfArrayLengthIsZero_WhenArrayIsAccepted_ShouldNotThrowAndReportNothing(string testName, Array array) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayLengthIsZero(array, nameof(array)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthIsZero" /> throws the expected exception type
    /// with <c>ParamName == "array"</c> for null arrays and zero-length arrays.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayLengthIsZeroInvalidContractData))]
    public void ThrowIfArrayLengthIsZero_WhenArrayIsRejected_ShouldThrowExpected(
        string testName, Array? array, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayLengthIsZero(array!, nameof(array)), expectedExceptionType, expectedParamName);

    private static IEnumerable<object?[]> ThrowIfArrayLengthIsZeroValidContractData()
    {
        yield return new object?[] { "single-element array", new[] { 0 } };
        yield return new object?[] { "non-empty string array", new[] { "a", "b" } };
    }

    private static IEnumerable<object?[]> ThrowIfArrayLengthIsZeroInvalidContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "empty array → ArgumentException", Array.Empty<int>(), typeof(ArgumentException), "array" };
    }

}
