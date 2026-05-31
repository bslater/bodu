// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfConditionallyRequiredParameterIsNull.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull" />, when ConditionDoesNotMatchOrValueIsNotNull, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(null, false, true)]
    [DataRow(null, true, false)]
    [DataRow("ok", true, true)]
    [DataRow("ok", false, true)]
    public void ThrowIfConditionallyRequiredParameterIsNull_WhenConditionDoesNotMatchOrValueIsNotNull_ShouldNotThrow(string? value, bool condition, bool matchValue) => ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull" />, when ConditionMatchesAndValueIsNull, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(null, true, true)]
    public void ThrowIfConditionallyRequiredParameterIsNull_WhenConditionMatchesAndValueIsNull_ShouldThrowExactly(string? value, bool condition, bool matchValue)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull{TValue, TCondition}" />
    /// does not throw — and on the ParamName-asserting overload reports nothing — when the requirement is
    /// not activated or the value is non-null.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value being validated.</param>
    /// <param name="condition">The conditional input.</param>
    /// <param name="matchValue">The condition value that activates the requirement.</param>
    [TestMethod]
    [DataRow("condition matches and value non-null", "ok", true, true)]
    [DataRow("condition does not match (null value)", null, true, false)]
    [DataRow("condition does not match (non-null value)", "ok", false, true)]
    public void ThrowIfConditionallyRequiredParameterIsNull_WhenRequirementIsNotViolated_ShouldNotThrowAndReportNothing(
        string testName, string? value, bool condition, bool matchValue) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue, nameof(value), "condition"),
            null,
            null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull{TValue, TCondition}" />
    /// throws <see cref="ArgumentException" /> with <c>ParamName == "value"</c> (never <c>"condition"</c>) when
    /// the condition matches and the value is <see langword="null" />.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value being validated.</param>
    /// <param name="condition">The conditional input.</param>
    /// <param name="matchValue">The condition value that activates the requirement.</param>
    [TestMethod]
    [DataRow("condition matches and value null", null, true, true)]
    public void ThrowIfConditionallyRequiredParameterIsNull_WhenRequirementIsViolated_ShouldThrowOnValue(
        string testName, string? value, bool condition, bool matchValue) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue, nameof(value), "condition"),
            typeof(ArgumentException),
            "value");

}
