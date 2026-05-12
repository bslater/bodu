// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingSymmetricAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// A diagnostic symmetric algorithm that reverses the byte order of each block. Intended exclusively for use
/// in test harnesses and diagnostic scenarios. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SimpleReversingSymmetricAlgorithm" /> provides a deterministic, inspectable transform that
/// makes it straightforward to verify block alignment, padding behaviour, mode-of-operation chaining, and
/// streaming correctness without cryptographic complexity.
/// </para>
/// <para>
/// Encryption and decryption are identical operations — reversing a reversed block restores the original
/// input — so the same key material and IV can be used for both directions in round-trip tests.
/// </para>
/// <para>
/// The algorithm supports multiple block size ranges. The default block size is 128 bits (16 bytes). Each
/// created transform exposes a <see cref="SimpleReversingCryptoTransform.Diagnostics" /> property that
/// records every block-level and transform-level operation for post-hoc assertion in tests.
/// </para>
/// <note type="warning">This algorithm provides no cryptographic security and must not be used in production code.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var algo = new SimpleReversingSymmetricAlgorithm();
/// algo.GenerateKey();
/// algo.GenerateIV();
///
/// var encryptor = (SimpleReversingCryptoTransform)algo.CreateEncryptor();
/// byte[] ciphertext;
/// using (var ms = new MemoryStream())
/// using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
/// {
///     cs.Write(plaintext);
///     cs.FlushFinalBlock();
///     ciphertext = ms.ToArray();
/// }
///
/// // Inspect diagnostics after the operation.
/// Assert.AreEqual(expectedBlockCount, encryptor.Diagnostics.EncryptLog.Count);
/// </code>
/// </example>
public sealed class SimpleReversingSymmetricAlgorithm
    : SymmetricAlgorithm
{
    // ── Constants ─────────────────────────────────────────────────────────────────────────────

    /// <summary>The default block size in bits.</summary>
    public const int DefaultBlockSizeBits = 128;

    /// <summary>The default key size in bits.</summary>
    public const int DefaultKeySizeBits = 128;

    // ── Static legal size declarations ────────────────────────────────────────────────────────

    /// <summary>
    /// The legal block sizes supported by this algorithm.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>128, 192, 256 bits (step 64)</description></item>
    /// <item><description>448, 576 bits (step 128)</description></item>
    /// <item><description>1024, 1536, 2048 bits (step 512)</description></item>
    /// </list>
    /// </remarks>
    public static readonly KeySizes[] BlockSizesValue =
    {
        new KeySizes(128,  256,  64),
        new KeySizes(448,  576, 128),
        new KeySizes(1024, 2048, 512),
    };

    /// <summary>
    /// The legal key sizes supported by this algorithm.
    /// Keys may be any byte-aligned length between 8 and 2048 bits (step 8).
    /// </summary>
    public static readonly KeySizes[] KeySizesValue =
    {
        new KeySizes(8, 2048, 8),
    };

    // ── Instance fields ───────────────────────────────────────────────────────────────────────

    private bool disposed;
    private CipherBlockMode blockMode = CipherBlockMode.CBC;

    // ── Constructor ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a new instance of the <see cref="SimpleReversingSymmetricAlgorithm" /> class with default
    /// parameters: 128-bit block, 128-bit key, CBC cipher mode, and PKCS7 padding.
    /// </summary>
    public SimpleReversingSymmetricAlgorithm()
    {
        BlockSizeValue = DefaultBlockSizeBits;
        LegalBlockSizesValue = BlockSizesValue;

        KeySizeValue = DefaultKeySizeBits;
        LegalKeySizesValue = KeySizesValue;

        FeedbackSizeValue = DefaultBlockSizeBits;
        ModeValue = CipherMode.CBC;
        PaddingValue = PaddingMode.PKCS7;
    }

    // ── Public properties ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>One of the <see cref="CipherBlockMode" /> values. The default is <see cref="CipherBlockMode.CBC" />.</value>
    /// <remarks>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode" /> property to support the
    /// extended set of modes provided by <see cref="BlockCipherModeFactory" />, including
    /// <see cref="CipherBlockMode.CTR" /> and <see cref="CipherBlockMode.OFB" />.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public CipherBlockMode BlockMode
    {
        get
        {
            ThrowIfDisposed();
            return blockMode;
        }

        set
        {
            ThrowIfDisposed();
            blockMode = value;

            // Keep the inherited ModeValue in sync for callers that inspect it directly.
            if (Enum.TryParse<CipherMode>(value.ToString(), out CipherMode mode) && Enum.IsDefined(mode))
                ModeValue = mode;
        }
    }

    // ── Static factory ────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a new <see cref="SimpleReversingSymmetricAlgorithm" /> instance with default parameters.</summary>
    public static new SimpleReversingSymmetricAlgorithm Create() => new();

    // ── ICryptoTransform factory overrides ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="SimpleReversingCryptoTransform" /> configured for encryption.
    /// </summary>
    /// <param name="rgbKey">The key bytes. Must match the configured <see cref="SymmetricAlgorithm.KeySize" />.</param>
    /// <param name="rgbIV">The IV bytes. Must match the configured <see cref="SymmetricAlgorithm.BlockSize" />.</param>
    /// <returns>
    /// A <see cref="SimpleReversingCryptoTransform" /> that may be cast to access
    /// <see cref="SimpleReversingCryptoTransform.Diagnostics" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey" /> or <paramref name="rgbIV" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">The key or IV length does not match the configured sizes.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        ThrowIfDisposed();
        Validate(rgbKey, rgbIV);
        return new SimpleReversingCryptoTransform(
            CreateCipher(rgbKey, BlockSizeBytes),
            blockMode, PaddingValue, rgbIV, encrypt: true);
    }

    /// <summary>
    /// Creates a <see cref="SimpleReversingCryptoTransform" /> configured for decryption.
    /// </summary>
    /// <param name="rgbKey">The key bytes. Must match the configured <see cref="SymmetricAlgorithm.KeySize" />.</param>
    /// <param name="rgbIV">The IV bytes. Must match the configured <see cref="SymmetricAlgorithm.BlockSize" />.</param>
    /// <returns>
    /// A <see cref="SimpleReversingCryptoTransform" /> that may be cast to access
    /// <see cref="SimpleReversingCryptoTransform.Diagnostics" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rgbKey" /> or <paramref name="rgbIV" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">The key or IV length does not match the configured sizes.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
    {
        ThrowIfDisposed();
        Validate(rgbKey, rgbIV);
        return new SimpleReversingCryptoTransform(
            CreateCipher(rgbKey, BlockSizeBytes),
            blockMode, PaddingValue, rgbIV, encrypt: false);
    }

    // ── Key and IV generation ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Generates a cryptographically random initialisation vector of the configured block size and assigns
    /// it to <see cref="SymmetricAlgorithm.IV" />.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public override void GenerateIV()
    {
        ThrowIfDisposed();
        IVValue = CryptoHelpers.GetRandomNonZeroBytes(BlockSizeBytes);
    }

    /// <summary>
    /// Generates a cryptographically random key of the configured key size and assigns it to
    /// <see cref="SymmetricAlgorithm.Key" />.
    /// </summary>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public override void GenerateKey()
    {
        ThrowIfDisposed();
        KeyValue = CryptoHelpers.GetRandomNonZeroBytes(KeySizeBytes);
    }

    // ── Disposal ──────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                CryptoHelpers.Clear(KeyValue);
                CryptoHelpers.Clear(IVValue);
            }

            disposed = true;
        }

        base.Dispose(disposing);
    }

    // ── Private helpers ───────────────────────────────────────────────────────────────────────

    /// <summary>Gets the block size in bytes.</summary>
    private int BlockSizeBytes => BlockSizeValue / 8;

    /// <summary>Gets the key size in bytes.</summary>
    private int KeySizeBytes => KeySizeValue / 8;

    /// <summary>Returns a new <see cref="SimpleReversingBlockCipher" /> for the given key and block size.</summary>
    private static SimpleReversingBlockCipher CreateCipher(byte[] key, int blockSizeBytes)
        => new SimpleReversingBlockCipher(key, blockSizeBytes);

    /// <summary>
    /// Verifies that <paramref name="key" /> and <paramref name="iv" /> match the algorithm's configured
    /// key size and block size respectively.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> or <paramref name="iv" /> is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">The key or IV length is not valid for this algorithm.</exception>
    private void Validate(byte[] key, byte[]? iv)
    {
        ThrowHelper.ThrowIfNull(key);

        if (key.Length != KeySizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidKeySize,
                              key.Length * 8,
                              CryptoHelpers.FormatLegalSizes(LegalKeySizesValue)),
                nameof(key));

        CryptoHelpers.ThrowIfInvalidIVForMode(iv, blockMode, BlockSizeBytes, LegalBlockSizesValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (this.disposed) throw new ObjectDisposedException(nameof(SimpleReversingSymmetricAlgorithm));
#endif
    }
}
