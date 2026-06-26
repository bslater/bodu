// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Serves as the base for secret-key known-answer test vectors — those whose computation is parameterized by a key.
/// Message-digest, key-derivation, and symmetric-cipher vectors all extend this root.
/// </summary>
/// <remarks>
/// The <see cref="Key" /> is optional: a <see langword="null" /> value defers to the variant's default key from the
/// owning specification, an empty array selects the canonical unkeyed mode for algorithms that accept one (Skein,
/// BLAKE2), and a populated array supplies the per-row key directly.
/// </remarks>
public abstract record KeyedKnownAnswer : CryptoKnownAnswer
{
    /// <summary>
    /// Gets the per-row key, or <see langword="null" /> to defer to the variant default supplied by the specification.
    /// </summary>
    /// <value>The key bytes, an empty array for the unkeyed sentinel, or <see langword="null" /> for the variant default.</value>
    public byte[]? Key { get; init; }
}
