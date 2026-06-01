// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayContainsNonNumeric.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayContainsNonNumeric" />, when ArrayContainsNonNumeric, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNonNumericArrayTestData))]
    public void ThrowIfArrayContainsNonNumeric_WhenArrayContainsNonNumeric_ShouldThrowExactly(Array array)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayContainsNonNumeric(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayContainsNonNumeric" />, when ArrayIsNumeric, NotThrow.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNumericArrayTestData))]
    public void ThrowIfArrayContainsNonNumeric_WhenArrayIsNumeric_ShouldNotThrow(Array array) => ThrowHelper.ThrowIfArrayContainsNonNumeric(array);
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayContainsNonNumeric" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for arrays composed of BCL numeric primitives, nullable
    /// numerics, and null elements (which the documented contract ignores).
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayContainsNonNumericValidContractData))]
    public void ThrowIfArrayContainsNonNumeric_WhenArrayIsAccepted_ShouldNotThrowAndReportNothing(string testName, Array array) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayContainsNonNumeric(array, nameof(array)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayContainsNonNumeric" /> throws the expected exception
    /// type with <c>ParamName == "array"</c> for null arrays and arrays containing any non-numeric element.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="array">The array passed to the guard.</param>
    /// <param name="expectedExceptionType">The exception type the guard must throw.</param>
    /// <param name="expectedParamName">The expected <see cref="ArgumentException.ParamName" />.</param>
    [TestMethod]
    [DynamicData(nameof(ThrowIfArrayContainsNonNumericInvalidContractData))]
    public void ThrowIfArrayContainsNonNumeric_WhenArrayIsRejected_ShouldThrowExpected(
        string testName, Array? array, Type expectedExceptionType, string? expectedParamName) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfArrayContainsNonNumeric(array, nameof(array)), expectedExceptionType, expectedParamName);

    private static IEnumerable<object[]> GetNonNumericArrayTestData()
    {
        yield return new object[] { new object[] { 1, 2.0, "string" } };   // includes string
        yield return new object[] { new object[] { "abc" } };              // only string
        yield return new object[] { new object[] { new() } };       // object instance
    }

    private static IEnumerable<object[]> GetNumericArrayTestData()
    {
        yield return new object[] { new object[] { 1, 2, 3, 4 } };                          // all int
        yield return new object[] { new object[] { 1.5, 2.5 } };                            // float/double
        yield return new object[] { new object[] { 1m, 2m, null } };                        // decimal + null
        yield return new object[] { new object[] { (byte)1, (short)2, (long)3 } };          // mixed numeric
        yield return new object[] { new object[] { (sbyte)1, (ushort)2, (uint)3, (ulong)4 } }; // all unsigned
    }

    private static IEnumerable<object?[]> ThrowIfArrayContainsNonNumericValidContractData()
    {
        yield return new object?[]
        {
            "all primitive numerics",
            new object[] { (byte)1, (sbyte)-1, (short)-2, (ushort)2, -3, 3u, -4L, 4UL, 1.25f, 2.5d, 3.75m },
        };
        yield return new object?[] { "null elements only", new object?[] { null, null, null } };
        yield return new object?[] { "nullable numeric elements", new object?[] { (int?)5, null, (decimal?)2.5m } };
    }

    private static IEnumerable<object?[]> ThrowIfArrayContainsNonNumericInvalidContractData()
    {
        yield return new object?[] { "null array → ArgumentNullException", null, typeof(ArgumentNullException), "array" };
        yield return new object?[] { "string element → ArgumentException", new object[] { 1, "x" }, typeof(ArgumentException), "array" };
        yield return new object?[] { "object element → ArgumentException", new object[] { new() }, typeof(ArgumentException), "array" };
        yield return new object?[] { "char element → ArgumentException", new object[] { 'a' }, typeof(ArgumentException), "array" };
        yield return new object?[] { "bool element → ArgumentException", new object[] { true }, typeof(ArgumentException), "array" };
    }

}
