// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOrEqual.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOrEqual{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is greater than or equal to the supplied
    /// exclusive upper bound.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-above-max pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanOrEqualInvalidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThanOrEqual_WhenValueIsGreaterThanOrEqualToMax_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfGreaterThanOrEqual(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOrEqual{T}(T, T, string)" /> completes
    /// silently when the value is strictly less than the supplied exclusive upper bound.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-below-max pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanOrEqualValidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThanOrEqual_WhenValueIsLessThanMax_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfGreaterThanOrEqual(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOrEqual{T}(T, T, string)" /> reports the
    /// explicitly supplied <c>paramName</c> on the thrown <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-above-max pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanOrEqualParamNameCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThanOrEqual_WhenValueIsGreaterThanOrEqualToMax_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfGreaterThanOrEqual(kat.Value, kat.Other, kat.ParamName),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThanOrEqual_WhenValueIsGreaterThanOrEqualToMax_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/max pairs where the value is greater than or equal to the maximum.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanOrEqualInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("5 >= 5", 5, 5, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("6 >= 5", 6, 5, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("1 >= 0", 1, 0, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("0 >= 0", 0, 0, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MaxValue >= MaxValue", int.MaxValue, int.MaxValue, typeof(ArgumentOutOfRangeException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThanOrEqual_WhenValueIsLessThanMax_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/max pairs where the value is strictly less than the maximum.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanOrEqualValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("-1 < 0", -1, 0) };
        yield return new object?[] { new GuardValidKat<int>("4 < 5", 4, 5) };
        yield return new object?[] { new GuardValidKat<int>("MinValue < MaxValue", int.MinValue, int.MaxValue) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThanOrEqual_WhenValueIsGreaterThanOrEqualToMax_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanOrEqualParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("5 >= 5, paramName=value", 5, 5, typeof(ArgumentOutOfRangeException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("6 >= 5, paramName=value", 6, 5, typeof(ArgumentOutOfRangeException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("MaxValue >= MaxValue, paramName=value", int.MaxValue, int.MaxValue, typeof(ArgumentOutOfRangeException), "value") };
    }

}
