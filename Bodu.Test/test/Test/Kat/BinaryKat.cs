// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row that maps a single <see cref="Input" /> to a single <see cref="Expected" />
/// outcome, used by tests that assert one binary observation per row.
/// </summary>
/// <typeparam name="TInput">The type of the input value supplied to the system under test.</typeparam>
/// <typeparam name="TExpected">The type of the expected outcome.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The input value supplied to the system under test.</param>
/// <param name="Expected">The expected outcome.</param>
/// <remarks>
/// <para>
/// <see cref="BinaryKat{TInput, TExpected}" /> is intentionally distinct from
/// <see cref="ValidKat{TInput, TExpected}" /> to communicate intent at call sites:
/// <see cref="ValidKat{TInput, TExpected}" /> is used where the system under test produces a domain result (parsed
/// value, decoded bytes, computed hash) and <see cref="BinaryKat{TInput, TExpected}" /> is used where the assertion is
/// a single yes/no observation (validation predicate result, capacity-change flag).
/// </para>
/// </remarks>
public sealed record BinaryKat<TInput, TExpected>(
    string Name,
    TInput Input,
    TExpected Expected) : IKat;
