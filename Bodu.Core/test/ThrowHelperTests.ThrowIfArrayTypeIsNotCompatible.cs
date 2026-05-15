// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayTypeIsNotCompatible.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

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
    public void ThrowIfArrayTypeIsNotCompatible_WhenArrayTypeIsIncorrect_ShouldThrowArgumentException(Array array)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array);
        });
    }
    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfArrayTypeIsNotCompatible{T}" /> contract for the
    /// <c>int</c> instantiation: null array → <see cref="ArgumentNullException" />, compatible
    /// <c>int[]</c> → no throw, incompatible types → <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw, or <see langword="null" />.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayTypeIsNotCompatibleContractData))]
    public void ThrowIfArrayTypeIsNotCompatible_WhenInvokedWithVariousArrays_ShouldFollowContract(
        string testName, Array? array, Type? expectedExceptionType, string? expectedParamName)
    {
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array!, nameof(array)),
            expectedExceptionType,
            expectedParamName);
    }

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

    private static IEnumerable<object?[]> ThrowIfArrayTypeIsNotCompatibleContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "string[] expected int[] → ArgumentException", new string[3], typeof(ArgumentException), "array" };
        yield return new object?[] { "double[] expected int[] → ArgumentException", new double[3], typeof(ArgumentException), "array" };
        yield return new object?[] { "object[] expected int[] → ArgumentException", new object[3], typeof(ArgumentException), "array" };
        yield return new object?[] { "matching int[] → no throw", new int[5], null, null };
        yield return new object?[] { "empty int[] → no throw", Array.Empty<int>(), null, null };
    }

}
