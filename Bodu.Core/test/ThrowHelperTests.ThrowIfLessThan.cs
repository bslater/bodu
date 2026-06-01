// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThan.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.Kat;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan{T}(T, T, string)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the value is strictly less than the supplied
    /// inclusive minimum.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-below-min pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanInvalidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThan_WhenValueIsLessThanMin_ShouldThrowExactly(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThan(kat.Value, kat.Other),
            kat.ExceptionType,
            expectedParamName: null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan{T}(T, T, string)" /> completes silently when
    /// the value is greater than or equal to the supplied inclusive minimum.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-at-or-above-min pair.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanValidCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThan_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(GuardValidKat<int> kat) =>
        ThrowHelper.ThrowIfLessThan(kat.Value, kat.Other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThan{T}(T, T, string)" /> reports the explicitly
    /// supplied <c>paramName</c> on the thrown <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="kat">The KAT row supplying a value-below-min pair and the expected <c>ParamName</c>.</param>
    [TestMethod]
    [DynamicData(
        nameof(ThrowIfLessThanParamNameCases),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ThrowIfLessThan_WhenValueIsLessThanMin_ShouldReportParamName(GuardInvalidKat<int> kat) =>
        ExceptionAssert.AssertGuard(
            kat.Name,
            () => ThrowHelper.ThrowIfLessThan(kat.Value, kat.Other, kat.ParamName),
            kat.ExceptionType,
            kat.ParamName);

    // Nullable overloads
    //
    // The nullable-struct ThrowIfLessThan overload accepts an additional `throwIfNull` flag whose handling
    // does not fit the standard two-operand guard shape, so these tests stay in their existing binary
    // [DataRow] form rather than migrating to the GuardValidKat / GuardInvalidKat KAT records used above.

    /// <summary>
    /// Verifies that the nullable-struct <see cref="ThrowHelper.ThrowIfLessThan{T}(T?, T, bool, string)" />
    /// overload completes silently when the value is non-null and greater than or equal to the supplied
    /// minimum, regardless of <c>throwIfNull</c>.
    /// </summary>
    /// <param name="value">The nullable value compared against <paramref name="min" />.</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="throwIfNull">Whether the overload should throw on a null value (ignored here since the value is non-null).</param>
    [TestMethod]
    [DataRow(5, 5, false)]
    [DataRow(6, 5, false)]
    public void ThrowIfLessThan_Nullable_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(int? value, int min, bool throwIfNull) =>
        ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);

    /// <summary>
    /// Verifies that the nullable-struct <see cref="ThrowHelper.ThrowIfLessThan{T}(T?, T, bool, string)" />
    /// overload throws <see cref="ArgumentOutOfRangeException" /> when the value is non-null and less than
    /// the supplied minimum.
    /// </summary>
    /// <param name="value">The nullable value compared against <paramref name="min" />.</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="throwIfNull">Whether the overload should throw on a null value (ignored here since the value is non-null).</param>
    [TestMethod]
    [DataRow(2, 5, false)]
    [DataRow(-1, 0, false)]
    public void ThrowIfLessThan_Nullable_WhenValueIsLessThanMin_ShouldThrowExactly(int? value, int min, bool throwIfNull)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        });
    }

    /// <summary>
    /// Verifies that the nullable-struct <see cref="ThrowHelper.ThrowIfLessThan{T}(T?, T, bool, string)" />
    /// overload throws <see cref="ArgumentNullException" /> when the value is <see langword="null" /> and
    /// <c>throwIfNull</c> is <see langword="true" />.
    /// </summary>
    /// <param name="value">The nullable value (always <see langword="null" /> here).</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="throwIfNull">Whether the overload should throw on a null value.</param>
    [TestMethod]
    [DataRow(null, 5, true)]
    public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNull_ShouldThrowExactly(int? value, int min, bool throwIfNull)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);
        });
    }

    /// <summary>
    /// Verifies that the nullable-struct <see cref="ThrowHelper.ThrowIfLessThan{T}(T?, T, bool, string)" />
    /// overload completes silently when the value is <see langword="null" /> and <c>throwIfNull</c> is
    /// <see langword="false" />.
    /// </summary>
    /// <param name="value">The nullable value (always <see langword="null" /> here).</param>
    /// <param name="min">The inclusive minimum.</param>
    /// <param name="throwIfNull">Whether the overload should throw on a null value.</param>
    [TestMethod]
    [DataRow(null, 5, false)]
    public void ThrowIfLessThan_Nullable_WhenValueIsNullAndThrowIfNullIsFalse_ShouldNotThrow(int? value, int min, bool throwIfNull) =>
        ThrowHelper.ThrowIfLessThan(value, min, throwIfNull);

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThan_WhenValueIsLessThanMin_ShouldThrowExactly(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/min pairs where the value is strictly less than the minimum.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanInvalidCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("4 < 5", 4, 5, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue < 0", int.MinValue, 0, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("-1 < 0", -1, 0, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("0 < 1", 0, 1, typeof(ArgumentOutOfRangeException)) };
        yield return new object?[] { new GuardInvalidKat<int>("5 < 6", 5, 6, typeof(ArgumentOutOfRangeException)) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardValidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThan_WhenValueIsGreaterThanOrEqualToMin_ShouldNotThrow(GuardValidKat{Int32})" />.
    /// </summary>
    /// <returns>Value/min pairs where the value is greater than or equal to the minimum.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanValidCases()
    {
        yield return new object?[] { new GuardValidKat<int>("5 == 5", 5, 5) };
        yield return new object?[] { new GuardValidKat<int>("6 > 5", 6, 5) };
        yield return new object?[] { new GuardValidKat<int>("MaxValue > MinValue", int.MaxValue, int.MinValue) };
        yield return new object?[] { new GuardValidKat<int>("0 == 0", 0, 0) };
    }

    /// <summary>
    /// Supplies the <see cref="GuardInvalidKat{Int32}" /> rows used by
    /// <see cref="ThrowIfLessThan_WhenValueIsLessThanMin_ShouldReportParamName(GuardInvalidKat{Int32})" />.
    /// </summary>
    /// <returns>Invalid rows whose <c>ParamName</c> the helper must propagate to the thrown exception.</returns>
    private static IEnumerable<object?[]> ThrowIfLessThanParamNameCases()
    {
        yield return new object?[] { new GuardInvalidKat<int>("4 < 5, paramName=value", 4, 5, typeof(ArgumentOutOfRangeException), "value") };
        yield return new object?[] { new GuardInvalidKat<int>("MinValue < 0, paramName=value", int.MinValue, 0, typeof(ArgumentOutOfRangeException), "value") };
    }

}
