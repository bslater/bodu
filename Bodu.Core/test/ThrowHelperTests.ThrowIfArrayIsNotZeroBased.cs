// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayIsNotZeroBased.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

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
    /// Verifies the full <see cref="ThrowHelper.ThrowIfArrayIsNotZeroBased" /> contract matrix: null array →
    /// <see cref="ArgumentNullException" />, non-zero-based array → <see cref="ArgumentException" />, zero-based
    /// array → no throw. ParamName is asserted for every throwing row.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw, or <see langword="null" /> if it must pass.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayIsNotZeroBasedContractData))]
    public void ThrowIfArrayIsNotZeroBased_WhenInvokedWithVariousArrays_ShouldFollowContract(
        string testName, Array? array, Type? expectedExceptionType, string? expectedParamName)
    {
        AssertGuard(testName, () =>
        {
            ThrowHelper.ThrowIfArrayIsNotZeroBased(array!, nameof(array));
        }, expectedExceptionType, expectedParamName);
    }

    private static IEnumerable<object[]> GetNonZeroBasedArrayTestData()
    {
        yield return new object[] { Array.CreateInstance(typeof(int), [5], [1]) };
        yield return new object[] { Array.CreateInstance(typeof(string), [3], [-10]) };
    }

    private static IEnumerable<object[]> GetZeroBasedArrayTestData()
    {
        yield return new object[] { Array.Empty<int>() };
        yield return new object[] { new string[5] };
        yield return new object[] { Array.CreateInstance(typeof(double), new int[] { 4 }) }; // Zero-based by default
    }

    private static IEnumerable<object?[]> ThrowIfArrayIsNotZeroBasedContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "non-zero lower bound → ArgumentException", Array.CreateInstance(typeof(int), [3], [1]), typeof(ArgumentException), "array" };
        yield return new object?[] { "negative lower bound → ArgumentException", Array.CreateInstance(typeof(string), [3], [-10]), typeof(ArgumentException), "array" };
        yield return new object?[] { "empty zero-based int array → no throw", Array.Empty<int>(), null, null };
        yield return new object?[] { "zero-based string array → no throw", new string[5], null, null };
    }

}
