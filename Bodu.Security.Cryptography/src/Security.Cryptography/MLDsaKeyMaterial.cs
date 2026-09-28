// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsaKeyMaterial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Extends <see cref="AsymmetricKeyMaterial" /> for ML-DSA with the values FIPS 204 derives from the encoded keys on
/// every operation: the matrix Â expanded from ρ, the hash tr, the private key's vectors ŝ₁, ŝ₂ and t̂₀, and the public
/// key's vector NTT(t₁·2ᵈ). They are computed once, when the key is set, so signing and verification no longer repeat
/// them.
/// </summary>
/// <remarks>
/// <para>
/// FIPS 204 permits an implementation to keep such derived values alongside the key. Each polynomial takes 1 KiB: Â
/// holds k·ℓ of them (30 KiB for ML-DSA-65) and is public, as is NTT(t₁·2ᵈ) with k; ŝ₁ holds ℓ and ŝ₂ and t̂₀ k each.
/// The instance never changes after construction, so concurrent operations read it safely.
/// </para>
/// <para>
/// ŝ₁, ŝ₂ and t̂₀ are secret: <see cref="Clear" /> zeroes them together with the encoded private key.
/// </para>
/// </remarks>
internal sealed class MLDsaKeyMaterial
    : AsymmetricKeyMaterial
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLDsaKeyMaterial" /> class taking ownership of the supplied arrays
    /// and deriving from the encoded keys the cached values not supplied.
    /// </summary>
    /// <param name="parameters">The parameter set the keys belong to.</param>
    /// <param name="publicKey">The encoded public key.</param>
    /// <param name="privateKey">The encoded private key, or <see langword="null" /> for a public-only instance.</param>
    /// <param name="matrix">The matrix Â already computed, or <see langword="null" /> to expand it here.</param>
    /// <param name="highOrderVector">
    /// The vector NTT(t₁·2ᵈ) already computed, or <see langword="null" /> to form it here.
    /// </param>
    /// <param name="secretVectors">
    /// The vectors ŝ₁, ŝ₂ and t̂₀ already computed, or <see langword="null" /> to decode them here from
    /// <paramref name="privateKey" />.
    /// </param>
    private MLDsaKeyMaterial(
        MLDsaParameters parameters,
        byte[] publicKey,
        byte[]? privateKey,
        int[]? matrix,
        int[]? highOrderVector,
        (int[] SecretVector1, int[] SecretVector2, int[] LowOrderVector)? secretVectors)
        : base(publicKey, privateKey)
    {
        int k = parameters.K;
        int l = parameters.L;

        // The private key carries tr = H(pk, 64): key generation writes it, and import checks it against the public
        // key. Only a public-only instance hashes the public key itself.
        PublicKeyHash = new byte[64];
        if (privateKey is not null)
            privateKey.AsSpan(64, 64).CopyTo(PublicKeyHash);
        else
            KeccakSponge.Shake256(publicKey, PublicKeyHash);

        if (matrix is null)
        {
            matrix = new int[k * l * MLDsaEngine.N];
            MLDsaEngine.ExpandMatrix(parameters, publicKey.AsSpan(0, 32), matrix);
        }

        if (highOrderVector is null)
        {
            highOrderVector = new int[k * MLDsaEngine.N];
            MLDsaEngine.ExpandPublicKey(parameters, publicKey, highOrderVector);
        }

        Matrix = matrix;
        HighOrderVector = highOrderVector;

        if (privateKey is null)
            return;

        if (secretVectors is null)
        {
            secretVectors = (new int[l * MLDsaEngine.N], new int[k * MLDsaEngine.N], new int[k * MLDsaEngine.N]);
            MLDsaEngine.ExpandPrivateKey(
                parameters, privateKey, secretVectors.Value.SecretVector1, secretVectors.Value.SecretVector2, secretVectors.Value.LowOrderVector);
        }

        (SecretVector1, SecretVector2, LowOrderVector) = secretVectors.Value;
    }

    /// <summary>
    /// Gets the hash tr of the public key.
    /// </summary>
    /// <value>The 64-byte digest H(pk, 64) of <see cref="AsymmetricKeyMaterial.PublicKey" />.</value>
    internal byte[] PublicKeyHash { get; }

    /// <summary>
    /// Gets the matrix Â expanded from the public key's seed ρ.
    /// </summary>
    /// <value>The k·ℓ NTT-domain polynomials, as <see cref="MLDsaEngine.ExpandMatrix" /> produces them.</value>
    internal int[] Matrix { get; }

    /// <summary>
    /// Gets the vector NTT(t₁·2ᵈ) formed from the public key.
    /// </summary>
    /// <value>The k polynomials, as <see cref="MLDsaEngine.ExpandPublicKey" /> produces them.</value>
    internal int[] HighOrderVector { get; }

    /// <summary>
    /// Gets the secret vector ŝ₁ decoded from the private key.
    /// </summary>
    /// <value>
    /// The ℓ polynomials, as <see cref="MLDsaEngine.ExpandPrivateKey" /> produces them, or <see langword="null" /> for
    /// a public-only instance.
    /// </value>
    internal int[]? SecretVector1 { get; }

    /// <summary>
    /// Gets the secret vector ŝ₂ decoded from the private key.
    /// </summary>
    /// <value>
    /// The k polynomials, as <see cref="MLDsaEngine.ExpandPrivateKey" /> produces them, or <see langword="null" /> for
    /// a public-only instance.
    /// </value>
    internal int[]? SecretVector2 { get; }

    /// <summary>
    /// Gets the vector t̂₀ decoded from the private key.
    /// </summary>
    /// <value>
    /// The k polynomials, as <see cref="MLDsaEngine.ExpandPrivateKey" /> produces them, or <see langword="null" /> for
    /// a public-only instance.
    /// </value>
    internal int[]? LowOrderVector { get; }

    /// <summary>
    /// Generates a key pair from the seed ξ, keeping the values key generation computes along the way.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="seed">The 32-byte key-generation seed ξ.</param>
    /// <returns>The key material for the generated key pair.</returns>
    internal static MLDsaKeyMaterial Generate(MLDsaParameters parameters, ReadOnlySpan<byte> seed)
    {
        byte[] publicKey = new byte[parameters.PublicKeySize];
        byte[] privateKey = new byte[parameters.PrivateKeySize];
        (int[] matrix, int[] highOrderVector, int[] secretVector1, int[] secretVector2, int[] lowOrderVector) =
            AllocateValues(parameters);

        MLDsaEngine.KeyGen(
            parameters, seed, publicKey, privateKey, matrix, highOrderVector, secretVector1, secretVector2, lowOrderVector);

        return new(parameters, publicKey, privateKey, matrix, highOrderVector, (secretVector1, secretVector2, lowOrderVector));
    }

    /// <summary>
    /// Attempts to import an encoded private key, recomputing its public key and keeping the values the recomputation
    /// produces along the way.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <returns>
    /// The key material, or <see langword="null" /> when the private key is not consistent: an s₁ or s₂ coefficient
    /// outside [−η, η], a t₀ that does not match the one recomputed, or a tr that does not match the public key.
    /// </returns>
    internal static MLDsaKeyMaterial? TryImportPrivateKey(MLDsaParameters parameters, ReadOnlySpan<byte> privateKey)
    {
        byte[] publicKey = new byte[parameters.PublicKeySize];
        (int[] matrix, int[] highOrderVector, int[] secretVector1, int[] secretVector2, int[] lowOrderVector) =
            AllocateValues(parameters);

        if (!MLDsaEngine.TryDerivePublicKey(
            parameters, privateKey, publicKey, matrix, highOrderVector, secretVector1, secretVector2, lowOrderVector))
        {
            CryptographyHelper.Clear(secretVector1);
            CryptographyHelper.Clear(secretVector2);
            CryptographyHelper.Clear(lowOrderVector);
            return null;
        }

        return new(
            parameters, publicKey, privateKey.ToArray(), matrix, highOrderVector, (secretVector1, secretVector2, lowOrderVector));
    }

    /// <summary>
    /// Creates key material for a full key pair, deriving every cached value from the encoded keys.
    /// </summary>
    /// <param name="parameters">The parameter set the keys belong to.</param>
    /// <param name="publicKey">The encoded public key.</param>
    /// <param name="privateKey">The encoded private key.</param>
    /// <returns>The key material owning the keys.</returns>
    internal static MLDsaKeyMaterial ForKeyPair(MLDsaParameters parameters, byte[] publicKey, byte[] privateKey) =>
        new(parameters, publicKey, privateKey, null, null, null);

    /// <summary>
    /// Creates public-only key material.
    /// </summary>
    /// <param name="parameters">The parameter set the key belongs to.</param>
    /// <param name="publicKey">The encoded public key.</param>
    /// <returns>The key material owning the key.</returns>
    internal static MLDsaKeyMaterial ForPublicKey(MLDsaParameters parameters, byte[] publicKey) =>
        new(parameters, publicKey, null, null, null, null);

    /// <inheritdoc />
    /// <remarks>
    /// Also zeroes the cached secret vectors ŝ₁, ŝ₂ and t̂₀.
    /// </remarks>
    internal override void Clear()
    {
        base.Clear();

        if (SecretVector1 is not null)
            CryptographyHelper.Clear(SecretVector1);

        if (SecretVector2 is not null)
            CryptographyHelper.Clear(SecretVector2);

        if (LowOrderVector is not null)
            CryptographyHelper.Clear(LowOrderVector);
    }

    /// <summary>
    /// Allocates the arrays that hold the values a key pair keeps.
    /// </summary>
    /// <param name="parameters">The parameter set.</param>
    /// <returns>The arrays for Â, NTT(t₁·2ᵈ), ŝ₁, ŝ₂ and t̂₀.</returns>
    private static (int[] Matrix, int[] HighOrderVector, int[] SecretVector1, int[] SecretVector2, int[] LowOrderVector) AllocateValues(
        MLDsaParameters parameters)
    {
        int k = parameters.K;
        int l = parameters.L;

        return (
            new int[k * l * MLDsaEngine.N],
            new int[k * MLDsaEngine.N],
            new int[l * MLDsaEngine.N],
            new int[k * MLDsaEngine.N],
            new int[k * MLDsaEngine.N]);
    }
}
