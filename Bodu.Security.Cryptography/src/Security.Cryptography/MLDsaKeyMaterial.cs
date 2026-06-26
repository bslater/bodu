// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds an immutable ML-DSA key pair or public-only key. Bundling the encoded public key with its optional encoded
/// private key makes "a private key implies a public key" an invariant of the type and centralizes zeroization of the
/// secret behind a single <see cref="Clear" /> call.
/// </summary>
internal sealed class MLDsaKeyMaterial
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLDsaKeyMaterial" /> class taking ownership of the supplied arrays.
    /// </summary>
    /// <param name="publicKey">The encoded public key.</param>
    /// <param name="privateKey">The encoded private key, or <see langword="null" /> for a public-only instance.</param>
    private MLDsaKeyMaterial(byte[] publicKey, byte[]? privateKey)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;
    }

    /// <summary>
    /// Gets the encoded public key.
    /// </summary>
    /// <value>The encoded public key, always present.</value>
    internal byte[] PublicKey { get; }

    /// <summary>
    /// Gets the encoded private key.
    /// </summary>
    /// <value>The encoded private key, or <see langword="null" /> for a public-only instance.</value>
    internal byte[]? PrivateKey { get; }

    /// <summary>
    /// Gets a value indicating whether private key material is present.
    /// </summary>
    /// <value><see langword="true" /> when a private key is held; otherwise, <see langword="false" />.</value>
    internal bool HasPrivateKey =>
        PrivateKey is not null;

    /// <summary>
    /// Creates key material for a full key pair.
    /// </summary>
    /// <param name="publicKey">The encoded public key.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <returns>The key material owning both arrays.</returns>
    internal static MLDsaKeyMaterial ForKeyPair(byte[] publicKey, byte[] privateKey) =>
        new(publicKey, privateKey);

    /// <summary>
    /// Creates public-only key material.
    /// </summary>
    /// <param name="publicKey">The encoded public key.</param>
    /// <returns>The key material owning the public key.</returns>
    internal static MLDsaKeyMaterial ForPublicKey(byte[] publicKey) =>
        new(publicKey, null);

    /// <summary>
    /// Zeroizes the held private key material.
    /// </summary>
    internal void Clear()
    {
        if (PrivateKey is not null)
            CryptographyHelper.Clear(PrivateKey);
    }
}
