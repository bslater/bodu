// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmSpecification.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Describes the expected observable properties of a single <see cref="System.Security.Cryptography.AsymmetricAlgorithm" />
/// implementation for use in constructor and behavioural tests.
/// </summary>
public record AsymmetricAlgorithmSpecification
{
    /// <summary>
    /// Gets the expected <see cref="System.Security.Cryptography.AsymmetricAlgorithm.KeySize" /> value of a freshly
    /// constructed instance. For the Curve25519 family this is the key length in bits (256); for the post-quantum
    /// families it is the FIPS parameter-set designator (512/768/1024 or 44/65/87).
    /// </summary>
    public required int KeySizeDesignator { get; init; }

    /// <summary>
    /// Gets the expected <see cref="System.Security.Cryptography.AsymmetricAlgorithm.KeyExchangeAlgorithm" /> value,
    /// or <see langword="null" /> for signature-only algorithms.
    /// </summary>
    public required string? KeyExchangeAlgorithmName { get; init; }

    /// <summary>
    /// Gets the expected <see cref="System.Security.Cryptography.AsymmetricAlgorithm.SignatureAlgorithm" /> value,
    /// or <see langword="null" /> for key-agreement and encapsulation algorithms.
    /// </summary>
    public required string? SignatureAlgorithmName { get; init; }

    /// <summary>
    /// Gets the exact size, in bytes, of the raw private key accepted by the import adapter and returned by the
    /// export adapter (the decapsulation key for KEMs).
    /// </summary>
    public required int PrivateKeySizeBytes { get; init; }

    /// <summary>
    /// Gets the exact size, in bytes, of the raw public key accepted by the import adapter and returned by the
    /// export adapter (the encapsulation key for KEMs).
    /// </summary>
    public required int PublicKeySizeBytes { get; init; }
}
