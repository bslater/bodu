// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThan.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThan{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for every <see cref="GuardInvalidKat{Int32}" /> row whose
    /// value exceeds the supplied maximum.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-exceeds-max pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanInvalidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThan_WhenValueExceedsMax_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfGreaterThan(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThan{T}(T, T, string)" /> completes silently when
    /// the value is less than or equal to the supplied maximum.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-below-max pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanValidCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThan_WhenValueIsLessThanOrEqualToMax_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfGreaterThan(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThan{T}(T, T, string)" /> reports the explicitly
    /// supplied <c>paramName</c> on the thrown <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-exceeds-max pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfGreaterThanParamNameCases),
        DynamicDataSourceType.Method,
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfGreaterThan_WhenValueExceedsMax_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfGreaterThan(kat.Value, kat.Other, kat.ParamName),
            kat.ExceptionType,
            kat.ParamName);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThan_WhenValueExceedsMax_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/max pairs where the value exceeds the maximum and the guard must throw.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("6 > 5", 6, 5, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("1 > 0", 1, 0, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MaxValue > MaxValue-1", int.MaxValue, int.MaxValue - 1, typeof(ArgumentOutOfRangeException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThan_WhenValueIsLessThanOrEqualToMax_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/max pairs where the value is less than or equal to the maximum.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("5 == 5", 5, 5) };
        yield return new object?[] { new GuardValidKat<int>("4 < 5", 4, 5) };
        yield return new object?[] { new GuardValidKat<int>("MinValue == MinValue", int.MinValue, int.MinValue) };
        yield return new object?[] { new GuardValidKat<int>("3 == 3", 3, 3) };
        yield return new object?[] { new GuardValidKat<int>("2 < 3", 2, 3) };
        yield return new object?[] { new GuardValidKat<int>("0 == 0", 0, 0) };
        yield return new object?[] { new GuardValidKat<int>("-1 < 0", -1, 0) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfGreaterThan_WhenValueExceedsMax_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfGreaterThanParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("6 > 5, paramName=value", 6, 5, typeof(ArgumentOutOfRangeException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("MaxValue > MaxValue-1, paramName=value", int.MaxValue, int.MaxValue - 1, typeof(ArgumentOutOfRangeException), "value") };
    }

}
