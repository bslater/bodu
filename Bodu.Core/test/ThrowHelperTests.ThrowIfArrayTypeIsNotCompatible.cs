// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayTypeIsNotCompatible.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayTypeIsNotCompatible" />, when ArrayTypeIsCorrect, NotThrow.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetCompatibleArrayTypeTestData))]
    public void ThrowIfArrayTypeIsNotCompatible_WhenArrayTypeIsCorrect_ShouldNotThrow(Array array) => ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayTypeIsNotCompatible" />, when ArrayTypeIsIncorrect, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetIncompatibleArrayTypeTestData))]
    public void ThrowIfArrayTypeIsNotCompatible_WhenArrayTypeIsIncorrect_ShouldThrowExactly(Array array)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayTypeIsNotCompatible{T}" /> does not throw — and on
    /// the ParamName-asserting overload reports nothing — for arrays whose element type matches
    /// <c>int</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayTypeIsNotCompatibleValidContractData))]
    public void ThrowIfArrayTypeIsNotCompatible_WhenArrayIsAccepted_ShouldNotThrowAndReportNothing(string testName, Array array) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array, nameof(array)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayTypeIsNotCompatible{T}" /> throws the expected
    /// exception type with <c>ParamName == "array"</c> for null arrays and arrays whose element type does
    /// not match <c>int</c>.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayTypeIsNotCompatibleInvalidContractData))]
    public void ThrowIfArrayTypeIsNotCompatible_WhenArrayIsRejected_ShouldThrowExpected(
        string testName, Array? array, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array!, nameof(array)), expectedExceptionType, expectedParamName);

    private static IEnumerable<object[]> GetCompatibleArrayTypeTestData()
    {
        yield return new object[] { Array.Empty<int>() };
        yield return new object[] { new int[10] };
        yield return new object[] { Array.CreateInstance(typeof(int), 5) };
    }

    private static IEnumerable<object[]> GetIncompatibleArrayTypeTestData()
    {
        yield return new object[] { new string[5] };
        yield return new object[] { new double[3] };
        yield return new object[] { Array.CreateInstance(typeof(object), 2) };
    }

    private static IEnumerable<object?[]> ThrowIfArrayTypeIsNotCompatibleValidContractData()
    {
        yield return new object?[] { "matching int[]", new int[5] };
        yield return new object?[] { "empty int[]", Array.Empty<int>() };
    }

    private static IEnumerable<object?[]> ThrowIfArrayTypeIsNotCompatibleInvalidContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "string[] expected int[] → ArgumentException", new string[3], typeof(ArgumentException), "array" };
        yield return new object?[] { "double[] expected int[] → ArgumentException", new double[3], typeof(ArgumentException), "array" };
        yield return new object?[] { "object[] expected int[] → ArgumentException", new object[3], typeof(ArgumentException), "array" };
    }

}
