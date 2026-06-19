// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumerableKat{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Represents a known-answer test row for extension methods over <see cref="IEnumerable{T}" /> where the source may be
/// empty, singleton, multi-item, duplicated, lazy, or throwing.
/// </summary>
/// <typeparam name="TInput">The element type of the source sequence.</typeparam>
/// <typeparam name="TExpected">The type of the expected result produced by the extension under test.</typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Source">The source sequence supplied to the extension under test.</param>
/// <param name="Expected">The expected result.</param>
public sealed record EnumerableKat<TInput, TExpected>(
    string Name,
    IEnumerable<TInput> Source,
    TExpected Expected) : IKat;
