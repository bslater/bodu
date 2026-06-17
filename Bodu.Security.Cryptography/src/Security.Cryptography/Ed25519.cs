// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Ed25519 digital signature algorithm (PureEdDSA over edwards25519) as
/// defined in RFC 8032, exposed through the standard <see cref="AsymmetricAlgorithm" /> framework. This class cannot be
/// inherited.
/// </summary>
/// <remarks>
/// <para>
/// Ed25519 produces deterministic 64-byte signatures from a 32-byte private seed at a 128-bit security level. The same
/// message signed twice under the same key yields the identical signature; no signing nonce is consumed, which removes
/// the catastrophic nonce-reuse failure mode of ECDSA-style schemes.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Private key: 32-byte seed (RFC 8032 §5.1.5); public key: 32 bytes; signature: 64 bytes (R ‖ S).
/// </description>
/// </item>
/// <item>
/// <description>
/// Hash: SHA-512; security level: 128 bits; specification: RFC 8032 (Ed25519, "pure" variant).
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Scope.</strong> Only pure Ed25519 is implemented. The pre-hash (Ed25519ph) and context (Ed25519ctx) variants
/// of RFC 8032 are deliberately out of scope for this version. Only the raw RFC 8032 32-byte key encodings are
/// supported; the PKCS#8 / SubjectPublicKeyInfo members inherited from <see cref="AsymmetricAlgorithm" /> are not
/// implemented and retain their base (throwing) behavior.
/// </para>
/// <para>
/// <strong>Verification policy.</strong> <see cref="VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> applies the
/// cofactorless equation [S]B = R + [k]A and rejects signatures whose S component is at or above the group order (the
/// RFC 8032 malleability check) and points whose encodings are non-canonical. Malformed or wrong-length signatures
/// return <see langword="false" /> rather than throwing.
/// </para>
/// <para>
/// Signing runs in constant time with respect to the private key, and intermediate secrets (the expanded SHA-512 seed,
/// the per-signature scalar r) are zeroed before returning. Private key material is zeroed when the instance is
/// disposed. This implementation offers best-effort side-channel resistance and has not been independently audited.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var signer = Ed25519.Create();
/// signer.GenerateKey();
/// byte[] signature = signer.SignData(message);
///
/// using var verifier = Ed25519.Create();
/// verifier.ImportPublicKey(signer.ExportPublicKey());
/// bool valid = verifier.VerifyData(message, signature);
///]]>
/// </code>
/// </example>
public sealed class Ed25519
    : AsymmetricAlgorithm
{
    /// <summary>The size, in bytes, of an Ed25519 private key seed.</summary>
    public const int PrivateKeySizeInBytes = 32;

    /// <summary>The size, in bytes, of an Ed25519 public key.</summary>
    public const int PublicKeySizeInBytes = 32;

    /// <summary>The size, in bytes, of an Ed25519 signature.</summary>
    public const int SignatureSizeInBytes = 64;

    /// <summary>The fixed key size, in bits, reported through <see cref="AsymmetricAlgorithm.KeySize" />.</summary>
    private const int KeySizeBits = 256;

    /// <summary>The single legal key size reported through <see cref="AsymmetricAlgorithm.LegalKeySizes" />.</summary>
    private static readonly KeySizes[] s_legalKeySizes = [new KeySizes(KeySizeBits, KeySizeBits, 0)];

    /// <summary>Indicates whether this instance has been disposed.</summary>
    private bool _disposed;

    /// <summary>The 32-byte RFC 8032 private key seed, or <see langword="null" /> when no private key is held.</summary>
    private byte[]? _privateKey;

    /// <summary>The 32-byte RFC 8032 public key, or <see langword="null" /> when no public key is held.</summary>
    private byte[]? _publicKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ed25519" /> class with no key material.
    /// </summary>
    /// <remarks>
    /// Call <see cref="GenerateKey" /> to create a fresh key pair, or import existing material with
    /// <see cref="ImportPrivateKey" /> or <see cref="ImportPublicKey" /> before signing or verifying.
    /// </remarks>
    public Ed25519()
    {
        LegalKeySizesValue = s_legalKeySizes;
        KeySizeValue = KeySizeBits;
    }

    /// <summary>
    /// Gets a value indicating whether the instance currently holds a private key.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when private key material is present; otherwise, <see langword="false" />.
    /// </value>
    /// <returns>
    /// <see langword="true" /> when <see cref="SignData(ReadOnlySpan{byte})" /> and <see cref="ExportPrivateKey" /> are
    /// available.
    /// </returns>
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
    /// <returns>
    /// <see langword="true" /> when <see cref="VerifyData(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> and
    /// <see cref="ExportPublicKey" /> are available.
    /// </returns>
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
    /// <returns>
    /// <see langword="null" />, because Ed25519 is a signature algorithm and performs no key exchange.
    /// </returns>
    public override string? KeyExchangeAlgorithm =>
        null;

    /// <inheritdoc />
    /// <returns>The string <c>"Ed25519"</c>.</returns>
    public override string? SignatureAlgorithm =>
        "Ed25519";

    /// <summary>
    /// Creates a new <see cref="Ed25519" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="Ed25519" /> instance.</returns>
    public static new Ed25519 Create() =>
        new();

    /// <summary>
    /// Exports the raw 32-byte RFC 8032 private key seed.
    /// </summary>
    /// <returns>A fresh copy of the 32-byte seed exactly as generated or imported.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public byte[] ExportPrivateKey()
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPrivateKey(_privateKey is not null);

        return (byte[])_privateKey!.Clone();
    }

    /// <summary>
    /// Exports the raw 32-byte RFC 8032 public key.
    /// </summary>
    /// <returns>A fresh copy of the 32-byte compressed point encoding.</returns>
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
    /// The 32-byte seed is drawn from a cryptographically secure random source. Any previously held private key is
    /// zeroed before being replaced.
    /// </remarks>
    public void GenerateKey()
    {
        ThrowIfDisposed();

        byte[] privateKey = CryptographyHelper.GetRandomBytes(PrivateKeySizeInBytes);
        SetPrivateKey(privateKey);
    }

    /// <summary>
    /// Imports a raw 32-byte RFC 8032 private key seed and derives the matching public key, replacing any existing key
    /// material on the instance.
    /// </summary>
    /// <param name="privateKey">
    /// The 32-byte private seed to import. The caller's buffer is copied and never modified.
    /// </param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="privateKey" /> is not exactly 32 bytes long.</exception>
    public void ImportPrivateKey(ReadOnlySpan<byte> privateKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(privateKey, PrivateKeySizeInBytes, "Ed25519 private");

        SetPrivateKey(privateKey.ToArray());
    }

    /// <summary>
    /// Imports a raw 32-byte RFC 8032 public key, replacing any existing key material on the instance.
    /// </summary>
    /// <param name="publicKey">The 32-byte compressed point encoding to import.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="publicKey" /> is not exactly 32 bytes long, or is not a canonical encoding of a point on
    /// edwards25519.
    /// </exception>
    /// <remarks>
    /// The encoding is validated by fully decompressing the point; non-canonical y values and encodings without a
    /// square x² candidate are rejected. Any private key previously held by the instance is zeroed and discarded,
    /// leaving a verify-only instance.
    /// </remarks>
    public void ImportPublicKey(ReadOnlySpan<byte> publicKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(publicKey, PublicKeySizeInBytes, "Ed25519 public");
        if (!Ed25519Point.TryDecode(publicKey, out _))
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    CryptoResourceStrings.Arg_Invalid_EncodedKey,
                    "Ed25519 public",
                    "RFC 8032 point"),
                nameof(publicKey));
        }

        CryptographyHelper.ClearAndNullify(ref _privateKey);
        _publicKey = publicKey.ToArray();
    }

    /// <summary>
    /// Signs the supplied data with the instance's private key, producing the deterministic 64-byte Ed25519 signature.
    /// </summary>
    /// <param name="data">The message bytes to sign. May be empty.</param>
    /// <returns>The 64-byte signature R ‖ S.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public byte[] SignData(ReadOnlySpan<byte> data)
    {
        byte[] signature = new byte[SignatureSizeInBytes];
        SignData(data, signature);

        return signature;
    }

    /// <summary>
    /// Signs the supplied data with the instance's private key, writing the deterministic 64-byte Ed25519 signature
    /// into <paramref name="destination" />.
    /// </summary>
    /// <param name="data">The message bytes to sign. May be empty.</param>
    /// <param name="destination">The 64-byte span that receives the signature R ‖ S.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="destination" /> is not exactly 64 bytes long.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public void SignData(ReadOnlySpan<byte> data, Span<byte> destination)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidDestinationLength(destination, SignatureSizeInBytes);
        CryptographyThrowHelper.ThrowIfNoPrivateKey(_privateKey is not null);

        // RFC 8032 §5.1.6: expand the seed into the clamped scalar s and the deterministic-nonce prefix.
        Span<byte> expanded = stackalloc byte[64];
        SHA512.HashData(_privateKey, expanded);

        Span<byte> s = stackalloc byte[32];
        expanded[..32].CopyTo(s);
        s[0] &= 248;
        s[31] &= 127;
        s[31] |= 64;

        Span<byte> digest = stackalloc byte[64];
        Span<byte> r = stackalloc byte[32];

        using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512))
        {
            // r = SHA-512(prefix ‖ M) mod L, then R = [r]B.
            hash.AppendData(expanded[32..]);
            hash.AppendData(data);
            hash.GetHashAndReset(digest);
            Ed25519Scalar.Reduce(digest, r);

            Span<byte> rEncoded = destination[..32];
            Ed25519Point.ScalarMultBase(r).Encode(rEncoded);

            // S = (r + SHA-512(R ‖ A ‖ M) · s) mod L.
            hash.AppendData(rEncoded);
            hash.AppendData(_publicKey);
            hash.AppendData(data);
            hash.GetHashAndReset(digest);

            Span<byte> k = stackalloc byte[32];
            Ed25519Scalar.Reduce(digest, k);
            Ed25519Scalar.MulAdd(k, s, r, destination[32..]);
        }

        CryptographyHelper.Clear(expanded);
        CryptographyHelper.Clear(s);
        CryptographyHelper.Clear(digest);
        CryptographyHelper.Clear(r);
    }

    /// <summary>
    /// Verifies an Ed25519 signature over the supplied data against the instance's public key.
    /// </summary>
    /// <param name="data">The message bytes that were signed. May be empty.</param>
    /// <param name="signature">The candidate 64-byte signature R ‖ S.</param>
    /// <returns>
    /// <see langword="true" /> when the signature is valid; <see langword="false" /> for any invalid, malformed,
    /// non-canonical, or wrong-length signature.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a public key.</exception>
    /// <remarks>
    /// Verification never throws for bad signature input: every failure mode — wrong length, S ≥ L, a non-canonical or
    /// off-curve R — yields <see langword="false" />. Verification time may vary with the inputs, which is acceptable
    /// because all inputs to verification are public.
    /// </remarks>
    public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPublicKey(_publicKey is not null);

        if (signature.Length != SignatureSizeInBytes)
            return false;

        ReadOnlySpan<byte> rEncoded = signature[..32];
        ReadOnlySpan<byte> sEncoded = signature[32..];

        // Reject S >= L (RFC 8032 malleability check) and non-canonical / off-curve point encodings.
        if (!Ed25519Scalar.IsCanonical(sEncoded))
            return false;

        if (!Ed25519Point.TryDecode(rEncoded, out Ed25519Point rPoint))
            return false;

        if (!Ed25519Point.TryDecode(_publicKey, out Ed25519Point publicPoint))
            return false;

        // k = SHA-512(R ‖ A ‖ M) mod L; accept when [S]B == R + [k]A (cofactorless).
        Span<byte> digest = stackalloc byte[64];
        using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA512))
        {
            hash.AppendData(rEncoded);
            hash.AppendData(_publicKey);
            hash.AppendData(data);
            hash.GetHashAndReset(digest);
        }

        Span<byte> k = stackalloc byte[32];
        Ed25519Scalar.Reduce(digest, k);

        Span<byte> left = stackalloc byte[32];
        Span<byte> right = stackalloc byte[32];
        Ed25519Point.ScalarMultBase(sEncoded).Encode(left);
        rPoint.Add(Ed25519Point.ScalarMult(publicPoint, k)).Encode(right);

        return left.SequenceEqual(right);
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
    /// Stores the supplied private seed on the instance, deriving and caching the matching public key per RFC 8032
    /// §5.1.5, and zeroing any previously held private key.
    /// </summary>
    /// <param name="privateKey">The 32-byte seed array to take ownership of.</param>
    private void SetPrivateKey(byte[] privateKey)
    {
        Span<byte> expanded = stackalloc byte[64];
        SHA512.HashData(privateKey, expanded);

        Span<byte> scalar = expanded[..32];
        scalar[0] &= 248;
        scalar[31] &= 127;
        scalar[31] |= 64;

        byte[] publicKey = new byte[PublicKeySizeInBytes];
        Ed25519Point.ScalarMultBase(scalar).Encode(publicKey);
        CryptographyHelper.Clear(expanded);

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
