// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MLKem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the family base class for the ML-KEM module-lattice key-encapsulation mechanism standardized by NIST FIPS
/// 203, exposed through the standard <see cref="AsymmetricAlgorithm" /> framework. Use the sealed
/// <see cref="MLKem512" />, <see cref="MLKem768" />, or <see cref="MLKem1024" /> parameter sets.
/// </summary>
/// <remarks>
/// <para>
/// ML-KEM is a key-encapsulation mechanism believed secure against quantum adversaries (its hardness rests on the
/// Module-LWE problem). One party holds the decapsulation key; anyone with the encapsulation key can call
/// <see cref="Encapsulate()" /> to produce a ciphertext together with a fresh 32-byte shared secret, which the key
/// holder recovers via <see cref="Decapsulate(ReadOnlySpan{byte})" />.
/// </para>
/// <para>
/// <strong><see cref="AsymmetricAlgorithm.KeySize" /> semantics.</strong> The reported key size is the FIPS 203
/// parameter-set designator (512, 768, or 1024) identifying the module rank and security category — it is not a key
/// length in bits, because module-lattice keys have no meaningful single bit-length.
/// </para>
/// <para>
/// <strong>Implicit rejection.</strong> <see cref="Decapsulate(ReadOnlySpan{byte})" /> never throws for a tampered
/// ciphertext of the correct length: per FIPS 203 it silently returns the unrelated key J(z ‖ c), so an attacker cannot
/// use decapsulation failures as an oracle. Only a wrong-length ciphertext throws <see cref="ArgumentException" />.
/// </para>
/// <para>
/// <strong>Key formats.</strong> Only the FIPS 203 byte encodings are supported: the 64-byte private seed (d ‖ z) via
/// <see cref="ImportPrivateSeed" />, and the encoded decapsulation / encapsulation keys via
/// <see cref="ImportDecapsulationKey" /> and <see cref="ImportEncapsulationKey" />, which apply the §7.3 hash and §7.2
/// modulus checks respectively. The PKCS#8 / SubjectPublicKeyInfo members inherited from
/// <see cref="AsymmetricAlgorithm" /> are not implemented and retain their base (throwing) behavior.
/// </para>
/// <para>
/// The decapsulation re-encryption comparison and key selection are constant-time, and private key material is zeroed
/// on dispose. This implementation offers best-effort side-channel resistance and has not been independently audited.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var receiver = MLKem768.Create();
/// receiver.GenerateKey();
///
/// using var sender = MLKem768.Create();
/// sender.ImportEncapsulationKey(receiver.ExportEncapsulationKey());
/// (byte[] ciphertext, byte[] senderSecret) = sender.Encapsulate();
///
/// byte[] receiverSecret = receiver.Decapsulate(ciphertext);
/// // senderSecret and receiverSecret are identical.
///]]>
/// </code>
/// </example>
public abstract class MLKem
    : AsymmetricAlgorithm
{
    /// <summary>
    /// The size, in bytes, of the shared secret produced by encapsulation and decapsulation.
    /// </summary>
    public const int SharedSecretSizeInBytes = 32;

    /// <summary>
    /// The size, in bytes, of the private seed d ‖ z accepted by <see cref="ImportPrivateSeed" />.
    /// </summary>
    public const int PrivateSeedSizeInBytes = 64;

    private readonly MLKemParameters _parameters;
    private byte[]? _decapsulationKey;
    private byte[]? _encapsulationKey;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MLKem" /> class for a specific FIPS 203 parameter set.
    /// </summary>
    /// <param name="parameters">The parameter set implemented by the derived type.</param>
    /// <param name="parameterSetDesignator">
    /// The designator (512, 768, or 1024) reported through <see cref="AsymmetricAlgorithm.KeySize" />.
    /// </param>
    private protected MLKem(MLKemParameters parameters, int parameterSetDesignator)
    {
        _parameters = parameters;

        LegalKeySizesValue = [new KeySizes(parameterSetDesignator, parameterSetDesignator, 0)];
        KeySizeValue = parameterSetDesignator;
    }

    /// <inheritdoc />
    /// <returns>The string <c>"ML-KEM"</c>.</returns>
    public override string? KeyExchangeAlgorithm =>
        "ML-KEM";

    /// <inheritdoc />
    /// <returns>
    /// <see langword="null" />, because ML-KEM is a key-encapsulation mechanism and produces no signatures.
    /// </returns>
    public override string? SignatureAlgorithm =>
        null;

    /// <summary>
    /// Gets the size, in bytes, of the encoded encapsulation (public) key.
    /// </summary>
    /// <value>800, 1184, or 1568 depending on the parameter set.</value>
    /// <returns>The encapsulation key size for this parameter set.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public int EncapsulationKeySizeInBytes
    {
        get
        {
            ThrowIfDisposed();
            return _parameters.EncapsulationKeySize;
        }
    }

    /// <summary>
    /// Gets the size, in bytes, of the encoded decapsulation (private) key.
    /// </summary>
    /// <value>1632, 2400, or 3168 depending on the parameter set.</value>
    /// <returns>The decapsulation key size for this parameter set.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public int DecapsulationKeySizeInBytes
    {
        get
        {
            ThrowIfDisposed();
            return _parameters.DecapsulationKeySize;
        }
    }

    /// <summary>
    /// Gets the size, in bytes, of the ciphertext produced by encapsulation.
    /// </summary>
    /// <value>768, 1088, or 1568 depending on the parameter set.</value>
    /// <returns>The ciphertext size for this parameter set.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public int CiphertextSizeInBytes
    {
        get
        {
            ThrowIfDisposed();
            return _parameters.CiphertextSize;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the instance currently holds a decapsulation key.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when decapsulation key material is present; otherwise, <see langword="false" />.
    /// </value>
    /// <returns>
    /// <see langword="true" /> when <see cref="Decapsulate(ReadOnlySpan{byte})" /> and
    /// <see cref="ExportDecapsulationKey" /> are available.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public bool HasDecapsulationKey
    {
        get
        {
            ThrowIfDisposed();
            return _decapsulationKey is not null;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the instance currently holds an encapsulation key.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when encapsulation key material is present; otherwise, <see langword="false" />.
    /// </value>
    /// <returns>
    /// <see langword="true" /> when <see cref="Encapsulate()" /> and <see cref="ExportEncapsulationKey" /> are
    /// available.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public bool HasEncapsulationKey
    {
        get
        {
            ThrowIfDisposed();
            return _encapsulationKey is not null;
        }
    }

    /// <summary>
    /// Generates a fresh random key pair, replacing any existing key material on the instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <remarks>
    /// The two 32-byte seeds (d for key derivation, z for implicit rejection) are drawn from a cryptographically secure
    /// random source. Any previously held decapsulation key is zeroed before being replaced.
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
    /// Imports the FIPS 203 64-byte private seed d ‖ z and regenerates the full key pair from it, replacing any
    /// existing key material on the instance.
    /// </summary>
    /// <param name="seed">
    /// The 64-byte concatenation of the key-derivation seed d and the implicit-rejection seed z.
    /// </param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="seed" /> is not exactly 64 bytes long.</exception>
    public void ImportPrivateSeed(ReadOnlySpan<byte> seed)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(seed, PrivateSeedSizeInBytes, $"{_parameters.Name} private seed");

        SetKeysFromSeed(seed);
    }

    /// <summary>
    /// Imports an encoded FIPS 203 decapsulation key, replacing any existing key material on the instance.
    /// </summary>
    /// <param name="decapsulationKey">The encoded decapsulation key dk_PKE ‖ ek ‖ H(ek) ‖ z.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="decapsulationKey" /> does not have the exact parameter-set length, or fails the FIPS 203 §7.3
    /// hash-consistency and embedded-key checks.
    /// </exception>
    public void ImportDecapsulationKey(ReadOnlySpan<byte> decapsulationKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(
            decapsulationKey, _parameters.DecapsulationKeySize, $"{_parameters.Name} decapsulation");
        if (!MLKemEngine.ValidateDecapsulationKey(_parameters, decapsulationKey))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_EncodedKey,
                    $"{_parameters.Name} decapsulation",
                    "FIPS 203"),
                nameof(decapsulationKey));
        }

        CryptographyHelper.ClearAndNullify(ref _decapsulationKey);
        _decapsulationKey = decapsulationKey.ToArray();
        _encapsulationKey = decapsulationKey.Slice(384 * _parameters.K, _parameters.EncapsulationKeySize).ToArray();
    }

    /// <summary>
    /// Imports an encoded FIPS 203 encapsulation key, replacing any existing key material on the instance.
    /// </summary>
    /// <param name="encapsulationKey">The encoded encapsulation key ByteEncode₁₂(t̂) ‖ ρ.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="encapsulationKey" /> does not have the exact parameter-set length, or fails the FIPS 203 §7.2
    /// modulus check.
    /// </exception>
    /// <remarks>
    /// Any decapsulation key previously held by the instance is zeroed and discarded, leaving an encapsulate-only
    /// instance.
    /// </remarks>
    public void ImportEncapsulationKey(ReadOnlySpan<byte> encapsulationKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(
            encapsulationKey, _parameters.EncapsulationKeySize, $"{_parameters.Name} encapsulation");
        if (!MLKemEngine.ValidateEncapsulationKey(_parameters, encapsulationKey))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_EncodedKey,
                    $"{_parameters.Name} encapsulation",
                    "FIPS 203"),
                nameof(encapsulationKey));
        }

        CryptographyHelper.ClearAndNullify(ref _decapsulationKey);
        _encapsulationKey = encapsulationKey.ToArray();
    }

    /// <summary>
    /// Exports the encoded FIPS 203 decapsulation key.
    /// </summary>
    /// <returns>A fresh copy of the encoded decapsulation key.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a decapsulation key.</exception>
    public byte[] ExportDecapsulationKey()
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPrivateKey(_decapsulationKey is not null);

        return (byte[])_decapsulationKey!.Clone();
    }

    /// <summary>
    /// Exports the encoded FIPS 203 encapsulation key.
    /// </summary>
    /// <returns>A fresh copy of the encoded encapsulation key.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold an encapsulation key.</exception>
    public byte[] ExportEncapsulationKey()
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPublicKey(_encapsulationKey is not null);

        return (byte[])_encapsulationKey!.Clone();
    }

    /// <summary>
    /// Encapsulates a fresh shared secret to the instance's encapsulation key.
    /// </summary>
    /// <returns>The ciphertext to transmit and the 32-byte shared secret to keep.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold an encapsulation key.</exception>
    public (byte[] Ciphertext, byte[] SharedSecret) Encapsulate()
    {
        ThrowIfDisposed();

        byte[] ciphertext = new byte[_parameters.CiphertextSize];
        byte[] sharedSecret = new byte[SharedSecretSizeInBytes];
        Encapsulate(ciphertext, sharedSecret);

        return (ciphertext, sharedSecret);
    }

    /// <summary>
    /// Encapsulates a fresh shared secret to the instance's encapsulation key, writing the ciphertext and secret into
    /// the supplied spans.
    /// </summary>
    /// <param name="ciphertext">
    /// The span receiving the ciphertext. Must be exactly <see cref="CiphertextSizeInBytes" /> bytes.
    /// </param>
    /// <param name="sharedSecret">The span receiving the shared secret. Must be exactly 32 bytes.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">A span does not have its exact required length.</exception>
    /// <exception cref="CryptographicException">The instance does not hold an encapsulation key.</exception>
    public void Encapsulate(Span<byte> ciphertext, Span<byte> sharedSecret)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidDestinationLength(ciphertext, _parameters.CiphertextSize);
        CryptographyThrowHelper.ThrowIfInvalidDestinationLength(sharedSecret, SharedSecretSizeInBytes);
        CryptographyThrowHelper.ThrowIfNoPublicKey(_encapsulationKey is not null);

        Span<byte> m = stackalloc byte[32];
        CryptographyHelper.FillWithRandomBytes(m);

        MLKemEngine.Encapsulate(_parameters, _encapsulationKey, m, ciphertext, sharedSecret);
        CryptographyHelper.Clear(m);
    }

    /// <summary>
    /// Recovers the shared secret from a ciphertext using the instance's decapsulation key.
    /// </summary>
    /// <param name="ciphertext">The ciphertext received from the encapsulating party.</param>
    /// <returns>The 32-byte shared secret.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertext" /> does not have the exact parameter-set length.
    /// </exception>
    /// <exception cref="CryptographicException">The instance does not hold a decapsulation key.</exception>
    /// <remarks>
    /// A tampered ciphertext of the correct length does not throw: implicit rejection silently yields a key unrelated
    /// to the encapsulating party's, per FIPS 203.
    /// </remarks>
    public byte[] Decapsulate(ReadOnlySpan<byte> ciphertext)
    {
        byte[] sharedSecret = new byte[SharedSecretSizeInBytes];
        Decapsulate(ciphertext, sharedSecret);

        return sharedSecret;
    }

    /// <summary>
    /// Recovers the shared secret from a ciphertext using the instance's decapsulation key, writing it into the
    /// supplied span.
    /// </summary>
    /// <param name="ciphertext">The ciphertext received from the encapsulating party.</param>
    /// <param name="sharedSecret">The span receiving the shared secret. Must be exactly 32 bytes.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ciphertext" /> does not have the exact parameter-set length, or <paramref name="sharedSecret" />
    /// is not exactly 32 bytes.
    /// </exception>
    /// <exception cref="CryptographicException">The instance does not hold a decapsulation key.</exception>
    public void Decapsulate(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
    {
        ThrowIfDisposed();
        if (ciphertext.Length != _parameters.CiphertextSize)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_KemCiphertextLength,
                    _parameters.CiphertextSize,
                    _parameters.Name),
                nameof(ciphertext));
        }

        CryptographyThrowHelper.ThrowIfInvalidDestinationLength(sharedSecret, SharedSecretSizeInBytes);
        CryptographyThrowHelper.ThrowIfNoPrivateKey(_decapsulationKey is not null);

        MLKemEngine.Decapsulate(_parameters, _decapsulationKey, ciphertext, sharedSecret);
    }

    /// <summary>
    /// Releases the resources used by this instance, zeroing all decapsulation key material.
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
            CryptographyHelper.ClearAndNullify(ref _decapsulationKey);
            _encapsulationKey = null;
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Derives and stores the key pair from a 64-byte d ‖ z seed, zeroing any previously held decapsulation key.
    /// </summary>
    /// <param name="seed">The 64-byte private seed.</param>
    private void SetKeysFromSeed(ReadOnlySpan<byte> seed)
    {
        byte[] encapsulationKey = new byte[_parameters.EncapsulationKeySize];
        byte[] decapsulationKey = new byte[_parameters.DecapsulationKeySize];
        MLKemEngine.KeyGen(_parameters, seed[..32], seed[32..], encapsulationKey, decapsulationKey);

        CryptographyHelper.ClearAndNullify(ref _decapsulationKey);
        _decapsulationKey = decapsulationKey;
        _encapsulationKey = encapsulationKey;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> when the instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
