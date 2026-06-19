// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ValidKat{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row whose <see cref="Input" /> is expected to produce a specific
/// <see cref="Expected" /> result when the system under test runs successfully.
/// </summary>
/// <typeparam name="TInput">The type of the input value supplied to the system under test.</typeparam>
/// <typeparam name="TExpected">The type of the expected result.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The input value supplied to the system under test.</param>
/// <param name="Expected">The expected result.</param>
public sealed record ValidKat<TInput, TExpected>(
    string Name,
    TInput Input,
    TExpected Expected) : IKat;
