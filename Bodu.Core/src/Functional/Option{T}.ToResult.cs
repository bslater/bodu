// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Option{T}.ToResult.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public readonly partial struct Option<T>
{
    /// <summary>
    /// Converts the option to a result, using the specified error when no value is present.
    /// </summary>
    /// <param name="error">The error carried by the failure produced when this option is <c>None</c>.</param>
    /// <returns><c>Success(value)</c> when a value is present; otherwise <c>Failure(error)</c>.</returns>
    /// <remarks>
    /// <para>
    /// This is the bridge from the absence-shaped <see cref="Option{T}" /> onto the failure-shaped
    /// <see cref="Result{T}" /> railway: the caller names the error that absence represents. The inverse direction is
    /// <see cref="Result{T}.ToOption" />, which discards the error.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var found = Option<string>.Some("value").ToResult("missing"); // Success(value)
    /// var missing = Option<string>.None.ToResult("missing");        // Failure(missing)
    ///]]>
    /// </code>
    /// </example>
    public Result<T> ToResult(ResultError error) =>
        _hasValue ? Result.Success(_value) : Result.Failure<T>(error);
}
