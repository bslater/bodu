// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionAssertTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Assertions;

/// <summary>
/// Verifies the behaviour of <see cref="ExceptionAssert" />: the <c>ParamName</c> assertion helper, the
/// guard-contract matrix helper, and the KAT-typed overloads used by guard contract test bases.
/// </summary>
[TestClass]
public sealed class ExceptionAssertTests
{
    /// <summary>
    /// Verifies that <see cref="ExceptionAssert.ThrowsExactlyWithParamName{TException}(Action, string?)" />
    /// returns the captured exception when the type and <c>ParamName</c> match.
    /// </summary>
    [TestMethod]
    public void ThrowsExactlyWithParamName_WhenTypeAndParamNameMatch_ShouldReturnCapturedException()
    {
        ArgumentNullException ex = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () => throw new ArgumentNullException("source"),
            "source");

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="ExceptionAssert.ThrowsExactlyWithParamName{TException}(Action, string?)" />
    /// fails the test (via an inner <see cref="AssertFailedException" />) when <c>ParamName</c> does not
    /// match the expected value.
    /// </summary>
    [TestMethod]
    public void ThrowsExactlyWithParamName_WhenParamNameMismatches_ShouldFail()
    {
        Assert.ThrowsExactly<AssertFailedException>(() =>
        {
            ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
                () => throw new ArgumentNullException("source"),
                "destination");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ExceptionAssert.AssertGuard(string, Action, Type?, string?)" /> accepts
    /// the no-throw path when <c>expectedExceptionType</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void AssertGuard_WhenNoExceptionExpectedAndActionDoesNotThrow_ShouldPass()
    {
        ExceptionAssert.AssertGuard(
            "happy path",
            () => { },
            expectedExceptionType: null,
            expectedParamName: null);
    }

    /// <summary>
    /// Verifies that <see cref="ExceptionAssert.AssertGuard(string, Action, Type?, string?)" /> accepts
    /// the throw path when the expected exception type and <c>ParamName</c> match.
    /// </summary>
    [TestMethod]
    public void AssertGuard_WhenExceptionMatchesExpectedTypeAndParamName_ShouldPass()
    {
        ExceptionAssert.AssertGuard(
            "out of range",
            () => throw new ArgumentOutOfRangeException("value"),
            typeof(ArgumentOutOfRangeException),
            "value");
    }

    /// <summary>
    /// Verifies that <see cref="ExceptionAssert.AssertGuard(string, Action, Type?, string?)" /> fails when
    /// no exception is thrown but one was expected.
    /// </summary>
    [TestMethod]
    public void AssertGuard_WhenNoExceptionThrownButOneExpected_ShouldFail()
    {
        Assert.ThrowsExactly<AssertFailedException>(() =>
        {
            ExceptionAssert.AssertGuard(
                "missing throw",
                () => { },
                typeof(ArgumentNullException),
                expectedParamName: null);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ExceptionAssert.AssertGuard(string, Action, Type?, string?)" /> fails when
    /// a different exception type is thrown.
    /// </summary>
    [TestMethod]
    public void AssertGuard_WhenWrongExceptionTypeThrown_ShouldFail()
    {
        Assert.ThrowsExactly<AssertFailedException>(() =>
        {
            ExceptionAssert.AssertGuard(
                "wrong type",
                () => throw new InvalidOperationException(),
                typeof(ArgumentNullException),
                expectedParamName: null);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="GuardValidKat{T}" /> overload accepts the row by invoking the helper
    /// with <c>(value, other, null)</c> and confirming the no-throw outcome.
    /// </summary>
    [TestMethod]
    public void AssertGuard_WithGuardValidKat_ShouldInvokeHelperAndPass()
    {
        var invoked = false;
        GuardValidKat<int> kat = new("distinct values", 1, 2);

        ExceptionAssert.AssertGuard(kat, (value, other, _) =>
        {
            invoked = true;
            Assert.AreNotEqual(value, other);
        });

        Assert.IsTrue(invoked);
    }

    /// <summary>
    /// Verifies that the <see cref="GuardInvalidKat{T}" /> overload forwards the operand pair and asserts
    /// the expected exception type and <c>ParamName</c>.
    /// </summary>
    [TestMethod]
    public void AssertGuard_WithGuardInvalidKat_ShouldInvokeHelperAndAssertThrow()
    {
        GuardInvalidKat<int> kat = new("equal values", 5, 5, typeof(ArgumentOutOfRangeException), "value");

        ExceptionAssert.AssertGuard(kat, (value, other, paramName) =>
        {
            if (value == other) throw new ArgumentOutOfRangeException(paramName);
        });
    }
}
