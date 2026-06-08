// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2id.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes the Argon2id password-hashing and key-derivation function (RFC 9106) — the hybrid variant that uses
/// data-independent addressing for the first half of the first pass and data-dependent addressing thereafter. This is
/// the RECOMMENDED default for password hashing. This class cannot be inherited.
/// </summary>
/// <remarks>
/// Argon2id combines the side-channel resistance of Argon2i with the time-memory trade-off resistance of Argon2d, and
/// is the variant any RFC 9106 implementation is required to support.
/// </remarks>
public sealed class Argon2id : Argon2
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Argon2id" /> class with the specified cost parameters.
    /// </summary>
    /// <param name="parameters">The cost and auxiliary parameters governing the derivation.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="parameters" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A cost parameter in <paramref name="parameters" /> falls outside the range permitted by RFC 9106.
    /// </exception>
    public Argon2id(Argon2Parameters parameters)
        : base(parameters)
    { }

    /// <inheritdoc />
    internal override Argon2Type Type => Argon2Type.Argon2id;

    /// <summary>
    /// Derives a tag from a password and salt using the supplied parameters in a single call.
    /// </summary>
    /// <param name="password">The password / message to derive from.</param>
    /// <param name="salt">The salt; must be at least 8 bytes.</param>
    /// <param name="parameters">The cost and auxiliary parameters.</param>
    /// <returns>The derived tag.</returns>
    public static new byte[] DeriveKey(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Argon2Parameters parameters) =>
        new Argon2id(parameters).GetBytes(password, salt);

    /// <summary>
    /// Derives a password hash and returns it as a PHC encoded-hash string in a single call.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The salt to embed; must be at least 8 bytes.</param>
    /// <param name="parameters">The cost and auxiliary parameters.</param>
    /// <returns>The PHC encoded-hash string.</returns>
    public static new string Hash(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, Argon2Parameters parameters) =>
        new Argon2id(parameters).Hash(password, salt);

    /// <summary>
    /// Verifies a password against an Argon2id PHC encoded-hash string.
    /// </summary>
    /// <param name="encoded">The PHC encoded-hash string; must name the <c>argon2id</c> variant.</param>
    /// <param name="password">The password to verify.</param>
    /// <returns>
    /// <see langword="true" /> if the string names Argon2id and the password matches; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static new bool Verify(string encoded, ReadOnlySpan<byte> password) =>
        VerifyAs(encoded, password, ReadOnlySpan<byte>.Empty, Argon2Type.Argon2id);

    /// <summary>
    /// Verifies a password against an Argon2id PHC encoded-hash string using the supplied secret key.
    /// </summary>
    /// <param name="encoded">The PHC encoded-hash string; must name the <c>argon2id</c> variant.</param>
    /// <param name="password">The password to verify.</param>
    /// <param name="secret">The secret key used when the hash was produced.</param>
    /// <returns>
    /// <see langword="true" /> if the string names Argon2id and the password matches; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static new bool Verify(string encoded, ReadOnlySpan<byte> password, ReadOnlySpan<byte> secret) =>
        VerifyAs(encoded, password, secret, Argon2Type.Argon2id);
}
