// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotEqual.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}(T, T, string)" /> completes silently for every
    /// <see cref="GuardValidKat{Int32}" /> row whose operands compare equal.
    /// </summary>
    /// <param name="kat">The KAT row supplying an equal operand pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfNotEqualIntValidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfNotEqual_WhenIntValuesAreEqual_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfNotEqual(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}(T, T, string)" /> throws
    /// <see cref="ArgumentException" /> for every <see cref="GuardInvalidKat{Int32}" /> row whose operands
    /// differ.
    /// </summary>
    /// <param name="kat">The KAT row supplying a differing operand pair and the expected exception type.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfNotEqualIntInvalidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfNotEqual_WhenIntValuesAreNotEqual_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfNotEqual(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}(T, T, string)" /> completes silently for every
    /// <see cref="GuardValidKat{String}" /> row whose operands compare equal.
    /// </summary>
    /// <param name="kat">The KAT row supplying an equal operand pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfNotEqualStringValidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfNotEqual_WhenStringValuesAreEqual_ShouldNotThrow(GuardValidKat<string> kat) =>
        ThrowHelper.ThrowIfNotEqual(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}(T, T, string)" /> throws
    /// <see cref="ArgumentException" /> for every <see cref="GuardInvalidKat{String}" /> row whose operands
    /// differ.
    /// </summary>
    /// <param name="kat">The KAT row supplying a differing operand pair and the expected exception type.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfNotEqualStringInvalidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfNotEqual_WhenStringValuesAreNotEqual_ShouldThrowExactly(GuardInvalidKat<string> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfNotEqual(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotEqual{T}(T, T, string)" /> reports the explicitly
    /// supplied <c>paramName</c> on the thrown <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a differing operand pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfNotEqualParamNameCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfNotEqual_WhenValuesAreNotEqual_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfNotEqual(kat.Value, kat.Other, kat.ParamName),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfNotEqual_WhenIntValuesAreEqual_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>The known valid rows for the integer overload.</returns>
    private static IEnumerable<object?[]> ThrowIfNotEqualIntValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("int zero", 0, 0) };
        yield return new object?[] { new GuardValidKat<int>("int one", 1, 1) };
        yield return new object?[] { new GuardValidKat<int>("int negative", -5, -5) };
        yield return new object?[] { new GuardValidKat<int>("int.MaxValue", int.MaxValue, int.MaxValue) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfNotEqual_WhenIntValuesAreNotEqual_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>The known invalid rows for the integer overload.</returns>
    private static IEnumerable<object?[]> ThrowIfNotEqualIntInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("zero vs one", 0, 1, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("one vs two", 1, 2, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("negative vs zero", -1, 0, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue vs MaxValue", int.MinValue, int.MaxValue, typeof(ArgumentException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{String}" /> rows used by
    /// <see cref="ThrowIfNotEqual_WhenStringValuesAreEqual_ShouldNotThrow(GuardValidKat{String})" />.
    /// </summary>
    /// <returns>The known valid rows for the string overload.</returns>
    private static IEnumerable<object?[]> ThrowIfNotEqualStringValidCases()
    {
        yield return new object?[] { new GuardValidKat<string>("hello equals hello", "hello", "hello") };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{String}" /> rows used by
    /// <see cref="ThrowIfNotEqual_WhenStringValuesAreNotEqual_ShouldThrowExactly(GuardInvalidKat{String})" />.
    /// </summary>
    /// <returns>The known invalid rows for the string overload.</returns>
    private static IEnumerable<object?[]> ThrowIfNotEqualStringInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<string>("hello vs world", "hello", "world", typeof(ArgumentException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfNotEqual_WhenValuesAreNotEqual_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfNotEqualParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("explicit paramName=value", 10, 42, typeof(ArgumentException), "value") };
    }

}
