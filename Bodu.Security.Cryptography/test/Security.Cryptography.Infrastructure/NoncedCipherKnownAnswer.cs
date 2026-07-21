// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NoncedCipherKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Serves as the base for nonce-based symmetric-cipher known-answer test vectors — stream ciphers and AEAD
/// constructions — adding the per-vector nonce to the plaintext / ciphertext pair.
/// </summary>
public abstract record NoncedCipherKnownAnswer
    : SymmetricCipherKnownAnswer
{
    /// <summary>
    /// Gets the nonce (initialization value) used for this vector.
    /// </summary>
    public required byte[] Nonce { get; init; }
}
