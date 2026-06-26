// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Serves as the base for asymmetric known-answer test vectors — key agreement, signature, key encapsulation, key
/// generation, and key validation. Adds the optional <see cref="ParameterSet" /> designator shared by the post-quantum
/// families.
/// </summary>
/// <remarks>
/// The <see cref="ParameterSet" /> identifies the parameter set a vector belongs to, for example <c>"ML-KEM-768"</c> or
/// <c>"ML-DSA-65"</c>, and is <see langword="null" /> for fixed-parameter algorithms such as X25519 and Ed25519. Loaders
/// that read a single file spanning multiple parameter sets filter on this value.
/// </remarks>
public abstract record AsymmetricKnownAnswer : CryptoKnownAnswer
{
    /// <summary>
    /// Gets the parameter-set designator this vector belongs to, or <see langword="null" /> for fixed-parameter algorithms.
    /// </summary>
    /// <value>A parameter-set name such as <c>"ML-KEM-768"</c>, or <see langword="null" />.</value>
    public string? ParameterSet { get; init; }
}
