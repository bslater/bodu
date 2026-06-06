// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayIsNotZeroBased.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayIsNotZeroBased" />, when ArrayIsNotZeroBased, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNonZeroBasedArrayTestData))]
    public void ThrowIfArrayIsNotZeroBased_WhenArrayIsNotZeroBased_ShouldThrowExactly(Array array)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayIsNotZeroBased" />, when ArrayIsZeroBased, NotThrow.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetZeroBasedArrayTestData))]
    public void ThrowIfArrayIsNotZeroBased_WhenArrayIsZeroBased_ShouldNotThrow(Array array) => ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayIsNotZeroBased" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for zero-based arrays.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayIsNotZeroBasedValidContractData))]
    public void ThrowIfArrayIsNotZeroBased_WhenArrayIsAccepted_ShouldNotThrowAndReportNothing(
        string testName, Array array) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayIsNotZeroBased(array, nameof(array)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayIsNotZeroBased" /> throws the expected exception
    /// type with <c>ParamName == "array"</c> for null arrays and arrays whose lower bound is non-zero.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayIsNotZeroBasedInvalidContractData))]
    public void ThrowIfArrayIsNotZeroBased_WhenArrayIsRejected_ShouldThrowExpected(
        string testName, Array? array, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayIsNotZeroBased(array!, nameof(array)), expectedExceptionType, expectedParamName);

    private static IEnumerable<object[]> GetNonZeroBasedArrayTestData()
    {
        yield return new object[] { Array.CreateInstance(typeof(int), [5], [1]) };
        yield return new object[] { Array.CreateInstance(typeof(string), [3], [-10]) };
    }

    private static IEnumerable<object[]> GetZeroBasedArrayTestData()
    {
        yield return new object[] { Array.Empty<int>() };
        yield return new object[] { new string[5] };
        yield return new object[] { Array.CreateInstance(typeof(double), [4]) }; // Zero-based by default
    }

    private static IEnumerable<object?[]> ThrowIfArrayIsNotZeroBasedValidContractData()
    {
        yield return new object?[] { "empty zero-based int array", Array.Empty<int>() };
        yield return new object?[] { "zero-based string array", new string[5] };
    }

    private static IEnumerable<object?[]> ThrowIfArrayIsNotZeroBasedInvalidContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "non-zero lower bound → ArgumentException", Array.CreateInstance(typeof(int), [3], [1]), typeof(ArgumentException), "array" };
        yield return new object?[] { "negative lower bound → ArgumentException", Array.CreateInstance(typeof(string), [3], [-10]), typeof(ArgumentException), "array" };
    }

}
