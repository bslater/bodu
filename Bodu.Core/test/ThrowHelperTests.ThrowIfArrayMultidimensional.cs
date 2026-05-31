// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayMultidimensional.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayHasRankThree, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayHasRankThree_ShouldThrowExactly()
    {
        Array array = new int[2, 2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayHasRankTwo, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayHasRankTwo_ShouldThrowExactly()
    {
        Array array = new int[2, 3];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayIsEmptySingleDimensional, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayIsEmptySingleDimensional_ShouldNotThrow()
    {
        Array array = Array.Empty<int>();
        ThrowHelper.ThrowIfArrayMultidimensional(array);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayIsNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayIsNull_ShouldThrowExactly()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayIsSingleDimensional, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayIsSingleDimensional_ShouldNotThrow()
    {
        Array array = new int[5];
        ThrowHelper.ThrowIfArrayMultidimensional(array);
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for single-dimensional arrays.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayMultidimensionalValidContractData))]
    public void ThrowIfArrayMultidimensional_WhenArrayIsAccepted_ShouldNotThrowAndReportNothing(string testName, Array array) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayMultidimensional(array, nameof(array)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" /> throws the expected exception
    /// type with <c>ParamName == "array"</c> for null arrays and arrays of rank greater than one.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayMultidimensionalInvalidContractData))]
    public void ThrowIfArrayMultidimensional_WhenArrayIsRejected_ShouldThrowExpected(
        string testName, Array? array, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayMultidimensional(array!, nameof(array)), expectedExceptionType, expectedParamName);

    private static IEnumerable<object?[]> ThrowIfArrayMultidimensionalValidContractData()
    {
        yield return new object?[] { "rank-1", new int[5] };
        yield return new object?[] { "rank-1 empty", Array.Empty<int>() };
    }

    private static IEnumerable<object?[]> ThrowIfArrayMultidimensionalInvalidContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "rank-2 → ArgumentException", new int[2, 3], typeof(ArgumentException), "array" };
        yield return new object?[] { "rank-3 → ArgumentException", new int[2, 2, 2], typeof(ArgumentException), "array" };
    }

}
