// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KdfKnownAnswerVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single password-hashing KDF known-answer test (KAT) vector — the password, salt, and cost parameters,
/// together with the expected derived bytes — for the Argon2 and scrypt families.
/// </summary>
/// <remarks>
/// Argon2 vectors populate <see cref="Variant" />, <see cref="Memory" />, <see cref="Iterations" />,
/// <see cref="Parallelism" />, <see cref="Version" />, and optionally <see cref="Secret" /> /
/// <see cref="AssociatedData" />; scrypt vectors populate <see cref="CostN" />, <see cref="BlockSizeR" />, and
/// <see cref="Parallelism" />.
/// </remarks>
public sealed record KdfKnownAnswerVector
    : IKat
{
    /// <summary>
    /// Gets the human-readable name identifying the vector and its source.
    /// </summary>
    /// <returns>The vector name.</returns>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the Argon2 variant for an Argon2 vector (<c>"d"</c>, <c>"i"</c>, or <c>"id"</c>); ignored for scrypt.
    /// </summary>
    /// <returns>The variant identifier.</returns>
    public string Variant { get; init; } = "id";

    /// <summary>
    /// Gets the password input.
    /// </summary>
    /// <returns>The password bytes.</returns>
    public required byte[] Password { get; init; }

    /// <summary>
    /// Gets the salt input.
    /// </summary>
    /// <returns>The salt bytes.</returns>
    public required byte[] Salt { get; init; }

    /// <summary>
    /// Gets the optional Argon2 secret key, or <see langword="null" /> when none is used.
    /// </summary>
    /// <returns>The secret bytes, or <see langword="null" />.</returns>
    public byte[]? Secret { get; init; }

    /// <summary>
    /// Gets the optional Argon2 associated data, or <see langword="null" /> when none is used.
    /// </summary>
    /// <returns>The associated-data bytes, or <see langword="null" />.</returns>
    public byte[]? AssociatedData { get; init; }

    /// <summary>
    /// Gets the Argon2 memory cost, in kibibytes.
    /// </summary>
    /// <returns>The memory cost.</returns>
    public int Memory { get; init; }

    /// <summary>
    /// Gets the Argon2 iteration count.
    /// </summary>
    /// <returns>The iteration count.</returns>
    public int Iterations { get; init; }

    /// <summary>
    /// Gets the degree of parallelism (Argon2 <c>p</c> or scrypt <c>p</c>).
    /// </summary>
    /// <returns>The parallelism.</returns>
    public int Parallelism { get; init; }

    /// <summary>
    /// Gets the Argon2 version code.
    /// </summary>
    /// <returns>The version code. The default is 0x13.</returns>
    public int Version { get; init; } = 0x13;

    /// <summary>
    /// Gets the scrypt CPU/memory cost parameter <c>N</c>.
    /// </summary>
    /// <returns>The cost parameter <c>N</c>.</returns>
    public int CostN { get; init; }

    /// <summary>
    /// Gets the scrypt block-size parameter <c>r</c>.
    /// </summary>
    /// <returns>The block-size parameter <c>r</c>.</returns>
    public int BlockSizeR { get; init; }

    /// <summary>
    /// Gets the requested output length, in bytes.
    /// </summary>
    /// <returns>The output length.</returns>
    public required int OutputLength { get; init; }

    /// <summary>
    /// Gets the expected derived bytes, as a lowercase hex string.
    /// </summary>
    /// <returns>The expected output hex.</returns>
    public required string ExpectedHex { get; init; }
}
