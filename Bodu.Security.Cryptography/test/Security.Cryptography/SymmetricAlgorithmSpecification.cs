// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SymmetricAlgorithmSpecification.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Describes the expected observable properties of a single <typeparamref name="TAlgorithm" /> implementation for use in
/// constructor and behavioural tests.
/// </summary>
public sealed record SymmetricAlgorithmSpecification
{
    /// <summary>Gets the expected <see cref="SymmetricAlgorithm.BlockSize" /> in bits.</summary>
    public required int BlockSizeBits { get; init; }

    /// <summary>Gets the expected default <see cref="SymmetricAlgorithm.KeySize" /> in bits on a freshly constructed instance.</summary>
    public required int DefaultKeySizeBits { get; init; }

    /// <summary>
    /// Gets the set of key sizes in bits to exercise in parameterised encryptor and decryptor tests. For algorithms with a
    /// fixed key size, this should list each discrete legal size. For algorithms with a broad key-size range, a representative
    /// subset covering the minimum, maximum, and at least one intermediate size is sufficient.
    /// </summary>
    public required IReadOnlyList<int> LegalKeySizesBits { get; init; }

    /// <summary>
    /// Gets the expected default <see cref="SymmetricAlgorithm.Mode" /> on a freshly constructed instance.
    /// Defaults to <see cref="CipherMode.CBC" />.
    /// </summary>
    public CipherMode DefaultMode { get; init; } = CipherMode.CBC;

    /// <summary>
    /// Gets the expected default <see cref="SymmetricAlgorithm.Padding" /> on a freshly constructed instance.
    /// Defaults to <see cref="PaddingMode.PKCS7" />.
    /// </summary>
    public PaddingMode DefaultPadding { get; init; } = PaddingMode.PKCS7;
}
