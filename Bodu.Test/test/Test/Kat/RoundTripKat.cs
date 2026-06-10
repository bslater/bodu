// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RoundTripKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row that pins a value to its expected wire form, so a serializer can be checked in
/// both directions: serializing <see cref="Value" /> produces <see cref="Wire" />, and deserializing
/// <see cref="Wire" /> reproduces <see cref="Value" />.
/// </summary>
/// <typeparam name="TValue">The type of the in-memory value.</typeparam>
/// <typeparam name="TWire">
/// The type of the serialized wire form (for example a <see cref="string" /> or a byte array).
/// </typeparam>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Value">The in-memory value.</param>
/// <param name="Wire">The expected serialized wire form of <paramref name="Value" />.</param>
public sealed record RoundTripKat<TValue, TWire>(
    string Name,
    TValue Value,
    TWire Wire) : IKat;
