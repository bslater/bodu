// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthNotPositiveMultipleOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" /> contract: null
    /// array → <see cref="ArgumentNullException" />, zero length → <see cref="ArgumentException" />, valid
    /// positive multiple → no throw, invalid multiple → <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="divisor">The required divisor.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw, or <see langword="null" />.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayLengthNotPositiveMultipleOfContractData))]
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenInvokedWithVariousArrays_ShouldFollowContract(
        string testName, Array? array, int divisor, Type? expectedExceptionType, string? expectedParamName)
    {
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array!, divisor, nameof(array)),
            expectedExceptionType,
            expectedParamName);
    }

    private static IEnumerable<object?[]> ThrowIfArrayLengthNotPositiveMultipleOfContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, 4, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "zero-length array → ArgumentException", Array.Empty<int>(), 4, typeof(ArgumentException), "array" };
        yield return new object?[] { "length not multiple of divisor → ArgumentException", new int[5], 2, typeof(ArgumentException), "array" };
        yield return new object?[] { "exact divisor length → no throw", new int[4], 4, null, null };
        yield return new object?[] { "valid multiple → no throw", new int[12], 4, null, null };
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" />, when LengthIsNotPositiveMultiple, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2)]   // 5 % 2 != 0
    [DataRow(7, 3)]   // 7 % 3 != 0
    [DataRow(0, 1)]   // 0 is not a positive multiple
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenLengthIsNotPositiveMultiple_ShouldThrowExactly(int arrayLength, int factor)
    {
        var array = new int[arrayLength];
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
        var array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array, factor);
    }
}
