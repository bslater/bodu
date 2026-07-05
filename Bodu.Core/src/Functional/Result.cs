// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Functional;

/// <summary>
/// Represents the outcome of an operation that produces no value: either <c>Success</c>, or <c>Failure(error)</c>
/// carrying a <see cref="ResultError" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Result" /> makes failure explicit in a signature without resorting to exceptions for expected error
/// paths. Collapse to a final value with <see cref="Match{TResult}(Func{TResult}, Func{ResultError, TResult})" />, or
/// branch with <see cref="Match(Action, Action{ResultError})" />. The type also hosts the factory surface for the whole
/// family, including the generic <see cref="Result{T}" /> factories <see cref="Success{T}(T)" /> and
/// <see cref="Failure{T}(ResultError)" />.
/// </para>
/// <para>
/// <c>default(Result)</c> equals a failure carrying an empty <see cref="ResultError" /> — a result that was never
/// assigned behaves exactly like an explicit failure, so the type is total and safe to use as a field or array element
/// without initialization. <see cref="Error" /> on <c>default(Result)</c> is valid and returns the empty error.
/// </para>
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public readonly partial struct Result
    : System.IEquatable<Result>
{
    /// <summary>Indicates whether this instance represents success. <see langword="false" /> for <c>default(Result)</c>.</summary>
    private readonly bool _isSuccess;

    /// <summary>The error when <see cref="_isSuccess" /> is <see langword="false" />; otherwise <c>default(ResultError)</c> and never observed.</summary>
    private readonly ResultError _error;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result" /> struct with the specified state.
    /// </summary>
    /// <param name="isSuccess">Whether the result represents success.</param>
    /// <param name="error">The error carried when <paramref name="isSuccess" /> is <see langword="false" />.</param>
    private Result(bool isSuccess, ResultError error)
    {
        _isSuccess = isSuccess;
        _error = error;
    }

    /// <summary>
    /// Gets a value indicating whether this result represents success.
    /// </summary>
    /// <value><see langword="true" /> if the operation succeeded; otherwise, <see langword="false" />.</value>
    public bool IsSuccess =>
        _isSuccess;

    /// <summary>
    /// Gets a value indicating whether this result represents failure.
    /// </summary>
    /// <value><see langword="true" /> if the operation failed; otherwise, <see langword="false" />.</value>
    public bool IsFailure =>
        !_isSuccess;

    /// <summary>
    /// Gets the error carried by a failed result.
    /// </summary>
    /// <value>The <see cref="ResultError" /> describing the failure.</value>
    /// <exception cref="InvalidOperationException">The result does not represent a failure.</exception>
    /// <remarks>
    /// <para>
    /// Prefer <see cref="TryGetError(out ResultError)" /> or
    /// <see cref="Match{TResult}(Func{TResult}, Func{ResultError, TResult})" /> over direct access when success is an
    /// expected state; <see cref="Error" /> is intended for call sites that have already established
    /// <see cref="IsFailure" />. On <c>default(Result)</c> it is valid and returns the empty error.
    /// </para>
    /// </remarks>
    public ResultError Error =>
        !_isSuccess ? _error : throw new InvalidOperationException(ResourceStrings.Op_Invalid_ResultNotFailure);

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    /// <param name="left">The first result to compare.</param>
    /// <param name="right">The second result to compare.</param>
    /// <returns>
    /// <see langword="true" /> if both are successes, or both are failures carrying equal errors; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator ==(Result left, Result right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two results are not equal.
    /// </summary>
    /// <param name="left">The first result to compare.</param>
    /// <param name="right">The second result to compare.</param>
    /// <returns><see langword="true" /> if the results differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Result left, Result right) =>
        !left.Equals(right);

    /// <summary>
    /// Attempts to retrieve the error carried by a failed result.
    /// </summary>
    /// <param name="error">
    /// When this method returns, the error if the result represents failure; otherwise the default error.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the result represents failure; otherwise, <see langword="false" />.
    /// </returns>
    public bool TryGetError(out ResultError error)
    {
        error = _error;
        return !_isSuccess;
    }

    /// <summary>
    /// Collapses the result to a single value by invoking the matching branch.
    /// </summary>
    /// <typeparam name="TResult">The type produced by both branches.</typeparam>
    /// <param name="onSuccess">The factory invoked when the result represents success.</param>
    /// <param name="onFailure">The projection invoked with the error when the result represents failure.</param>
    /// <returns>The value produced by the invoked branch.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
    /// </exception>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var label = result.Match(() => "ok", error => $"failed: {error}");
    ///]]>
    /// </code>
    /// </example>
    public TResult Match<TResult>(Func<TResult> onSuccess, Func<ResultError, TResult> onFailure)
    {
        ThrowHelper.ThrowIfNull(onSuccess);
        ThrowHelper.ThrowIfNull(onFailure);

        return _isSuccess ? onSuccess() : onFailure(_error);
    }

    /// <summary>
    /// Invokes the matching action for the state of the result.
    /// </summary>
    /// <param name="onSuccess">The action invoked when the result represents success.</param>
    /// <param name="onFailure">The action invoked with the error when the result represents failure.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="onSuccess" /> or <paramref name="onFailure" /> is <see langword="null" />.
    /// </exception>
    public void Match(Action onSuccess, Action<ResultError> onFailure)
    {
        ThrowHelper.ThrowIfNull(onSuccess);
        ThrowHelper.ThrowIfNull(onFailure);

        if (_isSuccess)
            onSuccess();
        else
            onFailure(_error);
    }

    /// <summary>
    /// Determines whether the specified object is equal to the current result.
    /// </summary>
    /// <param name="obj">
    /// The object to compare. Only another <see cref="Result" /> can be equal; all other types, including
    /// <see langword="null" />, return <see langword="false" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Result" /> equal to this instance; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Result other && Equals(other);

    /// <summary>
    /// Determines whether the specified result is equal to the current instance.
    /// </summary>
    /// <param name="other">The result to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if both are successes, or both are failures whose errors compare equal using
    /// <see cref="ResultError.Equals(ResultError)" />; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Result other) =>
        _isSuccess
            ? other._isSuccess
            : !other._isSuccess && _error.Equals(other._error);

    /// <summary>
    /// Returns a hash code for the current instance consistent with <see cref="Equals(Result)" />.
    /// </summary>
    /// <returns><c>1</c> for success; otherwise the error's hash code.</returns>
    public override int GetHashCode() =>
        _isSuccess ? 1 : _error.GetHashCode();

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    /// <returns><c>"Success"</c> for success; otherwise <c>"Failure(error)"</c>.</returns>
    public override string ToString() =>
        _isSuccess ? "Success" : $"Failure({_error})";
}
