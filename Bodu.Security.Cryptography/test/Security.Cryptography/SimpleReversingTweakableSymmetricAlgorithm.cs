// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingTweakableSymmetricAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// A diagnostic tweakable symmetric algorithm that reverses the byte order of each block with tweak mixing.
/// Intended exclusively for use in test harnesses and diagnostic scenarios. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SimpleReversingTweakableSymmetricAlgorithm" /> extends <see cref="TweakableSymmetricAlgorithm" />
/// and reuses the same <see cref="SimpleReversingBlockCipher" /> and <see cref="SimpleReversingCryptoTransform" />
/// infrastructure as <see cref="SimpleReversingSymmetricAlgorithm" />, adding tweak support.
/// </para>
/// <para>
/// The tweak is incorporated into the block cipher as follows:
/// <list type="bullet">
/// <item><description><b>Encrypt:</b> XOR the plaintext block with the tweak (cycling), then reverse the bytes.</description></item>
/// <item><description><b>Decrypt:</b> Reverse the ciphertext block bytes, then XOR with the tweak (cycling).</description></item>
/// </list>
/// This guarantees <c>Decrypt(Encrypt(pt, tweak), tweak) == pt</c> for any tweak and any block size.
/// </para>
/// <para>
/// The tweak is cycled byte-by-byte if it is shorter than the configured block, and truncated if longer.
/// Each created transform exposes a <see cref="SimpleReversingCryptoTransform.Diagnostics" /> property that
/// records every block-level and transform-level operation for post-hoc assertion in tests.
/// </para>
/// <para>
/// Legal tweak sizes mirror the legal block sizes, allowing tests to exercise matching, mismatched, and
/// cycling tweak lengths across all supported block size ranges.
/// </para>
/// <note type="warning">This algorithm provides no cryptographic security and must not be used in production code.</note>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var algo = new SimpleReversingTweakableSymmetricAlgorithm();
/// algo.GenerateKey();
/// algo.GenerateIV();
/// algo.GenerateTweak();
///
/// var encryptor = (SimpleReversingCryptoTransform)algo.CreateEncryptor();
/// byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
///
/// var decryptor = (SimpleReversingCryptoTransform)algo.CreateDecryptor();
/// byte[] recovered = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
///
/// Assert.AreEqual(1, encryptor.Diagnostics.EncryptLog.Count);
/// Assert.AreEqual(1, decryptor.Diagnostics.DecryptLog.Count);
/// </code>
/// </example>
public sealed class SimpleReversingTweakableSymmetricAlgorithm
    : TweakableSymmetricAlgorithm
{
    // ── Constants ─────────────────────────────────────────────────────────────────────────────

    /// <summary>The default block size in bits.</summary>
    public const int DefaultBlockSizeBits = 128;

    /// <summary>The default key size in bits.</summary>
    public const int DefaultKeySizeBits = 128;

    /// <summary>The default tweak size in bits.</summary>
    public const int DefaultTweakSizeBits = 128;

    // ── Static legal size declarations ────────────────────────────────────────────────────────

    /// <summary>
    /// The legal block sizes supported by this algorithm, mirroring
    /// <see cref="SimpleReversingSymmetricAlgorithm.BlockSizesValue" />.
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

    /// <summary>
    /// The legal tweak sizes supported by this algorithm. Tweak sizes mirror the legal block sizes,
    /// allowing tests to exercise matching, mismatched, and cycling tweak configurations.
    /// </summary>
    public static readonly KeySizes[] TweakSizesValue =
    {
        new KeySizes(128,  256,  64),
        new KeySizes(448,  576, 128),
        new KeySizes(1024, 2048, 512),
    };

    // ── Instance fields ───────────────────────────────────────────────────────────────────────

    private bool disposed;
    private CipherBlockMode blockMode = CipherBlockMode.CBC;

    // ── Constructor ───────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a new instance of the <see cref="SimpleReversingTweakableSymmetricAlgorithm" /> class with
    /// default parameters: 128-bit block, 128-bit key, 128-bit tweak, CBC cipher mode, and PKCS7 padding.
    /// </summary>
    public SimpleReversingTweakableSymmetricAlgorithm()
    {
        BlockSizeValue = DefaultBlockSizeBits;
        LegalBlockSizesValue = BlockSizesValue;

        KeySizeValue = DefaultKeySizeBits;
        LegalKeySizesValue = KeySizesValue;

        TweakSizeValue = DefaultTweakSizeBits;
        LegalTweakSizesValue = TweakSizesValue;

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
            if (Enum.TryParse<CipherMode>(value.ToString(), out var mode) && Enum.IsDefined(mode))
                ModeValue = mode;
        }
    }

    // ── Static factory ────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a new <see cref="SimpleReversingTweakableSymmetricAlgorithm" /> instance with default parameters.</summary>
    public static new SimpleReversingTweakableSymmetricAlgorithm Create() => new();

    // ── ICryptoTransform factory overrides ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="SimpleReversingCryptoTransform" /> configured for encryption using the specified
    /// key, initialisation vector, and tweak.
    /// </summary>
    /// <param name="rgbKey">The key bytes. Must match the configured <see cref="SymmetricAlgorithm.KeySize" />.</param>
    /// <param name="rgbIV">The IV bytes. Must match the configured <see cref="SymmetricAlgorithm.BlockSize" />.</param>
    /// <param name="tweak">The tweak bytes. Must match the configured <see cref="TweakableSymmetricAlgorithm.TweakSize" />.</param>
    /// <returns>
    /// A <see cref="SimpleReversingCryptoTransform" /> that may be cast to access
    /// <see cref="SimpleReversingCryptoTransform.Diagnostics" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any parameter is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">The key, IV, or tweak length does not match the configured sizes.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
    {
        ThrowIfDisposed();
        Validate(rgbKey, rgbIV, tweak);
        return new SimpleReversingCryptoTransform(
            CreateCipher(rgbKey, BlockSizeBytes, tweak),
            blockMode, PaddingValue, rgbIV, encrypt: true);
    }

    /// <summary>
    /// Creates a <see cref="SimpleReversingCryptoTransform" /> configured for decryption using the specified
    /// key, initialisation vector, and tweak.
    /// </summary>
    /// <param name="rgbKey">The key bytes. Must match the configured <see cref="SymmetricAlgorithm.KeySize" />.</param>
    /// <param name="rgbIV">The IV bytes. Must match the configured <see cref="SymmetricAlgorithm.BlockSize" />.</param>
    /// <param name="tweak">The tweak bytes. Must match the configured <see cref="TweakableSymmetricAlgorithm.TweakSize" />.</param>
    /// <returns>
    /// A <see cref="SimpleReversingCryptoTransform" /> that may be cast to access
    /// <see cref="SimpleReversingCryptoTransform.Diagnostics" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Any parameter is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">The key, IV, or tweak length does not match the configured sizes.</exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV, byte[] tweak)
    {
        ThrowIfDisposed();
        Validate(rgbKey, rgbIV, tweak);
        return new SimpleReversingCryptoTransform(
            CreateCipher(rgbKey, BlockSizeBytes, tweak),
            blockMode, PaddingValue, rgbIV, encrypt: false);
    }

    // ── Key, IV, and tweak generation ─────────────────────────────────────────────────────────

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

    /// <summary>
    /// Generates a cryptographically random tweak of the configured tweak size and assigns it to
    /// <see cref="TweakableSymmetricAlgorithm.Tweak" />.
    /// </summary>
    /// <exception cref="CryptographicException">
    /// <see cref="TweakableSymmetricAlgorithm.TweakSize" /> is not a valid tweak size for this algorithm.
    /// </exception>
    /// <exception cref="ObjectDisposedException">This instance has been disposed.</exception>
    public override void GenerateTweak()
    {
        ThrowIfDisposed();

        if (!ValidTweakSize(TweakSizeValue))
            throw new CryptographicException(
                $"TweakSize ({TweakSizeValue} bits) is not a valid tweak size for this algorithm.");

        TweakValue = CryptoHelpers.GetRandomNonZeroBytes(TweakSizeBytes);
    }

    // ── Disposal ──────────────────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// Key and IV material are zeroed here. <see cref="TweakableSymmetricAlgorithm.Dispose" /> zeros
    /// <see cref="TweakableSymmetricAlgorithm.TweakValue" /> — this override does not duplicate that work.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                CryptoHelpers.Clear(KeyValue);
                CryptoHelpers.Clear(IVValue);
                // TweakValue is zeroed by TweakableSymmetricAlgorithm.Dispose via base.Dispose below.
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

    /// <summary>Gets the tweak size in bytes.</summary>
    private int TweakSizeBytes => TweakSizeValue / 8;

    /// <summary>Returns a new <see cref="SimpleReversingBlockCipher" /> for the given key, block size, and tweak.</summary>
    private static SimpleReversingBlockCipher CreateCipher(byte[] key, int blockSizeBytes, byte[] tweak)
        => new SimpleReversingBlockCipher(key, blockSizeBytes, tweak);

    /// <summary>
    /// Verifies that <paramref name="key" />, <paramref name="iv" />, and <paramref name="tweak" /> each
    /// match the algorithm's configured key size, block size, and tweak size respectively.
    /// </summary>
    /// <exception cref="ArgumentNullException">Any parameter is <see langword="null" />.</exception>
    /// <exception cref="CryptographicException">The key, IV, or tweak length is not valid for this algorithm.</exception>
    private void Validate(byte[] key, byte[] iv, byte[] tweak)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(iv);
        ThrowHelper.ThrowIfNull(tweak);

        if (key.Length != KeySizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidKeySize,
                              key.Length * 8,
                              CryptoHelpers.FormatLegalSizes(LegalKeySizesValue)),
                nameof(key));

        if (iv.Length != BlockSizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidIVSize,
                              iv.Length * 8,
                              CryptoHelpers.FormatLegalSizes(LegalBlockSizesValue)),
                nameof(iv));

        if (tweak.Length != TweakSizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidTweakSize,
                              tweak.Length * 8,
                              CryptoHelpers.FormatLegalSizes(LegalTweakSizesValue)),
                nameof(tweak));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(disposed, this);
#else
        if (this.disposed) throw new ObjectDisposedException(nameof(SimpleReversingTweakableSymmetricAlgorithm));
#endif
    }
}
