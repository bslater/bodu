// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOther.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOther{T}(T, T, string, string)" /> throws
    /// <see cref="ArgumentException" /> when the value is strictly less than the comparison reference.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-below-other pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOtherInvalidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOther_WhenValueIsLessThanOther_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThanOther(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOther{T}(T, T, string, string)" /> completes
    /// silently when the value is greater than or equal to the comparison reference.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-above-other pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOtherValidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOther_WhenValueIsEqualOrGreaterThanOther_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfLessThanOther(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOther{T}(T, T, string, string)" /> reports the
    /// <c>value</c> parameter name (never <c>other</c>) on the thrown <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-below-other pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOtherParamNameCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOther_WhenValueIsLessThanOther_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThanOther(kat.Value, kat.Other, kat.ParamName, "other"),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOther_WhenValueIsLessThanOther_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/other pairs where the value is strictly less than the comparison reference.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOtherInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("-1 < 0", -1, 0, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("5 < 6", 5, 6, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("0 < 1", 0, 1, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue < MaxValue", int.MinValue, int.MaxValue, typeof(ArgumentException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOther_WhenValueIsEqualOrGreaterThanOther_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/other pairs where the value is greater than or equal to the comparison reference.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOtherValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("5 == 5", 5, 5) };
        yield return new object?[] { new GuardValidKat<int>("6 > 5", 6, 5) };
        yield return new object?[] { new GuardValidKat<int>("0 == 0", 0, 0) };
        yield return new object?[] { new GuardValidKat<int>("MaxValue > MinValue", int.MaxValue, int.MinValue) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOther_WhenValueIsLessThanOther_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOtherParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("-1 < 0, paramName=value", -1, 0, typeof(ArgumentException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("5 < 6, paramName=value", 5, 6, typeof(ArgumentException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue < MaxValue, paramName=value", int.MinValue, int.MaxValue, typeof(ArgumentException), "value") };
    }

}
