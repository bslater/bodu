// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricStreamAlgorithmSpecification.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Describes the expected observable properties of a single <see cref="SymmetricStreamAlgorithm" /> implementation for
/// use in constructor and behavioural tests.
/// </summary>
public record SymmetricStreamAlgorithmSpecification
{
    /// <summary>
    /// Gets the expected default <see cref="SymmetricStreamAlgorithm.KeySize" /> in bits on a freshly constructed
    /// instance.
    /// </summary>
    public required int DefaultKeySizeBits { get; init; }

    /// <summary>
    /// Gets the expected <see cref="SymmetricStreamAlgorithm.NonceSize" /> in bits. The nonce size is fixed per
    /// algorithm; there is no nonce-size range.
    /// </summary>
    public required int NonceSizeBits { get; init; }

    /// <summary>
    /// Gets the set of key sizes in bits to exercise in parameterised tests. For fixed-key ciphers this contains a
    /// single entry equal to <see cref="DefaultKeySizeBits" />. For multi-size ciphers (such as Salsa20) it enumerates
    /// every discrete legal key size.
    /// </summary>
    public required IReadOnlyList<int> LegalKeySizesBits { get; init; }
}
