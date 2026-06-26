// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyGenKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single key-generation known-answer test (KAT) vector for the post-quantum families: a seed and the
/// expected encoded key pair it must deterministically produce.
/// </summary>
/// <remarks>
/// <see cref="Seed" /> is the full private seed consumed by key generation: the 32-byte ξ for ML-DSA, or the
/// concatenated <c>d ‖ z</c> (64 bytes) for ML-KEM. <see cref="ExpectedPublicKey" /> / <see cref="ExpectedPrivateKey" />
/// are the encapsulation/decapsulation keys for ML-KEM and the public/private keys for ML-DSA. Vectors are constructed
/// by the per-family ACVP loaders, which map each file's field set onto this shape.
/// </remarks>
public sealed record KeyGenKnownAnswer : AsymmetricKnownAnswer
{
    /// <summary>
    /// Gets the full private seed consumed by key generation (ξ for ML-DSA, <c>d ‖ z</c> for ML-KEM).
    /// </summary>
    public required byte[] Seed { get; init; }

    /// <summary>
    /// Gets the expected encoded public key (the encapsulation key for ML-KEM).
    /// </summary>
    public required byte[] ExpectedPublicKey { get; init; }

    /// <summary>
    /// Gets the expected encoded private key (the decapsulation key for ML-KEM).
    /// </summary>
    public required byte[] ExpectedPrivateKey { get; init; }
}
