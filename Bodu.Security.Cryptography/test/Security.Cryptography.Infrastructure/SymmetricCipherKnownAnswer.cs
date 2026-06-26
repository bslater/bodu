// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricCipherKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Serves as the base for symmetric-cipher known-answer test vectors — a plaintext / ciphertext pair produced under the
/// inherited key. Block, stream, and AEAD vectors all extend this root.
/// </summary>
public abstract record SymmetricCipherKnownAnswer : KeyedKnownAnswer
{
    /// <summary>
    /// Gets the plaintext input — the value fed to encryption and the value expected from decrypting
    /// <see cref="Ciphertext" />.
    /// </summary>
    public required byte[] Plaintext { get; init; }

    /// <summary>
    /// Gets the ciphertext expected from encrypting <see cref="Plaintext" />, which is also the value fed to decryption
    /// during the round-trip assertion.
    /// </summary>
    public required byte[] Ciphertext { get; init; }
}
