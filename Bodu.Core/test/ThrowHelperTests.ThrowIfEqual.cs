// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEqual.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for every <see cref="GuardInvalidKat{Int32}" /> row.
    /// </summary>
    /// <param name="kat">The KAT row supplying the equal operand pair and expected exception type.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfEqualIntInvalidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfEqual_WhenIntValuesAreEqual_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfEqual(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}(T, T, string)" /> completes silently for every
    /// <see cref="GuardValidKat{Int32}" /> row.
    /// </summary>
    /// <param name="kat">The KAT row supplying a distinct operand pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfEqualIntValidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfEqual_WhenIntValuesAreNotEqual_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfEqual(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for every <see cref="GuardInvalidKat{String}" /> row.
    /// </summary>
    /// <param name="kat">The KAT row supplying the equal operand pair and expected exception type.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfEqualStringInvalidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfEqual_WhenStringValuesAreEqual_ShouldThrowExactly(GuardInvalidKat<string> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfEqual(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}(T, T, string)" /> completes silently for every
    /// <see cref="GuardValidKat{String}" /> row.
    /// </summary>
    /// <param name="kat">The KAT row supplying a distinct operand pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfEqualStringValidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfEqual_WhenStringValuesAreNotEqual_ShouldNotThrow(GuardValidKat<string> kat) =>
        ThrowHelper.ThrowIfEqual(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEqual{T}(T, T, string)" /> reports the explicitly
    /// supplied <c>paramName</c> on the thrown <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying the equal operand pair and expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfEqualParamNameCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfEqual_WhenValuesAreEqual_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfEqual(kat.Value, kat.Other, kat.ParamName),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfEqual_WhenIntValuesAreEqual_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>The known invalid rows for the integer overload.</returns>
    private static IEnumerable<object?[]> ThrowIfEqualIntInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("int zero", 0, 0, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("int one", 1, 1, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("int negative", -5, -5, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("int.MaxValue", int.MaxValue, int.MaxValue, typeof(ArgumentOutOfRangeException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfEqual_WhenIntValuesAreNotEqual_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>The known valid rows for the integer overload.</returns>
    private static IEnumerable<object?[]> ThrowIfEqualIntValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("zero vs one", 0, 1) };
        yield return new object?[] { new GuardValidKat<int>("one vs two", 1, 2) };
        yield return new object?[] { new GuardValidKat<int>("negative vs zero", -1, 0) };
        yield return new object?[] { new GuardValidKat<int>("MinValue vs MaxValue", int.MinValue, int.MaxValue) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{String}" /> rows used by
    /// <see cref="ThrowIfEqual_WhenStringValuesAreEqual_ShouldThrowExactly(GuardInvalidKat{String})" />.
    /// </summary>
    /// <returns>The known invalid rows for the string overload.</returns>
    private static IEnumerable<object?[]> ThrowIfEqualStringInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<string>("hello equals hello", "hello", "hello", typeof(ArgumentOutOfRangeException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{String}" /> rows used by
    /// <see cref="ThrowIfEqual_WhenStringValuesAreNotEqual_ShouldNotThrow(GuardValidKat{String})" />.
    /// </summary>
    /// <returns>The known valid rows for the string overload.</returns>
    private static IEnumerable<object?[]> ThrowIfEqualStringValidCases()
    {
        yield return new object?[] { new GuardValidKat<string>("hello vs world", "hello", "world") };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfEqual_WhenValuesAreEqual_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfEqualParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("explicit paramName=value", 42, 42, typeof(ArgumentOutOfRangeException), "value") };
    }

}
