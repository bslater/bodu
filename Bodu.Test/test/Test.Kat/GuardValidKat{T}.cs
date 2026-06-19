// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GuardValidKat{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a two-operand guard helper (for example,
/// <c>ThrowHelper.ThrowIfEqual&lt;T&gt;(value, other)</c>) where the operand pair is expected to pass validation
/// without throwing.
/// </summary>
/// <typeparam name="T">The operand type accepted by the guard helper.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Value">The first operand supplied to the guard.</param>
/// <param name="Other">The second operand supplied to the guard.</param>
public sealed record GuardValidKat<T>(
    string Name,
    T Value,
    T Other) : IKat;
