// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionAssert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Test.Assertions;

/// <summary>
/// Provides reusable exception assertion helpers for MSTest test projects, including the guard-contract
/// matrix helper used across <c>ThrowHelper</c>-style tests and a strongly typed <c>ParamName</c> assertion.
/// </summary>
public static class ExceptionAssert
{
    /// <summary>
    /// Asserts that <paramref name="action" /> throws <typeparamref name="TException" /> exactly, with the
    /// expected <see cref="ArgumentException.ParamName" />, and returns the captured exception for further
    /// inspection.
    /// </summary>
    /// <typeparam name="TException">An <see cref="ArgumentException" />-derived exception type.</typeparam>
    /// <param name="action">The invocation under test.</param>
    /// <param name="expectedParamName">The expected value of <see cref="ArgumentException.ParamName" /> on the thrown exception.</param>
    /// <returns>The captured exception, so callers may make additional assertions if needed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="action" /> is <see langword="null" />.</exception>
    public static TException ThrowsExactlyWithParamName<TException>(
        Action action,
        string? expectedParamName)
        where TException : ArgumentException
    {
        ArgumentNullException.ThrowIfNull(action);

        TException exception = Assert.ThrowsExactly<TException>(action);
        Assert.AreEqual(expectedParamName, exception.ParamName);
        return exception;
    }

    /// <summary>
    /// Asserts the standard guard contract for a single helper invocation: the action either completes
    /// silently (when <paramref name="expectedExceptionType" /> is <see langword="null" />) or throws
    /// exactly the requested exception type, optionally with a specific
    /// <see cref="ArgumentException.ParamName" />.
    /// </summary>
    /// <param name="testName">A descriptive label included in assertion failure messages so a failing row is identifiable in <c>[DynamicData]</c> or <c>[DataRow]</c> output.</param>
    /// <param name="act">The guard invocation under test.</param>
    /// <param name="expectedExceptionType">The exact <see cref="Exception" />-derived type that the guard must throw, or <see langword="null" /> if the invocation must complete without throwing.</param>
    /// <param name="expectedParamName">When the expected exception derives from <see cref="ArgumentException" />, the value <see cref="ArgumentException.ParamName" /> must carry; otherwise ignored.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="act" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// The helper deliberately accepts a base <see cref="Exception" /> type rather than a generic so that
    /// data rows can mix multiple expected exception types in a single matrix.
    /// </para>
    /// </remarks>
    public static void AssertGuard(
        string testName,
        Action act,
        Type? expectedExceptionType,
        string? expectedParamName)
    {
        ArgumentNullException.ThrowIfNull(act);

        if (expectedExceptionType is null)
        {
            act();
            return;
        }

        Exception? captured = null;
        try
        {
            act();
        }
        catch (Exception ex)
        {
            captured = ex;
        }

        Assert.IsNotNull(captured, $"{testName}: expected {expectedExceptionType.Name} but no exception was thrown.");
        Assert.AreEqual(expectedExceptionType, captured.GetType(), $"{testName}: wrong exception type.");

        if (expectedParamName is not null)
        {
            Assert.IsInstanceOfType<ArgumentException>(
                captured,
                $"{testName}: expected an ArgumentException-derived type to validate ParamName.");

            Assert.AreEqual(
                expectedParamName,
                ((ArgumentException)captured).ParamName,
                $"{testName}: wrong ParamName.");
        }
    }

    /// <summary>
    /// Asserts that <paramref name="invoke" /> applied to the operands in <paramref name="kat" /> completes
    /// silently, with the row's <see cref="IKat.Name" /> threaded into any failure message.
    /// </summary>
    /// <typeparam name="T">The operand type accepted by the guard helper.</typeparam>
    /// <param name="kat">The KAT row carrying the operand pair expected to pass validation.</param>
    /// <param name="invoke">A delegate that invokes the guard helper with <c>(value, other, paramName)</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="kat" /> or <paramref name="invoke" /> is <see langword="null" />.</exception>
    public static void AssertGuard<T>(GuardValidKat<T> kat, Action<T, T, string?> invoke)
    {
        ArgumentNullException.ThrowIfNull(kat);
        ArgumentNullException.ThrowIfNull(invoke);

        AssertGuard(
            kat.Name,
            () => invoke(kat.Value, kat.Other, null),
            expectedExceptionType: null,
            expectedParamName: null);
    }

    /// <summary>
    /// Asserts that <paramref name="invoke" /> applied to the operands in <paramref name="kat" /> throws
    /// <see cref="GuardInvalidKat{T}.ExceptionType" />, optionally with <see cref="GuardInvalidKat{T}.ParamName" />,
    /// with the row's <see cref="IKat.Name" /> threaded into any failure message.
    /// </summary>
    /// <typeparam name="T">The operand type accepted by the guard helper.</typeparam>
    /// <param name="kat">The KAT row carrying the operand pair expected to fail validation.</param>
    /// <param name="invoke">A delegate that invokes the guard helper with <c>(value, other, paramName)</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="kat" /> or <paramref name="invoke" /> is <see langword="null" />.</exception>
    public static void AssertGuard<T>(GuardInvalidKat<T> kat, Action<T, T, string?> invoke)
    {
        ArgumentNullException.ThrowIfNull(kat);
        ArgumentNullException.ThrowIfNull(invoke);

        AssertGuard(
            kat.Name,
            () => invoke(kat.Value, kat.Other, kat.ParamName),
            kat.ExceptionType,
            kat.ParamName);
    }
}
