// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result{T}.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Result<T>
{
    /// <summary>
    /// Gets a value indicating whether this result represents success.
    /// </summary>
    /// <value><see langword="true" /> if a value is present; otherwise, <see langword="false" />.</value>
    public bool IsSuccess =>
        _isSuccess;

    /// <summary>
    /// Gets a value indicating whether this result represents failure.
    /// </summary>
    /// <value><see langword="true" /> if the operation failed; otherwise, <see langword="false" />.</value>
    public bool IsFailure =>
        !_isSuccess;

    /// <summary>
    /// Gets the value carried by a successful result.
    /// </summary>
    /// <value>The value carried by this result.</value>
    /// <exception cref="InvalidOperationException">The result does not represent a success.</exception>
    /// <remarks>
    /// <para>
    /// Prefer <see cref="TryGetValue(out T)" />,
    /// <see cref="Match{TResult}(Func{T, TResult}, Func{ResultError, TResult})" />, or the <c>GetValueOrDefault</c>
    /// overloads over direct access when failure is an expected state; <see cref="Value" /> is intended for call sites
    /// that have already established <see cref="IsSuccess" />.
    /// </para>
    /// </remarks>
    public T Value =>
        _isSuccess ? _value : throw new InvalidOperationException(ResourceStrings.Op_Invalid_ResultNotSuccess);

    /// <summary>
    /// Gets the error carried by a failed result.
    /// </summary>
    /// <value>The <see cref="ResultError" /> describing the failure.</value>
    /// <exception cref="InvalidOperationException">The result does not represent a failure.</exception>
    /// <remarks>
    /// <para>
    /// Prefer <see cref="TryGetError(out ResultError)" /> or
    /// <see cref="Match{TResult}(Func{T, TResult}, Func{ResultError, TResult})" /> over direct access when success is
    /// an expected state; <see cref="Error" /> is intended for call sites that have already established
    /// <see cref="IsFailure" />. On <c>default(Result&lt;T&gt;)</c> it is valid and returns the empty error.
    /// </para>
    /// </remarks>
    public ResultError Error =>
        !_isSuccess ? _error : throw new InvalidOperationException(ResourceStrings.Op_Invalid_ResultNotFailure);
}
