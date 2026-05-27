// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RoundTripKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row that asserts a value survives a serialise/deserialise (or encode/decode) cycle
/// producing the same value and the same wire representation.
/// </summary>
/// <typeparam name="TValue">The type of the in-memory value being round-tripped.</typeparam>
/// <typeparam name="TWire">
/// The type of the on-the-wire representation, for example <see cref="string" /> or <c>byte[]</c>.
/// </typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Value">The expected in-memory value.</param>
/// <param name="Wire">The expected on-the-wire representation.</param>
/// <param name="Options">
/// An optional options object that configures the serialiser, encoder, or formatter under test.
/// </param>
public sealed record RoundTripKat<TValue, TWire>(
    string Name,
    TValue Value,
    TWire Wire,
    object? Options = null) : IKat;
