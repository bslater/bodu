// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLDsa.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the family base class for the ML-DSA module-lattice digital signature algorithm standardized by NIST FIPS
/// 204, exposed through the standard <see cref="AsymmetricAlgorithm" /> framework. Use the sealed
/// <see cref="MLDsa44" />, <see cref="MLDsa65" />, or <see cref="MLDsa87" /> parameter sets.
/// </summary>
/// <remarks>
/// <para>
/// ML-DSA is a digital signature scheme believed secure against quantum adversaries (its hardness rests on the
/// Module-LWE and SelfTargetMSIS problems). Signing accepts an optional <em>context</em> string of at most 255 bytes
/// that domain-separates signatures across applications; a signature created with a context verifies only with the same
/// context.
/// </para>
/// <para>
/// <strong>Hedged versus deterministic signing.</strong> By default signing is <em>hedged</em>: each signature mixes 32
/// fresh random bytes into the nonce derivation, which is the FIPS 204 default and protects against fault-injection and
/// randomness-disclosure attacks. Setting <see cref="DeterministicSigning" /> to <see langword="true" /> substitutes
/// the all-zero string, making signatures reproducible for a fixed key and message. Both variants are standard;
/// verification is unaffected.
/// </para>
/// <para>
/// <strong><see cref="AsymmetricAlgorithm.KeySize" /> semantics.</strong> The reported key size is the FIPS 204
/// parameter-set designator (44, 65, or 87) identifying the matrix dimensions and security category — it is not a key
/// length in bits.
/// </para>
/// <para>
/// <strong>Scope.</strong> This type implements pure ML-DSA (the ML-DSA.Sign / ML-DSA.Verify external functions). The
/// pre-hash variant HashML-DSA is deliberately out of scope for this version. Only the raw FIPS 204 byte encodings are
/// supported: the 32-byte seed ξ via <see cref="ImportPrivateSeed" /> and the encoded keys via
/// <see cref="ImportPrivateKey" /> (which cross-checks the embedded public-key hash) and <see cref="ImportPublicKey" />
/// . The PKCS#8 / SubjectPublicKeyInfo members inherited from <see cref="AsymmetricAlgorithm" /> are not implemented
/// and retain their base (throwing) behavior.
/// </para>
/// <para>
/// Verification returns <see langword="false" /> (never throws) for malformed, non-canonical, or wrong-length
/// signatures. Private key material is zeroed on dispose. This implementation offers best-effort side-channel
/// resistance and has not been independently audited.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var signer = MLDsa65.Create();
/// signer.GenerateKey();
/// byte[] signature = signer.SignData(message);
///
/// using var verifier = MLDsa65.Create();
/// verifier.ImportPublicKey(signer.ExportPublicKey());
/// bool valid = verifier.VerifyData(message, signature);
///]]>
/// </code>
/// </example>
public abstract class MLDsa
    : AsymmetricAlgorithm
{
    /// <summary>The maximum length, in bytes, of the signature context string.</summary>
    public const int MaxContextSizeInBytes = 255;

    /// <summary>The size, in bytes, of the private seed ξ accepted by <see cref="ImportPrivateSeed" />.</summary>
    public const int PrivateSeedSizeInBytes = 32;

    /// <summary>The FIPS 204 parameter set implemented by the derived type.</summary>
    private readonly MLDsaParameters _parameters;

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>The encoded private key, or <see langword="null" /> when no private key is set.</summary>
    private byte[]? _privateKey;

    /// <summary>The encoded public key, or <see langword="null" /> when no public key is set.</summary>
    private byte[]? _publicKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="MLDsa" /> class for a specific FIPS 204 parameter set.
    /// </summary>
    /// <param name="parameters">The parameter set implemented by the derived type.</param>
    /// <param name="parameterSetDesignator">
    /// The designator (44, 65, or 87) reported through <see cref="AsymmetricAlgorithm.KeySize" />.
    /// </param>
    private protected MLDsa(MLDsaParameters parameters, int parameterSetDesignator)
    {
        _parameters = parameters;

        LegalKeySizesValue = [new KeySizes(parameterSetDesignator, parameterSetDesignator, 0)];
        KeySizeValue = parameterSetDesignator;
    }

    /// <summary>
    /// Gets or sets a value indicating whether signing is deterministic instead of hedged.
    /// </summary>
    /// <value>
    /// <see langword="false" /> (the default) to mix 32 fresh random bytes into every signature per the FIPS 204 hedged
    /// variant; <see langword="true" /> to use the all-zero string, making signatures reproducible.
    /// </value>
    public bool DeterministicSigning { get; set; }

    /// <summary>
    /// Gets a value indicating whether the instance currently holds a private key.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when private key material is present; otherwise, <see langword="false" />.
    /// </value>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public bool HasPrivateKey
    {
        get
        {
            ThrowIfDisposed();
            return _privateKey is not null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the instance currently holds a public key.
    /// </summary>
    /// <value><see langword="true" /> when public key material is present; otherwise, <see langword="false" />.</value>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public bool HasPublicKey
    {
        get
        {
            ThrowIfDisposed();
            return _publicKey is not null;
        }
    }

    /// <inheritdoc />
    /// <value>
    /// <see langword="null" />, because ML-DSA is a signature algorithm and performs no key exchange.
    /// </value>
    public override string? KeyExchangeAlgorithm =>
        null;

    /// <summary>
    /// Gets the size, in bytes, of the encoded private key.
    /// </summary>
    /// <value>2560, 4032, or 4896 depending on the parameter set.</value>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public int PrivateKeySizeInBytes
    {
        get
        {
            ThrowIfDisposed();
            return _parameters.PrivateKeySize;
        }
    }

    /// <summary>
    /// Gets the size, in bytes, of the encoded public key.
    /// </summary>
    /// <value>1312, 1952, or 2592 depending on the parameter set.</value>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public int PublicKeySizeInBytes
    {
        get
        {
            ThrowIfDisposed();
            return _parameters.PublicKeySize;
        }
    }

    /// <inheritdoc />
    /// <value>The string <c>"ML-DSA"</c>.</value>
    public override string? SignatureAlgorithm =>
        "ML-DSA";

    /// <summary>
    /// Gets the size, in bytes, of an encoded signature.
    /// </summary>
    /// <value>2420, 3309, or 4627 depending on the parameter set.</value>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public int SignatureSizeInBytes
    {
        get
        {
            ThrowIfDisposed();
            return _parameters.SignatureSize;
        }
    }

    /// <summary>
    /// Exports the encoded FIPS 204 private key.
    /// </summary>
    /// <returns>A fresh copy of the encoded private key.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public byte[] ExportPrivateKey()
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPrivateKey(_privateKey is not null);

        return (byte[])_privateKey!.Clone();
    }

    /// <summary>
    /// Exports the encoded FIPS 204 public key.
    /// </summary>
    /// <returns>A fresh copy of the encoded public key.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a public key.</exception>
    public byte[] ExportPublicKey()
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPublicKey(_publicKey is not null);

        return (byte[])_publicKey!.Clone();
    }

    /// <summary>
    /// Generates a fresh random key pair, replacing any existing key material on the instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <remarks>
    /// The 32-byte seed ξ is drawn from a cryptographically secure random source. Any previously held private key is
    /// zeroed before being replaced.
    /// </remarks>
    public void GenerateKey()
    {
        ThrowIfDisposed();

        Span<byte> seed = stackalloc byte[PrivateSeedSizeInBytes];
        CryptographyHelper.FillWithRandomBytes(seed);

        SetKeysFromSeed(seed);
        CryptographyHelper.Clear(seed);
    }

    /// <summary>
    /// Imports an encoded FIPS 204 private key, recomputing and caching the matching public key, and replacing any
    /// existing key material on the instance.
    /// </summary>
    /// <param name="privateKey">The encoded private key ρ ‖ K ‖ tr ‖ s₁ ‖ s₂ ‖ t₀.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="privateKey" /> does not have the exact parameter-set length, or its embedded tr hash does not
    /// match the public key recomputed from the secret vectors.
    /// </exception>
    public void ImportPrivateKey(ReadOnlySpan<byte> privateKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(
            privateKey, _parameters.PrivateKeySize, $"{_parameters.Name} private");

        byte[] publicKey = new byte[_parameters.PublicKeySize];
        if (!MLDsaEngine.TryDerivePublicKey(_parameters, privateKey, publicKey))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_EncodedKey,
                    $"{_parameters.Name} private",
                    "FIPS 204"),
                nameof(privateKey));
        }

        CryptographyHelper.ClearAndNullify(ref _privateKey);
        _privateKey = privateKey.ToArray();
        _publicKey = publicKey;
    }

    /// <summary>
    /// Imports the FIPS 204 32-byte private seed ξ and regenerates the full key pair from it, replacing any existing
    /// key material on the instance.
    /// </summary>
    /// <param name="seed">The 32-byte key-generation seed ξ.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="seed" /> is not exactly 32 bytes long.</exception>
    public void ImportPrivateSeed(ReadOnlySpan<byte> seed)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(seed, PrivateSeedSizeInBytes, $"{_parameters.Name} private seed");

        SetKeysFromSeed(seed);
    }

    /// <summary>
    /// Imports an encoded FIPS 204 public key, replacing any existing key material on the instance.
    /// </summary>
    /// <param name="publicKey">The encoded public key ρ ‖ t₁.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="publicKey" /> does not have the exact parameter-set length.
    /// </exception>
    /// <remarks>
    /// Any private key previously held by the instance is zeroed and discarded, leaving a verify-only instance.
    /// </remarks>
    public void ImportPublicKey(ReadOnlySpan<byte> publicKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(
            publicKey, _parameters.PublicKeySize, $"{_parameters.Name} public");

        CryptographyHelper.ClearAndNullify(ref _privateKey);
        _publicKey = publicKey.ToArray();
    }

    /// <summary>
    /// Signs the supplied data with an empty context string.
    /// </summary>
    /// <param name="data">The message bytes to sign. May be empty.</param>
    /// <returns>The encoded signature.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public byte[] SignData(ReadOnlySpan<byte> data) =>
        SignData(data, []);

    /// <summary>
    /// Signs the supplied data under a context string.
    /// </summary>
    /// <param name="data">The message bytes to sign. May be empty.</param>
    /// <param name="context">The domain-separation context string of at most 255 bytes. May be empty.</param>
    /// <returns>The encoded signature.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="context" /> is longer than 255 bytes.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public byte[] SignData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context)
    {
        ThrowIfDisposed();

        byte[] signature = new byte[_parameters.SignatureSize];
        SignData(data, context, signature);

        return signature;
    }

    /// <summary>
    /// Signs the supplied data under a context string, writing the encoded signature into
    /// <paramref name="destination" />.
    /// </summary>
    /// <param name="data">The message bytes to sign. May be empty.</param>
    /// <param name="context">The domain-separation context string of at most 255 bytes. May be empty.</param>
    /// <param name="destination">
    /// The span receiving the signature. Must be exactly <see cref="SignatureSizeInBytes" /> bytes.
    /// </param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="context" /> is longer than 255 bytes, or <paramref name="destination" /> does not have the exact
    /// signature length.
    /// </exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public void SignData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination)
    {
        ThrowIfDisposed();
        ThrowIfContextTooLong(context);
        CryptographyThrowHelper.ThrowIfInvalidDestinationLength(destination, _parameters.SignatureSize);
        CryptographyThrowHelper.ThrowIfNoPrivateKey(_privateKey is not null);

        Span<byte> rnd = stackalloc byte[32];
        if (!DeterministicSigning)
            CryptographyHelper.FillWithRandomBytes(rnd);

        MLDsaEngine.Sign(_parameters, _privateKey, context, data, rnd, destination);
        CryptographyHelper.Clear(rnd);
    }

    /// <summary>
    /// Verifies a signature over the supplied data with an empty context string.
    /// </summary>
    /// <param name="data">The message bytes that were signed. May be empty.</param>
    /// <param name="signature">The candidate encoded signature.</param>
    /// <returns>
    /// <see langword="true" /> when the signature is valid; <see langword="false" /> for any invalid, malformed, or
    /// wrong-length signature.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a public key.</exception>
    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature) =>
        VerifyData(data, signature, []);

    /// <summary>
    /// Verifies a signature over the supplied data under a context string.
    /// </summary>
    /// <param name="data">The message bytes that were signed. May be empty.</param>
    /// <param name="signature">The candidate encoded signature.</param>
    /// <param name="context">The domain-separation context string used at signing time. May be empty.</param>
    /// <returns>
    /// <see langword="true" /> when the signature is valid; <see langword="false" /> for any invalid, malformed, or
    /// wrong-length signature.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="context" /> is longer than 255 bytes.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a public key.</exception>
    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> context)
    {
        ThrowIfDisposed();
        ThrowIfContextTooLong(context);
        CryptographyThrowHelper.ThrowIfNoPublicKey(_publicKey is not null);

        return MLDsaEngine.Verify(_parameters, _publicKey, context, data, signature);
    }

    /// <summary>
    /// Signs with explicit signer randomness, exposing the FIPS 204 Sign_internal interface for known-answer tests that
    /// pin hedged signatures.
    /// </summary>
    /// <param name="data">The message bytes to sign.</param>
    /// <param name="context">The domain-separation context string.</param>
    /// <param name="rnd">The 32-byte signer randomness.</param>
    /// <returns>The encoded signature.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="context" /> is longer than 255 bytes.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    internal byte[] SignDataInternal(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> rnd)
    {
        ThrowIfDisposed();
        ThrowIfContextTooLong(context);
        CryptographyThrowHelper.ThrowIfNoPrivateKey(_privateKey is not null);

        byte[] signature = new byte[_parameters.SignatureSize];
        MLDsaEngine.Sign(_parameters, _privateKey, context, data, rnd, signature);

        return signature;
    }

    /// <summary>
    /// Releases the resources used by this instance, zeroing all private key material.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            base.Dispose(disposing);
            return;
        }

        if (disposing)
        {
            CryptographyHelper.ClearAndNullify(ref _privateKey);
            _publicKey = null;
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Throws <see cref="ArgumentException" /> when the context string exceeds the FIPS 204 single-byte length limit.
    /// </summary>
    /// <param name="context">The context string to validate.</param>
    /// <exception cref="ArgumentException"><paramref name="context" /> is longer than 255 bytes.</exception>
    private static void ThrowIfContextTooLong(ReadOnlySpan<byte> context)
    {
        if (context.Length > MaxContextSizeInBytes)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_ContextTooLong,
                    MaxContextSizeInBytes),
                nameof(context));
        }
    }

    /// <summary>
    /// Derives and stores the key pair from the 32-byte seed ξ, zeroing any previously held private key.
    /// </summary>
    /// <param name="seed">The 32-byte seed.</param>
    private void SetKeysFromSeed(ReadOnlySpan<byte> seed)
    {
        byte[] publicKey = new byte[_parameters.PublicKeySize];
        byte[] privateKey = new byte[_parameters.PrivateKeySize];
        MLDsaEngine.KeyGen(_parameters, seed, publicKey, privateKey);

        CryptographyHelper.ClearAndNullify(ref _privateKey);
        _privateKey = privateKey;
        _publicKey = publicKey;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> when the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
