// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricKeyMaterial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds an immutable raw-key pair or public-only key for the library's asymmetric algorithms (X25519, Ed25519, ML-KEM,
/// ML-DSA). Bundling the public key with its optional private key makes "a private key implies a public key" an
/// invariant of the type and centralizes zeroization of the secret behind a single <see cref="Clear" /> call.
/// </summary>
/// <remarks>
/// For ML-KEM the public key is the FIPS 203 encapsulation key <c>ek</c> and the private key the decapsulation key
/// <c>dk</c>; the generic member names are shared across all four algorithm families. Algorithms that cache derived
/// state alongside the raw keys (for example <see cref="Ed25519KeyMaterial" />'s decoded public point) derive from this
/// class.
/// </remarks>
internal class AsymmetricKeyMaterial
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AsymmetricKeyMaterial" /> class taking ownership of the supplied
    /// arrays.
    /// </summary>
    /// <param name="publicKey">The raw public key.</param>
    /// <param name="privateKey">The raw private key, or <see langword="null" /> for a public-only instance.</param>
    protected AsymmetricKeyMaterial(byte[] publicKey, byte[]? privateKey)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;
    }

    /// <summary>
    /// Gets the raw public key.
    /// </summary>
    /// <value>The encoded public key, always present.</value>
    internal byte[] PublicKey { get; }

    /// <summary>
    /// Gets the raw private key.
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
    /// <param name="publicKey">The raw public key.</param>
    /// <param name="privateKey">The raw private key.</param>
    /// <returns>The key material owning both arrays.</returns>
    internal static AsymmetricKeyMaterial ForKeyPair(byte[] publicKey, byte[] privateKey) =>
        new(publicKey, privateKey);

    /// <summary>
    /// Creates public-only key material.
    /// </summary>
    /// <param name="publicKey">The raw public key.</param>
    /// <returns>The key material owning the public key.</returns>
    internal static AsymmetricKeyMaterial ForPublicKey(byte[] publicKey) =>
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
