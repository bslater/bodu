// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamCipherKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a known-answer test vector for a stream cipher — a keystream or an encryption vector keyed by a nonce and
/// an optional initial block counter.
/// </summary>
/// <remarks>
/// When <see cref="IsKeystream" /> is <see langword="true" />, the vector pins the raw keystream:
/// <see cref="SymmetricCipherKnownAnswer.Ciphertext" /> carries the expected keystream bytes and
/// <see cref="SymmetricCipherKnownAnswer.Plaintext" /> is empty. Otherwise the vector is an encryption trial mapping
/// <see cref="SymmetricCipherKnownAnswer.Plaintext" /> to <see cref="SymmetricCipherKnownAnswer.Ciphertext" />.
/// </remarks>
public sealed record StreamCipherKnownAnswer : NoncedCipherKnownAnswer
{
    /// <summary>
    /// Gets the initial block counter applied before generating keystream. Defaults to <c>0</c>.
    /// </summary>
    public uint Counter { get; init; }

    /// <summary>
    /// Gets a value indicating whether this vector pins the raw keystream rather than a plaintext / ciphertext mapping.
    /// </summary>
    public bool IsKeystream { get; init; }
}
