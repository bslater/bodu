// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthNotPositiveMultipleOf.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" /> does not throw —
    /// and on the ParamName-asserting overload reports nothing — for arrays whose length is a positive
    /// multiple of the divisor.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="divisor">The required divisor.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayLengthNotPositiveMultipleOfValidContractData))]
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenArrayIsAccepted_ShouldNotThrowAndReportNothing(
        string testName, Array array, int divisor) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array, divisor, nameof(array)),
            null,
            null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" /> throws the expected
    /// exception type with <c>ParamName == "array"</c> for null arrays, zero-length arrays, and arrays whose
    /// length is not a positive multiple of the divisor.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="divisor">The required divisor.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayLengthNotPositiveMultipleOfInvalidContractData))]
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenArrayIsRejected_ShouldThrowExpected(
        string testName, Array? array, int divisor, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array!, divisor, nameof(array)),
            expectedExceptionType,
            expectedParamName);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" />, when LengthIsNotPositiveMultiple, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2)]   // 5 % 2 != 0
    [DataRow(7, 3)]   // 7 % 3 != 0
    [DataRow(0, 1)]   // 0 is not a positive multiple
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenLengthIsNotPositiveMultiple_ShouldThrowExactly(int arrayLength, int factor)
    {
        int[] array = new int[arrayLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array, factor);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" />, when LengthIsPositiveMultiple, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(6, 3)]   // 6 % 3 == 0
    [DataRow(4, 2)]   // 4 % 2 == 0
    [DataRow(8, 4)]   // 8 % 4 == 0
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenLengthIsPositiveMultiple_ShouldNotThrow(int arrayLength, int factor)
    {
        int[] array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array, factor);
    }

    private static IEnumerable<object?[]> ThrowIfArrayLengthNotPositiveMultipleOfValidContractData()
    {
        yield return new object?[] { "exact divisor length", new int[4], 4 };
        yield return new object?[] { "valid multiple", new int[12], 4 };
    }

    private static IEnumerable<object?[]> ThrowIfArrayLengthNotPositiveMultipleOfInvalidContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, 4, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "zero-length array → ArgumentException", Array.Empty<int>(), 4, typeof(ArgumentException), "array" };
        yield return new object?[] { "length not multiple of divisor → ArgumentException", new int[5], 2, typeof(ArgumentException), "array" };
    }

}
