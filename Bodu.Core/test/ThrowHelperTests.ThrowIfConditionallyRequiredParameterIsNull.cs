// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfConditionallyRequiredParameterIsNull.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

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
    public void ThrowIfConditionallyRequiredParameterIsNull_WhenConditionMatchesAndValueIsNull_ShouldThrowArgumentException(string? value, bool condition, bool matchValue)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue);
        });
    }
    /// <summary>
    /// Verifies the multi-parameter contract for
    /// <see cref="ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull{TValue, TCondition}" />: when the
    /// guard fails because <c>value</c> is <see langword="null" /> under a matching condition, ParamName must
    /// reference the <c>value</c> parameter, not the condition parameter.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value being validated.</param>
    /// <param name="condition">The conditional input.</param>
    /// <param name="matchValue">The condition value that activates the requirement.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("condition matches and value null → throw on value", null, true, true, true)]
    [DataRow("condition matches and value non-null → pass", "ok", true, true, false)]
    [DataRow("condition does not match → pass even with null value", null, true, false, false)]
    [DataRow("condition does not match → pass even with non-null value", "ok", false, true, false)]
    public void ThrowIfConditionallyRequiredParameterIsNull_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, string? value, bool condition, bool matchValue, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfConditionallyRequiredParameterIsNull(value, condition, matchValue, nameof(value), "condition"),
            expected,
            expectedParam);
    }

}
