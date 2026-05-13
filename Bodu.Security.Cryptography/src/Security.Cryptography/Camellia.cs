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
/// <see cref="CamelliaBlockCipher"/> engine through the standard <see cref="SymmetricAlgorithm"/> framework.
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
/// This class integrates with the .NET <see cref="SymmetricAlgorithm"/> framework and supports standard block
/// cipher modes via the <see cref="BlockMode"/> property. The default mode is <see cref="CipherModeKind.CBC"/>
/// with <see cref="PaddingMode.PKCS7"/> padding and a default key size of 256 bits.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
///   <item><description>Block size: 128 bits (16 bytes).</description></item>
///   <item><description>Key sizes: 128 (18 rounds), 192 or 256 bits (24 rounds).</description></item>
///   <item><description>Specification: RFC 3713; approved by ISO/IEC, CRYPTREC, and NESSIE.</description></item>
///   <item><description>Default mode: <see cref="CipherModeKind.CBC"/>; default padding: <see cref="PaddingMode.PKCS7"/>.</description></item>
/// </list>
/// <para>
/// <strong>When to choose Camellia.</strong> Pick Camellia when standards-body approval matters — Japanese
/// CRYPTREC, European NESSIE, and ISO/IEC all list Camellia. It is the standard alternative to AES in many
/// non-US jurisdictions and TLS implementations. Performance characteristics are comparable to AES in software;
/// for new general-purpose work without a procurement constraint <see cref="System.Security.Cryptography.Aes"/>
/// is the more widely accelerated default.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using System.Security.Cryptography;
/// using Bodu.Security.Cryptography;
/// using Bodu.Security.Cryptography.Extensions;
///
/// using var camellia = new Camellia();
/// camellia.GenerateKey(); // 256-bit by default
/// camellia.GenerateIV();
///
/// byte[] ciphertext = camellia.Encrypt(plaintext);
/// byte[] roundTrip  = camellia.Decrypt(ciphertext);
/// </code>
/// </example>
/// <seealso href="../guides/cryptography/camellia.html">Using Camellia</seealso>
/// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
/// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
/// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
public sealed class Camellia
    : SymmetricAlgorithm
{
    /// <summary>
    /// Length of the Camellia block is 128 bits (16 bytes).
    /// </summary>
    internal const int BlockSizeBits = 128;

    /// <summary>
    /// Length of the minimum permitted Camellia key is 128 bits (16 bytes).
    /// </summary>
    internal const int MinKeySize = 128;

    /// <summary>
    /// Length of the maximum permitted Camellia key is 256 bits (32 bytes).
    /// </summary>
    internal const int MaxKeySize = 256;

    private static readonly KeySizes[] s_camelliaBlockSizes = [new KeySizes(BlockSizeBits, BlockSizeBits, 0)];
    private static readonly KeySizes[] s_camelliaKeySizes = [new KeySizes(128, 256, 64)];

    private bool _disposed;

    private PaddingModeKind _blockPadding = PaddingModeKind.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="Camellia"/> class with a 256-bit default key size, CBC cipher
    /// mode, and PKCS7 padding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="SymmetricAlgorithm.GenerateKey"/> and <see cref="SymmetricAlgorithm.GenerateIV"/> to produce
    /// random key material, or assign <see cref="SymmetricAlgorithm.Key"/> and <see cref="SymmetricAlgorithm.IV"/>
    /// directly before calling <see cref="CreateEncryptor(byte[], byte[])"/> or
    /// <see cref="CreateDecryptor(byte[], byte[])"/>.
    /// </para>
    /// <para>
    /// The key size can be changed via <see cref="SymmetricAlgorithm.KeySize"/> to 128, 192, or 256 bits prior to
    /// generating or assigning a key.
    /// </para>
    /// </remarks>
    public Camellia()
    {
        this.LegalBlockSizesValue = s_camelliaBlockSizes;
        this.LegalKeySizesValue = s_camelliaKeySizes;

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
    /// One of the <see cref="CipherModeKind"/> values. The default is <see cref="CipherModeKind.CBC"/>.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode"/> property for use with
    /// <see cref="BlockCipherModeFactory"/> and the extended set of modes it supports, including
    /// <see cref="CipherModeKind.CTR"/> and <see cref="CipherModeKind.OFB"/>, which are not available via the
    /// standard <see cref="CipherMode"/> enumeration.
    /// </para>
    /// </remarks>
    public CipherModeKind BlockMode { get; set; } = CipherModeKind.CBC;

    /// <summary>
    /// Gets or sets the extended padding mode used when creating encryptors and decryptors.
    /// </summary>
    /// <value>
    /// One of the <see cref="PaddingModeKind"/> values. The default is <see cref="PaddingModeKind.PKCS7"/>.
    /// </value>
    /// <remarks>
    /// When the assigned value has a matching member in <see cref="PaddingMode"/> (for example, PKCS7, Zeros,
    /// None), the inherited <see cref="SymmetricAlgorithm.Padding"/> is kept in sync. Extended modes with no
    /// <see cref="PaddingMode"/> equivalent (such as <see cref="PaddingModeKind.ISO7816_4"/>) leave the base
    /// property unchanged.
    /// </remarks>
    public PaddingModeKind BlockPadding
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
    /// Also synchronizes <see cref="BlockPadding"/> when the assigned value has a matching member in
    /// <see cref="PaddingModeKind"/>.
    /// </remarks>
    public override PaddingMode Padding
    {
        get => base.Padding;
        set
        {
            base.Padding = value;
            if (Enum.TryParse<PaddingModeKind>(value.ToString(), out PaddingModeKind bpm) && Enum.IsDefined(bpm))
                this._blockPadding = bpm;
        }
    }

    /// <summary>
    /// Creates a new <see cref="Camellia"/> instance with default parameters.
    /// </summary>
    /// <returns>A new <see cref="Camellia"/> instance.</returns>
    public new static Camellia Create() => new Camellia();

    /// <summary>
    /// Creates a symmetric <see cref="Camellia"/> decryptor using the specified key and initialization vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be exactly 16, 24, or 32 bytes (128, 192, or 256 bits) in
    /// length. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="rgbIV">
    /// The initialization vector. Must be exactly 16 bytes (128 bits) in length. Must not be
    /// <see langword="null"/>.
    /// </param>
    /// <returns>A symmetric <see cref="Camellia"/> decryptor object implementing <see cref="ICryptoTransform"/>.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey"/> or <paramref name="rgbIV"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey"/> is not 16, 24, or 32 bytes in length, or <paramref name="rgbIV"/> is not exactly
    /// 16 bytes in length.
    /// </exception>
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySize, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSize, this.LegalBlockSizes);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new CamelliaTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, false);
    }

    /// <summary>
    /// Creates a symmetric <see cref="Camellia"/> encryptor using the specified key and initialization vector.
    /// </summary>
    /// <param name="rgbKey">
    /// The secret key for the symmetric algorithm. Must be exactly 16, 24, or 32 bytes (128, 192, or 256 bits) in
    /// length. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="rgbIV">
    /// The initialization vector. Must be exactly 16 bytes (128 bits) in length. Must not be
    /// <see langword="null"/>.
    /// </param>
    /// <returns>A symmetric <see cref="Camellia"/> encryptor object implementing <see cref="ICryptoTransform"/>.</returns>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey"/> or <paramref name="rgbIV"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">
    /// <paramref name="rgbKey"/> is not 16, 24, or 32 bytes in length, or <paramref name="rgbIV"/> is not exactly
    /// 16 bytes in length.
    /// </exception>
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        this.ThrowIfDisposed();
        CryptoHelpers.ThrowIfInvalidKeySize(rgbKey, this.KeySize, this.LegalKeySizes);
        CryptoHelpers.ThrowIfInvalidIVForMode(rgbIV, this.BlockMode, this.BlockSize, this.LegalBlockSizes);

        IBlockCipher engine = CreateCipher(rgbKey);
        return new CamelliaTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, true);
    }

    /// <summary>
    /// Generates a cryptographically random initialization vector (<see cref="SymmetricAlgorithm.IV"/>) suitable
    /// for use with the Camellia algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated IV is 16 bytes (128 bits) — matching the Camellia block size — and is assigned directly to
    /// <see cref="SymmetricAlgorithm.IV"/>.
    /// </para>
    /// <para>
    /// A new IV should be generated for each independent encryption operation when reusing a <see cref="Camellia"/>
    /// instance with the same key.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeValue / 8);
    }

    /// <summary>
    /// Generates a cryptographically random key for use with the Camellia algorithm using the currently configured
    /// <see cref="SymmetricAlgorithm.KeySize"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated key length is determined by <see cref="SymmetricAlgorithm.KeySize"/>, which defaults to 256
    /// bits. The generated key is assigned directly to <see cref="SymmetricAlgorithm.Key"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">The current instance has been disposed.</exception>
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeBytes);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Camellia"/> instance and, optionally, the managed
    /// resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release
    /// only unmanaged resources.
    /// </param>
    /// <remarks>
    /// Clears any cached key material and initialization vector before delegating to the base implementation. This
    /// method is idempotent and safe to call multiple times.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Zero sensitive key material and IV buffers so their contents do not linger in managed memory.
                CryptoHelpers.Clear(this.KeyValue);
                CryptoHelpers.Clear(this.IVValue);
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }


    /// <summary>
    /// Gets the configured key size expressed in bytes.
    /// </summary>
    /// <returns>The key size in bytes.</returns>
    private int KeySizeBytes => this.KeySizeValue / 8;

    /// <summary>
    /// Creates a new <see cref="CamelliaBlockCipher"/> engine initialized with the supplied key.
    /// </summary>
    /// <param name="key">The key material used to derive the round subkeys.</param>
    /// <returns>An <see cref="IBlockCipher"/> configured for single-block encryption and decryption.</returns>
    private static IBlockCipher CreateCipher(byte[] key) => new CamelliaBlockCipher(key);

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
