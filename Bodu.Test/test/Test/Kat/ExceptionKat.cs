// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExceptionKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row that asserts the system under test throws <see cref="ExceptionType" /> for a
/// given <see cref="Input" />, optionally with a specific <see cref="ParamName" /> or message substring.
/// </summary>
/// <typeparam name="TInput">The type of the input value supplied to the system under test.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The input value supplied to the system under test.</param>
/// <param name="ExceptionType">The exact exception type the system under test must throw.</param>
/// <param name="ParamName">
/// The expected <c>ParamName</c> when the exception derives from <see cref="ArgumentException" />, or
/// <see langword="null" /> to skip the parameter-name check.
/// </param>
/// <param name="MessageContains">
/// An optional substring the exception message must contain, or <see langword="null" /> to skip the message check.
/// </param>
/// <remarks>
/// <para>
/// <see cref="ExceptionKat{TInput}" /> exists alongside <see cref="InvalidKat{TInput}" /> to express intent at call
/// sites: use <see cref="InvalidKat{TInput}" /> when the row models a malformed domain input that should be rejected
/// (parser, encoder, validator), and use <see cref="ExceptionKat{TInput}" /> when the row models a non-domain failure
/// mode (disposed instance, cancellation, exceeded capacity).
/// </para>
/// </remarks>
public sealed record ExceptionKat<TInput>(
    string Name,
    TInput Input,
    Type ExceptionType,
    string? ParamName = null,
    string? MessageContains = null) : IKat;
