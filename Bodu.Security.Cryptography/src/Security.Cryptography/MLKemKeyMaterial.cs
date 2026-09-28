// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKemKeyMaterial.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Extends <see cref="AsymmetricKeyMaterial" /> for ML-KEM with the values FIPS 203 derives from the encoded keys on
/// every operation: the matrix Â expanded from ρ, the decoded vectors t̂ and ŝ, and the hash H(ek). They are computed
/// once, when the key is set, so encapsulation and decapsulation no longer repeat them.
/// </summary>
/// <remarks>
/// <para>
/// FIPS 203 permits an implementation to keep such derived values alongside the key. Â is public and takes 1,024k²
/// bytes (9 KiB for ML-KEM-768); t̂ and ŝ take 1,024k bytes each. The instance never changes after construction, so
/// concurrent operations read it safely.
/// </para>
/// <para>
/// ŝ is secret: <see cref="Clear" /> zeroes it together with the encoded decapsulation key.
/// </para>
/// </remarks>
internal sealed class MLKemKeyMaterial
    : AsymmetricKeyMaterial
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MLKemKeyMaterial" /> class taking ownership of the supplied arrays
    /// and deriving from them the cached values not supplied.
    /// </summary>
    /// <param name="parameters">The parameter set the keys belong to.</param>
    /// <param name="encapsulationKey">The encoded encapsulation key.</param>
    /// <param name="decapsulationKey">
    /// The encoded decapsulation key, or <see langword="null" /> for a public-only instance.
    /// </param>
    /// <param name="matrix">
    /// The matrix Â already expanded by key generation, or <see langword="null" /> to expand it here.
    /// </param>
    /// <param name="publicVector">
    /// The vector t̂ key generation computed, or <see langword="null" /> to decode it here.
    /// </param>
    /// <param name="secretVector">
    /// The vector ŝ key generation computed, or <see langword="null" /> to decode it here.
    /// </param>
    private MLKemKeyMaterial(
        MLKemParameters parameters,
        byte[] encapsulationKey,
        byte[]? decapsulationKey,
        int[]? matrix,
        int[]? publicVector,
        int[]? secretVector)
        : base(encapsulationKey, decapsulationKey)
    {
        int k = parameters.K;
        int packedVectorSize = 384 * k;

        // The decapsulation key carries H(ek): key generation writes it, and import checks it against ek. Only a
        // public-only instance hashes the encapsulation key itself.
        EncapsulationKeyHash = new byte[32];
        if (decapsulationKey is not null)
            decapsulationKey.AsSpan(packedVectorSize + parameters.EncapsulationKeySize, 32).CopyTo(EncapsulationKeyHash);
        else
            KeccakSponge.Sha3_256(encapsulationKey, EncapsulationKeyHash);

        if (matrix is null)
        {
            matrix = new int[k * k * MLKemEngine.N];
            MLKemEngine.ExpandMatrix(parameters, encapsulationKey.AsSpan(packedVectorSize), matrix);
        }

        if (publicVector is null)
        {
            publicVector = new int[k * MLKemEngine.N];
            MLKemEngine.DecodeVector(parameters, encapsulationKey.AsSpan(0, packedVectorSize), publicVector);
        }

        if (decapsulationKey is not null && secretVector is null)
        {
            secretVector = new int[k * MLKemEngine.N];
            MLKemEngine.DecodeVector(parameters, decapsulationKey.AsSpan(0, packedVectorSize), secretVector);
        }

        Matrix = matrix;
        PublicVector = publicVector;
        SecretVector = decapsulationKey is null ? null : secretVector;
    }

    /// <summary>
    /// Gets the hash H(ek) of the encapsulation key.
    /// </summary>
    /// <value>The 32-byte SHA3-256 digest of <see cref="AsymmetricKeyMaterial.PublicKey" />.</value>
    internal byte[] EncapsulationKeyHash { get; }

    /// <summary>
    /// Gets the matrix Â expanded from the encapsulation key's seed ρ.
    /// </summary>
    /// <value>The k² NTT-domain polynomials, laid out as <see cref="MLKemEngine.ExpandMatrix" /> lays them out.</value>
    internal int[] Matrix { get; }

    /// <summary>
    /// Gets the vector t̂ decoded from the encapsulation key.
    /// </summary>
    /// <value>The k NTT-domain polynomials, 256 coefficients each.</value>
    internal int[] PublicVector { get; }

    /// <summary>
    /// Gets the secret vector ŝ decoded from the decapsulation key.
    /// </summary>
    /// <value>The k NTT-domain polynomials, or <see langword="null" /> for a public-only instance.</value>
    internal int[]? SecretVector { get; }

    /// <summary>
    /// Creates key material for a full key pair.
    /// </summary>
    /// <param name="parameters">The parameter set the keys belong to.</param>
    /// <param name="encapsulationKey">The encoded encapsulation key.</param>
    /// <param name="decapsulationKey">The encoded decapsulation key.</param>
    /// <param name="matrix">
    /// The matrix Â already expanded by key generation, or <see langword="null" /> to expand it.
    /// </param>
    /// <param name="publicVector">
    /// The vector t̂ key generation computed, or <see langword="null" /> to decode it.
    /// </param>
    /// <param name="secretVector">
    /// The vector ŝ key generation computed, or <see langword="null" /> to decode it.
    /// </param>
    /// <returns>The key material owning the arrays.</returns>
    internal static MLKemKeyMaterial ForKeyPair(
        MLKemParameters parameters,
        byte[] encapsulationKey,
        byte[] decapsulationKey,
        int[]? matrix = null,
        int[]? publicVector = null,
        int[]? secretVector = null) =>
        new(parameters, encapsulationKey, decapsulationKey, matrix, publicVector, secretVector);

    /// <summary>
    /// Creates public-only key material.
    /// </summary>
    /// <param name="parameters">The parameter set the key belongs to.</param>
    /// <param name="encapsulationKey">The encoded encapsulation key.</param>
    /// <returns>The key material owning the key.</returns>
    internal static MLKemKeyMaterial ForPublicKey(MLKemParameters parameters, byte[] encapsulationKey) =>
        new(parameters, encapsulationKey, null, null, null, null);

    /// <inheritdoc />
    /// <remarks>
    /// Also zeroes the cached secret vector ŝ.
    /// </remarks>
    internal override void Clear()
    {
        base.Clear();

        if (SecretVector is not null)
            CryptographyHelper.Clear(SecretVector);
    }
}
