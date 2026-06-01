// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOrEqual.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqual{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is less than or equal to the supplied
    /// exclusive lower bound.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-below-min pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOrEqualInvalidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOrEqual_WhenValueIsLessThanOrEqualToMin_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThanOrEqual(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqual{T}(T, T, string)" /> completes silently
    /// when the value is strictly greater than the supplied exclusive lower bound.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-above-min pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOrEqualValidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOrEqual_WhenValueIsGreaterThanMin_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfLessThanOrEqual(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqual{T}(T, T, string)" /> reports the
    /// explicitly supplied <c>paramName</c> on the thrown <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-below-min pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOrEqualParamNameCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOrEqual_WhenValueIsLessThanOrEqualToMin_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThanOrEqual(kat.Value, kat.Other, kat.ParamName),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOrEqual_WhenValueIsLessThanOrEqualToMin_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/min pairs where the value is less than or equal to the minimum.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOrEqualInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("5 <= 5", 5, 5, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("4 <= 5", 4, 5, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue <= MinValue", int.MinValue, int.MinValue, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("0 <= 1", 0, 1, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("3 <= 3", 3, 3, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue <= MaxValue", int.MinValue, int.MaxValue, typeof(ArgumentOutOfRangeException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOrEqual_WhenValueIsGreaterThanMin_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/min pairs where the value is strictly greater than the minimum.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOrEqualValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("6 > 5", 6, 5) };
        yield return new object?[] { new GuardValidKat<int>("1 > 0", 1, 0) };
        yield return new object?[] { new GuardValidKat<int>("MaxValue > MinValue", int.MaxValue, int.MinValue) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOrEqual_WhenValueIsLessThanOrEqualToMin_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOrEqualParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("5 <= 5, paramName=value", 5, 5, typeof(ArgumentOutOfRangeException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("4 <= 5, paramName=value", 4, 5, typeof(ArgumentOutOfRangeException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue <= MinValue, paramName=value", int.MinValue, int.MinValue, typeof(ArgumentOutOfRangeException), "value") };
    }

}
