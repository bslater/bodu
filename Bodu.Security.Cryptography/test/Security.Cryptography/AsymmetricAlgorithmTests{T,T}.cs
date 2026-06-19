// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsymmetricAlgorithmTests{T,T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides unit tests for asymmetric algorithms derived from <see cref="AsymmetricAlgorithm" />, verifying
/// construction defaults, key-size reporting, raw key import/export behaviour, defensive copying, and disposal
/// semantics. Operation-specific contracts live in the derived <see cref="KeyAgreementAlgorithmTests{TTest, TAlgorithm}" />,
/// <see cref="SignatureAlgorithmTests{TTest, TAlgorithm}" />, and <see cref="KemAlgorithmTests{TTest, TAlgorithm}" />
/// bases.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for <see cref="DynamicDataAttribute" /> sources.</typeparam>
/// <typeparam name="TAlgorithm">The type of asymmetric algorithm under test.</typeparam>
/// <remarks>
/// The four asymmetric families expose differently named key members (private/public keys for the Curve25519
/// family, decapsulation/encapsulation keys for ML-KEM), so the shared contract is expressed through abstract
/// adapter members that each family base or concrete test class maps onto the algorithm's surface.
/// </remarks>
[TestClass]
public abstract partial class AsymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : AsymmetricAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : AsymmetricAlgorithm, new()
{
    /// <summary>
    /// Creates an instance of the asymmetric algorithm under test.
    /// </summary>
    /// <returns>A new <typeparamref name="TAlgorithm" /> instance with no key material.</returns>
    protected virtual TAlgorithm CreateAlgorithm() => new();

    /// <summary>
    /// Returns the <see cref="AsymmetricAlgorithmSpecification" /> describing the expected observable properties of
    /// <typeparamref name="TAlgorithm" />.
    /// </summary>
    /// <returns>A populated <see cref="AsymmetricAlgorithmSpecification" />.</returns>
    protected abstract AsymmetricAlgorithmSpecification GetSpecification();

    /// <summary>
    /// Generates a fresh random key pair on the algorithm instance.
    /// </summary>
    /// <param name="algorithm">The instance to populate.</param>
    protected abstract void GenerateKey(TAlgorithm algorithm);

    /// <summary>
    /// Reports whether the instance currently holds private key material.
    /// </summary>
    /// <param name="algorithm">The instance to query.</param>
    /// <returns><see langword="true" /> when private key material is present.</returns>
    protected abstract bool HasPrivateKey(TAlgorithm algorithm);

    /// <summary>
    /// Reports whether the instance currently holds public key material.
    /// </summary>
    /// <param name="algorithm">The instance to query.</param>
    /// <returns><see langword="true" /> when public key material is present.</returns>
    protected abstract bool HasPublicKey(TAlgorithm algorithm);

    /// <summary>
    /// Imports raw private key material into the instance (the decapsulation key for KEMs).
    /// </summary>
    /// <param name="algorithm">The instance to populate.</param>
    /// <param name="privateKey">The raw private key bytes.</param>
    protected abstract void ImportPrivateKey(TAlgorithm algorithm, byte[] privateKey);

    /// <summary>
    /// Imports raw public key material into the instance (the encapsulation key for KEMs).
    /// </summary>
    /// <param name="algorithm">The instance to populate.</param>
    /// <param name="publicKey">The raw public key bytes.</param>
    protected abstract void ImportPublicKey(TAlgorithm algorithm, byte[] publicKey);

    /// <summary>
    /// Exports the raw private key material from the instance.
    /// </summary>
    /// <param name="algorithm">The instance to export from.</param>
    /// <returns>The raw private key bytes.</returns>
    protected abstract byte[] ExportPrivateKey(TAlgorithm algorithm);

    /// <summary>
    /// Exports the raw public key material from the instance.
    /// </summary>
    /// <param name="algorithm">The instance to export from.</param>
    /// <returns>The raw public key bytes.</returns>
    protected abstract byte[] ExportPublicKey(TAlgorithm algorithm);

    /// <summary>
    /// Creates an algorithm instance populated with a freshly generated key pair.
    /// </summary>
    /// <returns>A new instance holding both private and public key material.</returns>
    protected TAlgorithm CreateAlgorithmWithGeneratedKey()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        GenerateKey(algorithm);

        return algorithm;
    }

    /// <summary>
    /// Creates a verify-only / encapsulate-only instance holding the public key of <paramref name="donor" />.
    /// </summary>
    /// <param name="donor">The instance whose public key is copied.</param>
    /// <returns>A new instance holding only public key material.</returns>
    protected TAlgorithm CreatePublicOnlyAlgorithm(TAlgorithm donor)
    {
        TAlgorithm algorithm = CreateAlgorithm();
        ImportPublicKey(algorithm, ExportPublicKey(donor));

        return algorithm;
    }
}
