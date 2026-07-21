// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519KeyMaterial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Extends <see cref="AsymmetricKeyMaterial" /> for Ed25519 with the public key decoded to a curve point at
/// construction, so verification does not re-decode and re-evaluate the small-order status of the immutable public key
/// on every call.
/// </summary>
internal sealed class Ed25519KeyMaterial
    : AsymmetricKeyMaterial
{
    /// <summary>The public key decoded to a curve point, cached so verification does not re-decode on every call.</summary>
    private readonly Ed25519Point _publicPoint;

    /// <summary>Indicates whether <see cref="_publicPoint" /> successfully decoded from <see cref="AsymmetricKeyMaterial.PublicKey" />.</summary>
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
        : base(publicKey, privateKey)
    {
        // Decode the public key and evaluate its small-order status once here. Verification re-ran both on every call
        // (~500 field multiplications) even though the point is immutable; decoding eagerly at construction also avoids
        // a torn read of this multi-word struct under concurrent verification.
        _publicPointDecoded = Ed25519Point.TryDecode(publicKey, out _publicPoint);
        _publicPointIsSmallOrder = _publicPointDecoded && _publicPoint.IsSmallOrder();
    }

    /// <summary>
    /// Creates key material for a full key pair.
    /// </summary>
    /// <param name="publicKey">The raw public key.</param>
    /// <param name="privateKey">The raw private seed.</param>
    /// <returns>The key material owning both arrays.</returns>
    internal static new Ed25519KeyMaterial ForKeyPair(byte[] publicKey, byte[] privateKey) =>
        new(publicKey, privateKey);

    /// <summary>
    /// Creates public-only key material.
    /// </summary>
    /// <param name="publicKey">The raw public key.</param>
    /// <returns>The key material owning the public key.</returns>
    internal static new Ed25519KeyMaterial ForPublicKey(byte[] publicKey) =>
        new(publicKey, null);

    /// <summary>
    /// Gets the decoded public curve point cached at construction, together with its small-order status, avoiding a
    /// per-verification decode and small-order evaluation of the immutable public key.
    /// </summary>
    /// <param name="point">When this method returns <see langword="true" />, the decoded public point.</param>
    /// <param name="isSmallOrder">
    /// When this method returns <see langword="true" />, whether the point is small-order.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the public key decoded to a valid point; otherwise, <see langword="false" />.
    /// </returns>
    internal bool TryGetPublicPoint(out Ed25519Point point, out bool isSmallOrder)
    {
        point = _publicPoint;
        isSmallOrder = _publicPointIsSmallOrder;
        return _publicPointDecoded;
    }
}
