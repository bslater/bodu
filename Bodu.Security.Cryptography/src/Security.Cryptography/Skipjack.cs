// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skipjack.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a managed implementation of the Skipjack symmetric block cipher, exposing the <see cref="SkipjackBlockCipher"/> engine
/// through the standard <see cref="SymmetricAlgorithm"/> framework. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// Skipjack is a block cipher designed by the United States National Security Agency (NSA) and declassified in 1998. It operates on
/// 64-bit (8-byte) blocks, uses a fixed 80-bit (10-byte) key, and applies an unbalanced Feistel network across 32 rounds. Skipjack
/// is supported here for legacy and research scenarios only.
/// </para>
/// <para>
/// This class integrates with the .NET <see cref="SymmetricAlgorithm"/> framework and supports standard block cipher modes via the
/// <see cref="BlockMode"/> property. The default mode is <see cref="CipherBlockMode.CBC"/> with
/// <see cref="PaddingMode.PKCS7"/> padding.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Block size: 64 bits (8 bytes).</description></item>
///   <item><description>Key size: 80 bits (10 bytes), fixed.</description></item>
///   <item><description>32 rounds, unbalanced Feistel network.</description></item>
///   <item><description>Default mode: <see cref="CipherBlockMode.CBC"/>; default padding: <see cref="PaddingMode.PKCS7"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Skipjack.</strong> Only when reading or producing data encrypted under a Skipjack-based
/// legacy system, or when reproducing published test vectors for research. For any new design use
/// <see cref="System.Security.Cryptography.Aes"/> or another 128-bit block cipher.
/// </para>
/// <note type="important">
/// Because of its 80-bit key and 64-bit block, Skipjack offers no modern security margin and must not be used to protect sensitive
/// data in new applications. The 64-bit block size also exposes it to birthday-bound attacks (SWEET32) when large volumes of data
/// are encrypted under the same key. Prefer a modern cipher such as AES.
/// </note>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// // Legacy interop only — do not use Skipjack to protect new data.
/// using var skipjack = new Skipjack();
/// skipjack.Key = legacyKeyMaterial;       // exactly 10 bytes
/// skipjack.IV  = RandomNumberGenerator.GetBytes(8); // matches the 64-bit block
///
/// byte[] ciphertext = skipjack.Encrypt(legacyPlaintext);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/skipjack.html">Using Skipjack (guide with full encrypt / decrypt examples)</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Skipjack
    : SymmetricAlgorithm
{
    /// <summary>
    /// The Skipjack block size, in bits.
    /// </summary>
    internal const int BlockSizeBits = 64;

    /// <summary>
    /// The Skipjack key size, in bits. Skipjack defines a single fixed key length.
    /// </summary>
    internal const int KeySizeBits = 80;

    // Skipjack has a single fixed 64-bit block size; expressed as a single-entry range with skip size 0.
    private static readonly KeySizes[] s_skipjackBlockSizes = [new KeySizes(BlockSizeBits, BlockSizeBits, 0)];

    // Skipjack has a single fixed 80-bit key size; expressed as a single-entry range with skip size 0.
    private static readonly KeySizes[] s_skipjackKeySizes = [new KeySizes(KeySizeBits, KeySizeBits, 0)];

    private bool _disposed = false;

    private BlockPaddingMode _blockPadding = BlockPaddingMode.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="Skipjack"/> class with the fixed 80-bit key size and 64-bit block size, CBC
    /// cipher mode, and PKCS7 padding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="SymmetricAlgorithm.GenerateKey"/> and <see cref="SymmetricAlgorithm.GenerateIV"/> to produce random key
    /// material, or assign <see cref="SymmetricAlgorithm.Key"/> and <see cref="SymmetricAlgorithm.IV"/> directly before calling
    /// <see cref="CreateEncryptor(byte[], byte[])"/> or <see cref="CreateDecryptor(byte[], byte[])"/>.
    /// </para>
    /// </remarks>
    public Skipjack()
    {
        // Skipjack defines a single fixed key length and block size — neither is configurable.
        this.LegalKeySizesValue = s_skipjackKeySizes;
        this.LegalBlockSizesValue = s_skipjackBlockSizes;

        this.KeySizeValue = KeySizeBits;
        this.BlockSizeValue = BlockSizeBits;

        this.Padding = PaddingMode.PKCS7;
        this.Mode = CipherMode.CBC;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="CipherBlockMode"/> values. The default is <see cref="CipherBlockMode.CBC"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode"/> property for use with
    /// <see cref="BlockCipherModeFactory"/> and the extended set of modes it supports, including
    /// <see cref="CipherBlockMode.CTR"/> and <see cref="CipherBlockMode.OFB"/>, which are not available via the standard
    /// <see cref="CipherMode"/> enumeration.
    /// </para>
    /// </remarks>
    public CipherBlockMode BlockMode { get; set; } = CipherBlockMode.CBC;

    /// <summary>
    /// Gets or sets the extended padding mode used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="BlockPaddingMode"/> values. The default is <see cref="BlockPaddingMode.PKCS7"/>.
    /// </value>
    /// <remarks>
    /// When the assigned value has a matching member in <see cref="PaddingMode"/> (for example, PKCS7, Zeros,
    /// None), the inherited <see cref="SymmetricAlgorithm.Padding"/> is kept in sync. Extended modes with no
    /// <see cref="PaddingMode"/> equivalent (such as <see cref="BlockPaddingMode.ISO7816_4"/>) leave the base
    /// property unchanged.
    /// </remarks>
    public BlockPaddingMode BlockPadding
    {
        get => this._blockPadding;
        set
        {
            this._blockPadding = value;
            if (Enum.TryParse<PaddingMode>(value.ToString(), out PaddingMode mode) && Enum.IsDefined(mode))
                this.PaddingValue = mode;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Also synchronises <see cref="BlockPadding"/> when the assigned value has a matching member in
    /// <see cref="BlockPaddingMode"/>.
    /// </remarks>
    public override PaddingMode Padding
    {
        get => base.Padding;
        set
        {
            base.Padding = value;
            if (Enum.TryParse<BlockPaddingMode>(value.ToString(), out BlockPaddingMode bpm) && Enum.IsDefined(bpm))
                this._blockPadding = bpm;
        }
    }

    /// <summary>
    /// Creates a symmetric <see cref="Skipjack"/> decryptor using the specified key and initialisation vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be exactly 10 bytes (80 bits) in length. Must not be
    /// <see langword="null"/>.
    /// </param>
    /// <param name="rgbIV">
    /// The initialisation vector. Must be exactly 8 bytes (64 bits) in length and must not be <see langword="null"/> for any
    /// cipher mode other than ECB.
    /// </param>
    /// <returns>A symmetric <see cref="Skipjack"/> decryptor object implementing <see cref="ICryptoTransform"/>.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey"/> or <paramref name="rgbIV"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey"/> is not exactly 10 bytes in length, or <paramref name="rgbIV"/> is not exactly 8 bytes in length.
    /// </exception>
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new SkipjackTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, false);
    }

    /// <summary>
    /// Creates a symmetric <see cref="Skipjack"/> encryptor using the specified key and initialisation vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be exactly 10 bytes (80 bits) in length. Must not be
    /// <see langword="null"/>.
    /// </param>
    /// <param name="rgbIV">
    /// The initialisation vector. Must be exactly 8 bytes (64 bits) in length and must not be <see langword="null"/> for any
    /// cipher mode other than ECB.
    /// </param>
    /// <returns>A symmetric <see cref="Skipjack"/> encryptor object implementing <see cref="ICryptoTransform"/>.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey"/> or <paramref name="rgbIV"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey"/> is not exactly 10 bytes in length, or <paramref name="rgbIV"/> is not exactly 8 bytes in length.
    /// </exception>
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new SkipjackTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, true);
    }

    /// <summary>
    /// Generates a cryptographically random initialisation vector (<see cref="SymmetricAlgorithm.IV"/>) suitable for use with the
    /// Skipjack algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated IV length is determined by the algorithm block size.
    /// </para>
    /// <para>
    /// The generated IV is assigned to <see cref="SymmetricAlgorithm.IV"/>.
    /// </para>
    /// <para>
    /// A new IV should be generated for each independent encryption operation when reusing a <see cref="Skipjack"/> instance with
    /// the same key.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeBytes);
    }

    /// <summary>
    /// Generates a cryptographically random key for use with the Skipjack algorithm using the currently configured
    /// <see cref="SymmetricAlgorithm.KeySize"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated key length is determined by <see cref="SymmetricAlgorithm.KeySize"/>.
    /// </para>
    /// <para>
    /// The generated key is assigned to <see cref="SymmetricAlgorithm.Key"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeBytes);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Skipjack"/> instance and, optionally, the managed resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged
    /// resources.
    /// </param>
    /// <remarks>
    /// Clears any cached key material and initialisation vector before delegating to the base implementation. This method is
    /// idempotent and safe to call multiple times.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                // Zero sensitive key material and IV buffers so their contents do not linger in managed memory after disposal.
                CryptoHelpers.Clear(this.Key);
                CryptoHelpers.Clear(this.IVValue!);
            }

            this._disposed = true;
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Gets the configured block size expressed in bytes.
    /// </summary>
    private int BlockSizeBytes => this.BlockSizeValue / 8;

    /// <summary>
    /// Gets the configured key size expressed in bytes.
    /// </summary>
    private int KeySizeBytes => this.KeySizeValue / 8;

    /// <summary>
    /// Creates a new <see cref="SkipjackBlockCipher"/> engine initialised with the supplied key.
    /// </summary>
    /// <param name="key">The 10-byte key material used to derive the round subkeys.</param>
    /// <returns>An <see cref="IBlockCipher"/> configured for single-block encryption and decryption.</returns>
    private static IBlockCipher CreateCipher(byte[] key) => new SkipjackBlockCipher(key);

    /// <summary>
    /// Validates that <paramref name="key"/> and <paramref name="iv"/> match the algorithm's configured key size and block size
    /// respectively.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <param name="iv">The initialisation vector to validate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="iv"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">The key or IV length is not valid for this algorithm.</exception>
    private void Validate(byte[] key, byte[]? iv)
    {
        ThrowHelper.ThrowIfNull(key);

        // Key length must match the fixed 80-bit key size exactly — Skipjack does not support variable-length keys.
        if (key.Length != this.KeySizeBytes)
            throw new CryptographicException(
                string.Format(
                    CryptoResourceStrings.CryptographicException_InvalidKeySize,
                    key.Length * 8,
                    CryptoHelpers.FormatLegalSizes(this.LegalKeySizesValue)),
                nameof(key));

        CryptoHelpers.ThrowIfInvalidIVForMode(iv, this.BlockMode, this.BlockSizeBytes, this.LegalBlockSizesValue);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }
}
