// ---------------------------------------------------------------------------------------------------------------
// <copyright file="X25519.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the X25519 elliptic-curve Diffie-Hellman key agreement function defined in RFC
/// 7748, exposed through the standard <see cref="AsymmetricAlgorithm" /> framework. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// X25519 performs scalar multiplication on the Montgomery form of Curve25519, providing a 128-bit security level with
/// 32-byte keys and a 32-byte shared secret. Two parties each generate a key pair, exchange public keys, and call
/// <see cref="DeriveSharedSecret(ReadOnlySpan{byte})" /> with the peer's public key to arrive at the same shared
/// secret. The shared secret is a raw curve point coordinate and should be passed through a key derivation function
/// (such as HKDF or <see cref="Blake2b" /> in keyed mode) before use as symmetric key material.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Private key: 32 bytes; clamped per RFC 7748 §5 at the point of use, never in stored form.</description>
/// </item>
/// <item>
/// <description>
/// Public key: 32 bytes (the little-endian u-coordinate of the scalar multiple of the base point).
/// </description>
/// </item>
/// <item>
/// <description>Shared secret: 32 bytes; security level: 128 bits.</description>
/// </item>
/// <item>
/// <description>Specification: RFC 7748.</description>
/// </item>
/// </list>
/// <para>
/// <strong>Low-order point rejection.</strong> When the peer public key is one of the small set of low-order points,
/// every scalar produces an all-zero shared secret that an attacker can predict without knowing the private key.
/// <see cref="DeriveSharedSecret(ReadOnlySpan{byte})" /> applies the check recommended by RFC 7748 §6.1 strictly and
/// throws <see cref="CryptographicException" /> instead of returning the all-zero output.
/// </para>
/// <para>
/// <strong>Key formats.</strong> Only the raw RFC 7748 32-byte key encodings are supported via
/// <see cref="ImportPrivateKey" />, <see cref="ImportPublicKey" />, <see cref="ExportPrivateKey" />, and
/// <see cref="ExportPublicKey" />, plus the RFC 8410 PKCS#8 / SubjectPublicKeyInfo DER containers (OID 1.3.101.110) and
/// the RFC 7468 PEM helpers inherited from <see cref="AsymmetricAlgorithm" /> (<c>ImportFromPem</c>,
/// <c>ExportPkcs8PrivateKeyPem</c>, <c>ExportSubjectPublicKeyInfoPem</c>). The XML members and encrypted PKCS#8 are out
/// of scope and throw <see cref="NotSupportedException" />.
/// </para>
/// <para>
/// Scalar multiplication runs in constant time with respect to the private key. Private key material is zeroed when the
/// instance is disposed. This implementation, like the rest of the library, offers best-effort side-channel resistance
/// and has not been independently audited.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using var alice = X25519.Create();
/// using var bob = X25519.Create();
/// alice.GenerateKey();
/// bob.GenerateKey();
///
/// byte[] aliceShared = alice.DeriveSharedSecret(bob.ExportPublicKey());
/// byte[] bobShared = bob.DeriveSharedSecret(alice.ExportPublicKey());
/// // aliceShared and bobShared are identical.
///]]>
/// </code>
/// </example>
public sealed partial class X25519
    : RawKeyAsymmetricAlgorithm
{
    /// <summary>The size, in bytes, of an X25519 private key and public key (32 bytes each).</summary>
    public const int KeySizeInBytes = 32;

    /// <summary>The size, in bytes, of the shared secret produced by <see cref="DeriveSharedSecret(ReadOnlySpan{byte})" />.</summary>
    public const int SharedSecretSizeInBytes = 32;

    /// <summary>The fixed key size, in bits, reported through <see cref="AsymmetricAlgorithm.KeySize" />.</summary>
    private const int KeySizeBits = 256;

    /// <summary>The legal key sizes reported through <see cref="AsymmetricAlgorithm.LegalKeySizes" />, fixed at 256 bits.</summary>
    private static readonly KeySizes[] s_legalKeySizes = [new KeySizes(KeySizeBits, KeySizeBits, 0)];

    /// <summary>The canonical encodings of the Curve25519 u-coordinates whose order divides the cofactor, in the form used by <see cref="IsLowOrderPoint" />. The ignored high bit of byte 31 is cleared in each entry so the check can mask the input identically; the list matches the small-order table used by reference implementations such as libsodium (0, 1, the two order-8 u-coordinates, and the non-reduced encodings of p − 1, p, and p + 1).</summary>
    private static readonly byte[][] s_lowOrderPoints =
    [
        Convert.FromHexString("0000000000000000000000000000000000000000000000000000000000000000"),
        Convert.FromHexString("0100000000000000000000000000000000000000000000000000000000000000"),
        Convert.FromHexString("e0eb7a7c3b41b8ae1656e3faf19fc46ada098deb9c32b1fd866205165f49b800"),
        Convert.FromHexString("5f9c95bca3508c24b1d0b1559c83ef5b04445cc4581c8e86d8224eddd09f1157"),
        Convert.FromHexString("ecffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
        Convert.FromHexString("edffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
        Convert.FromHexString("eeffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff7f"),
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="X25519" /> class with no key material.
    /// </summary>
    /// <remarks>
    /// Call <see cref="GenerateKey" /> to create a fresh key pair, or import existing material with
    /// <see cref="ImportPrivateKey" /> or <see cref="ImportPublicKey" /> before deriving a shared secret.
    /// </remarks>
    public X25519()
    {
        LegalKeySizesValue = s_legalKeySizes;
        KeySizeValue = KeySizeBits;
    }

    /// <summary>
    /// Creates a new <see cref="X25519" /> instance with no key material.
    /// </summary>
    /// <returns>A new <see cref="X25519" /> instance.</returns>
    public static new X25519 Create() =>
        new();

    /// <inheritdoc />
    /// <value>The string <c>"X25519"</c>.</value>
    public override string? KeyExchangeAlgorithm =>
        "X25519";

    /// <inheritdoc />
    /// <value><see langword="null" />, because X25519 is a key agreement algorithm and produces no signatures.</value>
    public override string? SignatureAlgorithm =>
        null;

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
            return KeyMaterial?.HasPrivateKey ?? false;
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
            return KeyMaterial is not null;
        }
    }

    /// <summary>
    /// Generates a fresh random key pair, replacing any existing key material on the instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <remarks>
    /// The private key is drawn from a cryptographically secure random source. Any previously held private key is
    /// zeroed before being replaced.
    /// </remarks>
    public void GenerateKey()
    {
        ThrowIfDisposed();

        byte[] privateKey = CryptographyHelper.GetRandomBytes(KeySizeInBytes);
        SetPrivateKey(privateKey);
    }

    /// <summary>
    /// Imports a raw 32-byte RFC 7748 private key and derives the matching public key, replacing any existing key
    /// material on the instance.
    /// </summary>
    /// <param name="privateKey">
    /// The 32-byte private scalar to import. The caller's buffer is copied and never modified.
    /// </param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="privateKey" /> is not exactly 32 bytes long.</exception>
    /// <remarks>
    /// The scalar is stored exactly as supplied; RFC 7748 clamping is applied at the point of use, so the value
    /// returned by <see cref="ExportPrivateKey" /> round-trips byte-for-byte.
    /// </remarks>
    public void ImportPrivateKey(ReadOnlySpan<byte> privateKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(privateKey, KeySizeInBytes, "X25519 private");

        SetPrivateKey(privateKey.ToArray());
    }

    /// <summary>
    /// Imports a raw 32-byte RFC 7748 public key, replacing any existing key material on the instance.
    /// </summary>
    /// <param name="publicKey">The 32-byte public u-coordinate to import.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="publicKey" /> is not exactly 32 bytes long.</exception>
    /// <remarks>
    /// <para>
    /// This import is purely syntactic: it validates only the 32-byte length and stores the u-coordinate verbatim. It
    /// does not prove the point is contributory or reject low-order points — RFC 7748 defines X25519 over u-coordinates
    /// and places the all-zero-output check at key-agreement time.
    /// <see cref="DeriveSharedSecret(ReadOnlySpan{byte})" /> performs that mandatory rejection;
    /// <see cref="IsLowOrderPoint" /> is available for callers that want to screen a peer key before agreement.
    /// </para>
    /// <para>
    /// Any private key previously held by the instance is zeroed and discarded, leaving a public-only instance whose
    /// key can be exported but that cannot derive shared secrets.
    /// </para>
    /// </remarks>
    public void ImportPublicKey(ReadOnlySpan<byte> publicKey)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(publicKey, KeySizeInBytes, "X25519 public");

        ReplaceKeyMaterial(AsymmetricKeyMaterial.ForPublicKey(publicKey.ToArray()));
    }

    /// <summary>
    /// Determines whether <paramref name="publicKey" /> is one of the known low-order Curve25519 u-coordinates, for
    /// which X25519 yields an all-zero shared secret that an observer can predict without the private key.
    /// </summary>
    /// <param name="publicKey">The 32-byte peer u-coordinate to screen.</param>
    /// <returns>
    /// <see langword="true" /> when the encoding (ignoring its high bit, per RFC 7748) is a low-order point; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="publicKey" /> is not exactly 32 bytes long.</exception>
    /// <remarks>
    /// This is an optional preflight for callers that prefer to reject a peer key before agreement, for example to
    /// avoid even attempting a derivation. It is not required for safety:
    /// <see cref="DeriveSharedSecret(ReadOnlySpan{byte})" /> already rejects the all-zero result that these points
    /// produce. The comparison clears the ignored bit 255 of the input so non-canonical high-bit-set encodings of the
    /// same coordinate are also detected.
    /// </remarks>
    public static bool IsLowOrderPoint(ReadOnlySpan<byte> publicKey)
    {
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(publicKey, KeySizeInBytes, "X25519 public");

        Span<byte> masked = stackalloc byte[KeySizeInBytes];
        publicKey.CopyTo(masked);
        masked[31] &= 0x7f;

        int found = 0;
        foreach (byte[] point in s_lowOrderPoints)
        {
            int difference = 0;
            for (int i = 0; i < KeySizeInBytes; i++)
                difference |= masked[i] ^ point[i];

            // difference == 0 → ((difference - 1) >> 31) is all ones; accumulate without a data-dependent branch.
            found |= ((difference - 1) >> 31) & 1;
        }

        CryptographyHelper.Clear(masked);

        return found != 0;
    }

    /// <summary>
    /// Exports the raw 32-byte RFC 7748 private key.
    /// </summary>
    /// <returns>A fresh copy of the 32-byte private scalar exactly as generated or imported.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a private key.</exception>
    public byte[] ExportPrivateKey()
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPrivateKey(KeyMaterial?.HasPrivateKey ?? false);

        return (byte[])KeyMaterial!.PrivateKey!.Clone();
    }

    /// <summary>
    /// Exports the raw 32-byte RFC 7748 public key.
    /// </summary>
    /// <returns>A fresh copy of the 32-byte public u-coordinate.</returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="CryptographicException">The instance does not hold a public key.</exception>
    public byte[] ExportPublicKey()
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfNoPublicKey(KeyMaterial is not null);

        return (byte[])KeyMaterial!.PublicKey.Clone();
    }

    /// <summary>
    /// Derives the 32-byte X25519 shared secret between this instance's private key and the supplied peer public key.
    /// </summary>
    /// <param name="peerPublicKey">The peer's 32-byte RFC 7748 public key.</param>
    /// <returns>
    /// The 32-byte shared secret. Pass the value through a KDF before using it as symmetric key material.
    /// </returns>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException"><paramref name="peerPublicKey" /> is not exactly 32 bytes long.</exception>
    /// <exception cref="CryptographicException">
    /// The instance does not hold a private key, or <paramref name="peerPublicKey" /> is a low-order point whose shared
    /// secret would be all zero.
    /// </exception>
    public byte[] DeriveSharedSecret(ReadOnlySpan<byte> peerPublicKey)
    {
        byte[] sharedSecret = new byte[SharedSecretSizeInBytes];
        DeriveSharedSecret(peerPublicKey, sharedSecret);

        return sharedSecret;
    }

    /// <summary>
    /// Derives the 32-byte X25519 shared secret between this instance's private key and the supplied peer public key,
    /// writing it into <paramref name="destination" />.
    /// </summary>
    /// <param name="peerPublicKey">The peer's 32-byte RFC 7748 public key.</param>
    /// <param name="destination">The 32-byte span that receives the shared secret.</param>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="peerPublicKey" /> or <paramref name="destination" /> is not exactly 32 bytes long.
    /// </exception>
    /// <exception cref="CryptographicException">
    /// The instance does not hold a private key, or <paramref name="peerPublicKey" /> is a low-order point whose shared
    /// secret would be all zero. The destination is zeroed before the exception is thrown.
    /// </exception>
    public void DeriveSharedSecret(ReadOnlySpan<byte> peerPublicKey, Span<byte> destination)
    {
        ThrowIfDisposed();
        CryptographyThrowHelper.ThrowIfInvalidRawKeyLength(peerPublicKey, KeySizeInBytes, "X25519 public");
        CryptographyThrowHelper.ThrowIfInvalidDestinationLength(destination, SharedSecretSizeInBytes);
        CryptographyThrowHelper.ThrowIfNoPrivateKey(KeyMaterial?.HasPrivateKey ?? false);

        bool allZero = Curve25519.ScalarMult(KeyMaterial!.PrivateKey!, peerPublicKey, destination);

        // RFC 7748 §6.1 strict check: a low-order peer point collapses the secret to a value any observer can
        // predict; reject it rather than hand the caller attacker-known key material.
        if (allZero)
        {
            CryptographyHelper.Clear(destination);
            throw new CryptographicException(CryptoResourceStrings.Crypt_Invalid_X25519AllZeroSharedSecret);
        }
    }

    /// <summary>
    /// Stores the supplied private key array on the instance, deriving and caching the matching public key, and zeroing
    /// any previously held private key.
    /// </summary>
    /// <param name="privateKey">The 32-byte private key array to take ownership of.</param>
    private void SetPrivateKey(byte[] privateKey)
    {
        byte[] publicKey = new byte[KeySizeInBytes];
        Curve25519.ScalarMultBase(privateKey, publicKey);

        ReplaceKeyMaterial(AsymmetricKeyMaterial.ForKeyPair(publicKey, privateKey));
    }

}
