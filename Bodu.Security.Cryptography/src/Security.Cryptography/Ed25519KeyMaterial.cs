// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519KeyMaterial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds an immutable Ed25519 key pair or public-only key. Bundling the public key with its optional private seed makes
/// "a private key implies a public key" an invariant of the type and centralizes zeroization of the secret behind a
/// single <see cref="Clear" /> call.
/// </summary>
internal sealed class Ed25519KeyMaterial
{
    /// <summary>The public key decoded to a curve point, cached so verification does not re-decode on every call.</summary>
    private readonly Ed25519Point _publicPoint;

    /// <summary>Indicates whether <see cref="_publicPoint" /> successfully decoded from <see cref="PublicKey" />.</summary>
    private readonly bool _publicPointDecoded;

    /// <summary>Indicates whether the decoded public point is a small-order point, computed once at construction.</summary>
    private readonly bool _publicPointIsSmallOrder;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ed25519KeyMaterial" /> class taking ownership of the supplied
    /// arrays.
    /// </summary>
    /// <param name="publicKey">The raw public key.</param>
    /// <param name="privateKey">The raw private seed, or <see langword="null" /> for a public-only instance.</param>
    private Ed25519KeyMaterial(byte[] publicKey, byte[]? privateKey)
    {
        PublicKey = publicKey;
        PrivateKey = privateKey;

        // Decode the public key and evaluate its small-order status once here. Verification re-ran both on every call
        // (~500 field multiplications) even though the point is immutable; decoding eagerly at construction also avoids
        // a torn read of this multi-word struct under concurrent verification.
        _publicPointDecoded = Ed25519Point.TryDecode(publicKey, out _publicPoint);
        _publicPointIsSmallOrder = _publicPointDecoded && _publicPoint.IsSmallOrder();
    }

    /// <summary>
    /// Gets the raw public key.
    /// </summary>
    /// <value>The 32-byte public key, always present.</value>
    internal byte[] PublicKey { get; }

    /// <summary>
    /// Gets the raw private seed.
    /// </summary>
    /// <value>The 32-byte private seed, or <see langword="null" /> for a public-only instance.</value>
    internal byte[]? PrivateKey { get; }

    /// <summary>
    /// Gets a value indicating whether private key material is present.
    /// </summary>
    /// <value><see langword="true" /> when a private seed is held; otherwise, <see langword="false" />.</value>
    internal bool HasPrivateKey =>
        PrivateKey is not null;

    /// <summary>
    /// Creates key material for a full key pair.
    /// </summary>
    /// <param name="publicKey">The raw public key.</param>
    /// <param name="privateKey">The raw private seed.</param>
    /// <returns>The key material owning both arrays.</returns>
    internal static Ed25519KeyMaterial ForKeyPair(byte[] publicKey, byte[] privateKey) =>
        new(publicKey, privateKey);

    /// <summary>
    /// Creates public-only key material.
    /// </summary>
    /// <param name="publicKey">The raw public key.</param>
    /// <returns>The key material owning the public key.</returns>
    internal static Ed25519KeyMaterial ForPublicKey(byte[] publicKey) =>
        new(publicKey, null);

    /// <summary>
    /// Gets the decoded public curve point cached at construction, together with its small-order status, avoiding a
    /// per-verification decode and small-order evaluation of the immutable public key.
    /// </summary>
    /// <param name="point">When this method returns <see langword="true" />, the decoded public point.</param>
    /// <param name="isSmallOrder">When this method returns <see langword="true" />, whether the point is small-order.</param>
    /// <returns><see langword="true" /> if the public key decoded to a valid point; otherwise, <see langword="false" />.</returns>
    internal bool TryGetPublicPoint(out Ed25519Point point, out bool isSmallOrder)
    {
        point = _publicPoint;
        isSmallOrder = _publicPointIsSmallOrder;
        return _publicPointDecoded;
    }

    /// <summary>
    /// Zeroizes the held private seed material.
    /// </summary>
    internal void Clear()
    {
        if (PrivateKey is not null)
            CryptographyHelper.Clear(PrivateKey);
    }
}
