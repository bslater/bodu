// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Functional;

/// <summary>
/// Represents the outcome of an operation that produces a value: either <c>Success(value)</c> carrying a non-<see langword="null" />
/// value of type <typeparamref name="T" />, or <c>Failure(error)</c> carrying a <see cref="ResultError" />.
/// </summary>
/// <typeparam name="T">The type of the value carried on success.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Result{T}" /> makes failure explicit in a signature without resorting to exceptions for expected error
/// paths. Chain transformations with <see cref="Map{TResult}(Func{T, TResult})" /> and
/// <see cref="Bind{TResult}(Func{T, Result{TResult}})" /> — the error propagates untouched past every combinator — and
/// collapse to a final value with <see cref="Match{TResult}(Func{T, TResult}, Func{ResultError, TResult})" /> or the
/// <c>GetValueOrDefault</c> overloads. Instances are created through the factories on the non-generic
/// <see cref="Result" /> type: <see cref="Result.Success{T}(T)" /> and <see cref="Result.Failure{T}(ResultError)" />.
/// </para>
/// <para>
/// <c>default(Result&lt;T&gt;)</c> equals a failure carrying an empty <see cref="ResultError" /> — a result that was
/// never assigned behaves exactly like an explicit failure, so the type is total and safe to use as a field or array
/// element without initialization. <see cref="Error" /> on <c>default(Result&lt;T&gt;)</c> is valid and returns the
/// empty error.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Result<T>
{
    /// <summary>Indicates whether this instance carries a value. <see langword="false" /> for <c>default(Result&lt;T&gt;)</c>.</summary>
    private readonly bool _isSuccess;

    /// <summary>The value when <see cref="_isSuccess" /> is <see langword="true" />; otherwise <c>default(T)</c> and never observed.</summary>
    private readonly T _value;

    /// <summary>The error when <see cref="_isSuccess" /> is <see langword="false" />; otherwise <c>default(ResultError)</c> and never observed.</summary>
    private readonly ResultError _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}" /> struct with the specified state.
    /// </summary>
    /// <param name="isSuccess">Whether the result represents success.</param>
    /// <param name="value">The value carried when <paramref name="isSuccess" /> is <see langword="true" />.</param>
    /// <param name="error">The error carried when <paramref name="isSuccess" /> is <see langword="false" />.</param>
    private Result(bool isSuccess, T value, ResultError error)
    {
        _isSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result carrying the specified value.
    /// </summary>
    /// <param name="value">
    /// The value to store. The caller has already validated it is not <see langword="null" />.
    /// </param>
    /// <returns>A successful <see cref="Result{T}" />.</returns>
    internal static Result<T> CreateSuccess(T value) =>
        new Result<T>(true, value, default);

    /// <summary>
    /// Creates a failed result carrying the specified error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}" />.</returns>
    internal static Result<T> CreateFailure(ResultError error) =>
        new Result<T>(false, default!, error);
}
