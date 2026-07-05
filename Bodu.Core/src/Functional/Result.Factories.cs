// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Result.Factories.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Result
{
    /// <summary>
    /// Creates a result representing success.
    /// </summary>
    /// <returns>A <see cref="Result" /> whose <see cref="IsSuccess" /> is <see langword="true" />.</returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var ok = Result.Success();                                    // Success
    /// var failed = Result.Failure(ResultError.FromMessage("boom")); // Failure(boom)
    ///]]>
    /// </code>
    /// </example>
    public static Result Success() =>
        new Result(true, default);

    /// <summary>
    /// Creates a result representing failure with the specified error.
    /// </summary>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>A <see cref="Result" /> whose <see cref="IsFailure" /> is <see langword="true" />.</returns>
    public static Result Failure(ResultError error) =>
        new Result(false, error);

    /// <summary>
    /// Creates a successful result carrying the specified non-<see langword="null" /> value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to wrap. Must not be <see langword="null" />.</param>
    /// <returns>
    /// A <see cref="Result{T}" /> whose <see cref="Result{T}.IsSuccess" /> is <see langword="true" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// This is a strict lift: a <see langword="null" /> argument is rejected because a successful result must always
    /// carry a value, mirroring <see cref="Option{T}.Some(T)" />.
    /// </para>
    /// </remarks>
    public static Result<T> Success<T>(T value)
    {
        ThrowHelper.ThrowIfNull(value);

        return Result<T>.CreateSuccess(value);
    }

    /// <summary>
    /// Creates a failed result of the specified value type carrying the specified error.
    /// </summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="error">The error describing the failure.</param>
    /// <returns>
    /// A <see cref="Result{T}" /> whose <see cref="Result{T}.IsFailure" /> is <see langword="true" />.
    /// </returns>
    public static Result<T> Failure<T>(ResultError error) =>
        Result<T>.CreateFailure(error);
}
