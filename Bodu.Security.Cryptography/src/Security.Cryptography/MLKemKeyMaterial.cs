// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemKeyMaterial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds an immutable ML-KEM key pair or encapsulation-only key. Bundling the encapsulation (public) key with its
/// optional decapsulation (private) key makes "a decapsulation key implies an encapsulation key" an invariant of the
/// type and centralizes zeroization of the secret behind a single <see cref="Clear" /> call.
/// </summary>
internal sealed class MLKemKeyMaterial
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLKemKeyMaterial" /> class taking ownership of the supplied arrays.
    /// </summary>
    /// <param name="encapsulationKey">The encoded encapsulation key.</param>
    /// <param name="decapsulationKey">
    /// The encoded decapsulation key, or <see langword="null" /> for an encapsulation-only instance.
    /// </param>
    private MLKemKeyMaterial(byte[] encapsulationKey, byte[]? decapsulationKey)
    {
        EncapsulationKey = encapsulationKey;
        DecapsulationKey = decapsulationKey;
    }

    /// <summary>
    /// Gets the encoded encapsulation (public) key.
    /// </summary>
    /// <value>The encoded encapsulation key, always present.</value>
    internal byte[] EncapsulationKey { get; }

    /// <summary>
    /// Gets the encoded decapsulation (private) key.
    /// </summary>
    /// <value>The encoded decapsulation key, or <see langword="null" /> for an encapsulation-only instance.</value>
    internal byte[]? DecapsulationKey { get; }

    /// <summary>
    /// Gets a value indicating whether decapsulation key material is present.
    /// </summary>
    /// <value><see langword="true" /> when a decapsulation key is held; otherwise, <see langword="false" />.</value>
    internal bool HasDecapsulationKey =>
        DecapsulationKey is not null;

    /// <summary>
    /// Creates key material for a full key pair.
    /// </summary>
    /// <param name="encapsulationKey">The encoded encapsulation key.</param>
    /// <param name="decapsulationKey">The encoded decapsulation key.</param>
    /// <returns>The key material owning both arrays.</returns>
    internal static MLKemKeyMaterial ForKeyPair(byte[] encapsulationKey, byte[] decapsulationKey) =>
        new(encapsulationKey, decapsulationKey);

    /// <summary>
    /// Creates encapsulation-only key material.
    /// </summary>
    /// <param name="encapsulationKey">The encoded encapsulation key.</param>
    /// <returns>The key material owning the encapsulation key.</returns>
    internal static MLKemKeyMaterial ForEncapsulationKey(byte[] encapsulationKey) =>
        new(encapsulationKey, null);

    /// <summary>
    /// Zeroizes the held decapsulation key material.
    /// </summary>
    internal void Clear()
    {
        if (DecapsulationKey is not null)
            CryptographyHelper.Clear(DecapsulationKey);
    }
}
