// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOther.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOther{T}(T, T, string, string)" /> throws
    /// <see cref="ArgumentException" /> when the value is strictly greater than the comparison reference.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-exceeds-other pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanOtherInvalidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThanOther_WhenValueIsGreaterThanOther_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfGreaterThanOther(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOther{T}(T, T, string, string)" /> completes
    /// silently when the value is less than or equal to the comparison reference.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-below-other pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanOtherValidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThanOther_WhenValueIsLessThanOrEqualToOther_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfGreaterThanOther(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOther{T}(T, T, string, string)" /> reports
    /// the <c>value</c> parameter name (never <c>other</c>) on the thrown <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-exceeds-other pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanOtherParamNameCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThanOther_WhenValueIsGreaterThanOther_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfGreaterThanOther(kat.Value, kat.Other, kat.ParamName, "other"),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThanOther_WhenValueIsGreaterThanOther_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/other pairs where the value strictly exceeds the comparison reference.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanOtherInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("6 > 5", 6, 5, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("1 > 0", 1, 0, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MaxValue > MaxValue-1", int.MaxValue, int.MaxValue - 1, typeof(ArgumentException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThanOther_WhenValueIsLessThanOrEqualToOther_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/other pairs where the value is less than or equal to the comparison reference.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanOtherValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("5 == 5", 5, 5) };
        yield return new object?[] { new GuardValidKat<int>("4 < 5", 4, 5) };
        yield return new object?[] { new GuardValidKat<int>("3 == 3", 3, 3) };
        yield return new object?[] { new GuardValidKat<int>("2 < 3", 2, 3) };
        yield return new object?[] { new GuardValidKat<int>("0 == 0", 0, 0) };
        yield return new object?[] { new GuardValidKat<int>("-1 < 0", -1, 0) };
        yield return new object?[] { new GuardValidKat<int>("MinValue < MaxValue", int.MinValue, int.MaxValue) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThanOther_WhenValueIsGreaterThanOther_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanOtherParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("6 > 5, paramName=value", 6, 5, typeof(ArgumentException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("MaxValue > MaxValue-1, paramName=value", int.MaxValue, int.MaxValue - 1, typeof(ArgumentException), "value") };
    }

}
