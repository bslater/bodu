// ---------------------------------------------------------------------------------------------------------------
// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthIsZero.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfArrayLengthIsZero" /> contract matrix: null array →
    /// <see cref="ArgumentNullException" />, zero-length array → <see cref="ArgumentException" />, non-empty
    /// array → no throw.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw, or <see langword="null" /> if it must pass.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayLengthIsZeroContractData))]
    public void ThrowIfArrayLengthIsZero_WhenInvokedWithVariousArrays_ShouldFollowContract(
        string testName, Array? array, Type? expectedExceptionType, string? expectedParamName)
    {
        AssertGuard(testName, () =>
        {
            ThrowHelper.ThrowIfArrayLengthIsZero(array!, nameof(array));
        }, expectedExceptionType, expectedParamName);
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

    private static IEnumerable<object?[]> ThrowIfArrayLengthIsZeroContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "empty array → ArgumentException", Array.Empty<int>(), typeof(ArgumentException), "array" };
        yield return new object?[] { "single-element array → no throw", new[] { 0 }, null, null };
        yield return new object?[] { "non-empty string array → no throw", new[] { "a", "b" }, null, null };
    }
}
