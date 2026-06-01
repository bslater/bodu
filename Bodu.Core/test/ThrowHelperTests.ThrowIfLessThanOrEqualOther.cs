// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOrEqualOther.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqualOther{T}(T, T, string, string)" /> throws
    /// <see cref="ArgumentException" /> when the value is less than or equal to the comparison reference.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-below-other pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOrEqualOtherInvalidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOrEqualOther_WhenValueIsLessThanOrEqualToOther_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThanOrEqualOther(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqualOther{T}(T, T, string, string)" />
    /// completes silently when the value is strictly greater than the comparison reference.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-above-other pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOrEqualOtherValidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOrEqualOther_WhenValueIsGreaterThanOther_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfLessThanOrEqualOther(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqualOther{T}(T, T, string, string)" /> reports
    /// the <c>value</c> parameter name (never <c>other</c>) on the thrown <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-below-other pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanOrEqualOtherParamNameCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThanOrEqualOther_WhenValueIsLessThanOrEqualToOther_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThanOrEqualOther(kat.Value, kat.Other, kat.ParamName, "other"),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOrEqualOther_WhenValueIsLessThanOrEqualToOther_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/other pairs where the value is less than or equal to the comparison reference.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOrEqualOtherInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("0 <= 1", 0, 1, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("5 <= 5", 5, 5, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("5 <= 6", 5, 6, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("3 <= 3", 3, 3, typeof(ArgumentException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue <= MaxValue", int.MinValue, int.MaxValue, typeof(ArgumentException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOrEqualOther_WhenValueIsGreaterThanOther_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/other pairs where the value is strictly greater than the comparison reference.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOrEqualOtherValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("6 > 5", 6, 5) };
        yield return new object?[] { new GuardValidKat<int>("1 > 0", 1, 0) };
        yield return new object?[] { new GuardValidKat<int>("MaxValue > MinValue", int.MaxValue, int.MinValue) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThanOrEqualOther_WhenValueIsLessThanOrEqualToOther_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanOrEqualOtherParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("0 <= 1, paramName=value", 0, 1, typeof(ArgumentException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("5 <= 5, paramName=value", 5, 5, typeof(ArgumentException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue <= MaxValue, paramName=value", int.MinValue, int.MaxValue, typeof(ArgumentException), "value") };
    }

}
