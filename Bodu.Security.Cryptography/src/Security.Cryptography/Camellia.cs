// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Camellia.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Camellia symmetric block cipher, exposing the
/// <see cref="CamelliaBlockCipher" /> engine through the standard <see cref="SymmetricAlgorithm" /> framework.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Camellia is a symmetric-key block cipher jointly developed by NTT and Mitsubishi Electric and specified by
/// RFC 3713. It operates on a fixed 128-bit block size and accepts 128-bit, 192-bit, and 256-bit keys. The
/// 128-bit key variant applies 18 Feistel rounds; the 192-bit and 256-bit key variants apply 24 rounds. Camellia
/// has been evaluated and approved by ISO/IEC, CRYPTREC, and NESSIE.
/// </para>
/// <para>
/// This class integrates with the .NET <see cref="SymmetricAlgorithm" /> framework and supports standard block
/// cipher modes via the <see cref="BlockMode" /> property. The default mode is <see cref="CipherBlockMode.CBC" />
/// with <see cref="PaddingMode.PKCS7" /> padding and a default key size of 256 bits.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/camellia.html">Using Camellia</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Camellia
    : SymmetricAlgorithm
{
    /// <summary>
    /// The Camellia block size, in bits.
    /// </summary>
    internal const int BlockSizeBits = 128;

    /// <summary>
    /// The minimum permitted key size, in bytes.
    /// </summary>
    internal const int MinKeySizeBytes = 16;

    /// <summary>
    /// The maximum permitted key size, in bytes.
    /// </summary>
    internal const int MaxKeySizeBytes = 32;

    private static readonly KeySizes[] CamelliaBlockSizes = { new KeySizes(BlockSizeBits, BlockSizeBits, 0) };
    private static readonly KeySizes[] CamelliaKeySizes = { new KeySizes(128, 256, 64) };

    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="Camellia" /> class with a 256-bit default key size, CBC cipher
    /// mode, and PKCS7 padding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="SymmetricAlgorithm.GenerateKey" /> and <see cref="SymmetricAlgorithm.GenerateIV" /> to produce
    /// random key material, or assign <see cref="SymmetricAlgorithm.Key" /> and <see cref="SymmetricAlgorithm.IV" />
    /// directly before calling <see cref="CreateEncryptor(byte[], byte[])" /> or
    /// <see cref="CreateDecryptor(byte[], byte[])" />.
    /// </para>
    /// <para>
    /// The key size can be changed via <see cref="SymmetricAlgorithm.KeySize" /> to 128, 192, or 256 bits prior to
    /// generating or assigning a key.
    /// </para>
    /// </remarks>
    public Camellia()
    {
        this.LegalBlockSizesValue = CamelliaBlockSizes;
        this.LegalKeySizesValue = CamelliaKeySizes;

        this.BlockSizeValue = BlockSizeBits;
        this.KeySizeValue = 256;

        this.FeedbackSizeValue = BlockSizeBits;
        this.ModeValue = CipherMode.CBC;
        this.PaddingValue = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherBlockMode" /> values. The default is <see cref="CipherBlockMode.CBC" />.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property for use with
    /// <see cref="BlockCipherModeFactory" /> and the extended set of modes it supports, including
    /// <see cref="CipherBlockMode.CTR" /> and <see cref="CipherBlockMode.OFB" />, which are not available via the
    /// standard <see cref="CipherMode" /> enumeration.
    /// </para>
    /// </remarks>
    public CipherBlockMode BlockMode { get; set; } = CipherBlockMode.CBC;

    /// <summary>
    /// Creates a new <see cref="Camellia" /> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Camellia" /> instance.</returns>
    public new static Camellia Create() => new Camellia();

    /// <summary>
    /// Creates a symmetric <see cref="Camellia" /> decryptor using the specified key and initialisation vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be exactly 16, 24, or 32 bytes (128, 192, or 256 bits) in
    /// length. Must not be <see langword="null" />.
    /// </param>
    /// <param name="rgbIV">
    /// The initialisation vector. Must be exactly 16 bytes (128 bits) in length. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>A symmetric <see cref="Camellia" /> decryptor object implementing <see cref="ICryptoTransform" />.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey" /> or <paramref name="rgbIV" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey" /> is not 16, 24, or 32 bytes in length, or <paramref name="rgbIV" /> is not exactly
    /// 16 bytes in length.
    /// </exception>
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new CamelliaTransform(engine, this.BlockMode, this.Padding, rgbIV, false);
    }

    /// <summary>
    /// Creates a symmetric <see cref="Camellia" /> encryptor using the specified key and initialisation vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be exactly 16, 24, or 32 bytes (128, 192, or 256 bits) in
    /// length. Must not be <see langword="null" />.
    /// </param>
    /// <param name="rgbIV">
    /// The initialisation vector. Must be exactly 16 bytes (128 bits) in length. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>A symmetric <see cref="Camellia" /> encryptor object implementing <see cref="ICryptoTransform" />.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey" /> or <paramref name="rgbIV" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey" /> is not 16, 24, or 32 bytes in length, or <paramref name="rgbIV" /> is not exactly
    /// 16 bytes in length.
    /// </exception>
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new CamelliaTransform(engine, this.BlockMode, this.Padding, rgbIV, true);
    }

    /// <summary>
    /// Generates a cryptographically random initialisation vector (<see cref="SymmetricAlgorithm.IV" />) suitable
    /// for use with the Camellia algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated IV is 16 bytes (128 bits) — matching the Camellia block size — and is assigned directly to
    /// <see cref="SymmetricAlgorithm.IV" />.
    /// </para>
    /// <para>
    /// A new IV should be generated for each independent encryption operation when reusing a <see cref="Camellia" />
    /// instance with the same key.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeBytes);
    }

    /// <summary>
    /// Generates a cryptographically random key for use with the Camellia algorithm using the currently configured
    /// <see cref="SymmetricAlgorithm.KeySize" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated key length is determined by <see cref="SymmetricAlgorithm.KeySize" />, which defaults to 256
    /// bits. The generated key is assigned directly to <see cref="SymmetricAlgorithm.Key" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeBytes);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Camellia" /> instance and, optionally, the managed
    /// resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    /// <remarks>
    /// Clears any cached key material and initialisation vector before delegating to the base implementation. This
    /// method is idempotent and safe to call multiple times.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Zero sensitive key material and IV buffers so their contents do not linger in managed memory.
                CryptoHelpers.Clear(this.KeyValue!);
                CryptoHelpers.Clear(this.IVValue!);
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Gets the configured block size expressed in bytes.
    /// </summary>
    /// <returns>The block size in bytes.</returns>
    private int BlockSizeBytes => this.BlockSizeValue / 8;

    /// <summary>
    /// Gets the configured key size expressed in bytes.
    /// </summary>
    /// <returns>The key size in bytes.</returns>
    private int KeySizeBytes => this.KeySizeValue / 8;

    /// <summary>
    /// Creates a new <see cref="CamelliaBlockCipher" /> engine initialised with the supplied key.
    /// </summary>
    /// <param name="key">The key material used to derive the round subkeys.</param>
    /// <returns>An <see cref="IBlockCipher" /> configured for single-block encryption and decryption.</returns>
    private static IBlockCipher CreateCipher(byte[] key) => new CamelliaBlockCipher(key);

    /// <summary>
    /// Validates that <paramref name="key" /> and <paramref name="iv" /> match the algorithm's configured key size
    /// and block size respectively.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <param name="iv">The initialisation vector to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">The key or IV length is not valid for this algorithm.</exception>
    private void Validate(byte[] key, byte[]? iv)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(iv);

        if (key.Length != this.KeySizeBytes)
            throw new CryptographicException(
                string.Format(ResourceStrings.CryptographicException_InvalidKeySize,
                              key.Length * 8,
                              CryptoHelpers.FormatLegalSizes(this.LegalKeySizesValue)),
                nameof(key));

        if (iv!.Length != this.BlockSizeBytes)
            throw new CryptographicException(
                string.Format(ResourceStrings.CryptographicException_InvalidIVSize,
                              iv.Length * 8,
                              CryptoHelpers.FormatLegalSizes(this.LegalBlockSizesValue)),
                nameof(iv));
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(_disposed, this);
#else
        if (_disposed)
            throw new ObjectDisposedException(nameof(Camellia));
#endif
    }
}
