// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GuardInvalidKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a two-operand guard helper (for example,
/// <c>ThrowHelper.ThrowIfEqual&lt;T&gt;(value, other)</c>) where the operand pair is expected to cause the
/// guard to throw <see cref="ExceptionType" />, optionally with a specific <see cref="ParamName" />.
/// </summary>
/// <typeparam name="T">The operand type accepted by the guard helper.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Value">The first operand supplied to the guard.</param>
/// <param name="Other">The second operand supplied to the guard.</param>
/// <param name="ExceptionType">The exact exception type the guard must throw.</param>
/// <param name="ParamName">The expected <c>ParamName</c> when the exception derives from <see cref="ArgumentException" />, or <see langword="null" /> to skip the parameter-name check.</param>
public sealed record GuardInvalidKat<T>(
    string Name,
    T Value,
    T Other,
    Type ExceptionType,
    string? ParamName = null) : IKat;
