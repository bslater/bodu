// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base class for managed implementations of the Threefish tweakable symmetric block cipher family
/// (Threefish-256, Threefish-512, and Threefish-1024).
/// </summary>
/// <remarks>
/// <para>
/// Threefish is the tweakable block cipher used as the core primitive of the Skein hash function. Each variant operates on a block
/// whose size in bits matches its key size (256, 512, or 1024 bits) together with a 128-bit tweak. Derived classes must implement
/// <see cref="CreateCipher(byte[], byte[])"/> to instantiate the appropriate concrete engine.
/// </para>
/// <para>
/// The <see cref="BlockMode"/> property replaces the standard <see cref="SymmetricAlgorithm.Mode"/> property, enabling the use of
/// additional or non-standard block cipher modes such as <see cref="CipherBlockMode.CTR"/> and <see cref="CipherBlockMode.OFB"/>.
/// </para>
/// <para>
/// <strong>Concrete variants.</strong>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Threefish256"/> — 256-bit block, 256-bit key, 128-bit tweak.</description></item>
///   <item><description><see cref="Threefish512"/> — 512-bit block, 512-bit key, 128-bit tweak (the recommended general-purpose default).</description></item>
///   <item><description><see cref="Threefish1024"/> — 1024-bit block, 1024-bit key, 128-bit tweak.</description></item>
/// </list>
/// <para>
/// Threefish is the cipher under the UBI mode of <see cref="Skein{T}"/> — the same key-and-tweak primitive that
/// drives Skein's hash compression. For a non-tweakable, hardware-accelerated default prefer
/// <see cref="System.Security.Cryptography.Aes"/>. For try-pattern transform creation that surfaces bad
/// key/IV/tweak combinations as a <see langword="false"/> return, see
/// <see cref="Bodu.Security.Cryptography.Extensions.TweakableSymmetricAlgorithmExtensions"/>.
/// </para>
/// <note type="important">This class is not intended to be instantiated directly. Use <see cref="Threefish256"/>,
/// <see cref="Threefish512"/>, or <see cref="Threefish1024"/> instead.</note>
/// </remarks>
/// <seealso cref="Threefish256"/>
/// <seealso cref="Threefish512"/>
/// <seealso cref="Threefish1024"/>
/// <seealso cref="TweakableSymmetricAlgorithm"/>
/// <seealso cref="Skein{T}"/>
public abstract class Threefish
    : TweakableSymmetricAlgorithm
{
    /// <summary>
    /// The block size in bytes.
    /// </summary>
    protected readonly int BlockSizeBytes;

    /// <summary>
    /// The key size in bytes.
    /// </summary>
    protected readonly int KeySizeBytes;

    private readonly int _defaultTweakSizeBytes;

    private bool _disposed;

    private BlockPaddingMode _blockPadding = BlockPaddingMode.PKCS7;

    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish"/> class with the specified block and tweak sizes.
    /// </summary>
    /// <param name="blockSizeBits">The block size in bits. Must match the Threefish variant block size (256, 512, or 1024).</param>
    /// <param name="tweakSizeBits">The tweak size in bits. 128 bits for all Threefish variants.</param>
    protected Threefish(int blockSizeBits, int tweakSizeBits)
    {
        this.BlockSizeValue = this.KeySizeValue = blockSizeBits;
        this.FeedbackSizeValue = 8;

        this.BlockSizeBytes = this.KeySizeBytes = blockSizeBits / 8;
        this._defaultTweakSizeBytes = tweakSizeBits / 8;

        this.LegalBlockSizesValue = [new KeySizes(blockSizeBits, blockSizeBits, 0)];
        this.LegalKeySizesValue = [new KeySizes(blockSizeBits, blockSizeBits, 0)];
        this.LegalTweakSizesValue = [new KeySizes(tweakSizeBits, tweakSizeBits, 0)];
        this.TweakSizeValue = tweakSizeBits;

        this.ModeValue = CipherMode.CBC;
        this.Padding = PaddingMode.PKCS7;
    }

    /// <summary>
    /// Gets or sets the block cipher mode of operation used when creating encryptors and decryptors.
    /// </summary>
    /// <value>One of the <see cref="CipherBlockMode"/> values. The default is <see cref="CipherBlockMode.CBC"/>.</value>
    /// <remarks>
    /// This property replaces the inherited <see cref="SymmetricAlgorithm.Mode"/> property when used with
    /// <see cref="BlockCipherModeFactory"/> and the extended set of modes it supports, including <see cref="CipherBlockMode.CTR"/>
    /// and <see cref="CipherBlockMode.OFB"/>.
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

    /// <inheritdoc />
    public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV, tweak);
        IBlockCipher engine = this.CreateCipher(rgbKey, tweak);
        return new ThreefishTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, false);
    }

    /// <inheritdoc />
    public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV, byte[] tweak)
    {
        this.ThrowIfDisposed();
        this.Validate(rgbKey, rgbIV, tweak);
        IBlockCipher engine = this.CreateCipher(rgbKey, tweak);
        return new ThreefishTransform(engine, this.BlockMode, this.BlockPadding, rgbIV, true);
    }

    /// <inheritdoc />
    public override void GenerateIV()
    {
        this.ThrowIfDisposed();
        this.IVValue = CryptoHelpers.GetRandomNonZeroBytes(this.BlockSizeBytes);
    }

    /// <inheritdoc />
    public override void GenerateKey()
    {
        this.ThrowIfDisposed();
        this.KeyValue = CryptoHelpers.GetRandomNonZeroBytes(this.KeySizeBytes);
    }

    /// <inheritdoc />
    public override void GenerateTweak()
    {
        this.ThrowIfDisposed();
        this.TweakValue = CryptoHelpers.GetRandomNonZeroBytes(this._defaultTweakSizeBytes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Marks the instance as disposed, zeroes any retained key and IV buffers, and delegates the tweak buffer cleanup to the
    /// base implementation.
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (!this._disposed)
        {
            if (disposing)
            {
                CryptoHelpers.Clear(this.KeyValue);
                CryptoHelpers.Clear(this.IVValue);
            }

            this._disposed = true;
        }

        base.Dispose(disposing);
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

    /// <summary>
    /// Instantiates the concrete Threefish block cipher with the specified key and tweak.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="tweak">The tweak value.</param>
    /// <returns>A configured <see cref="IBlockCipher"/> instance for encryption or decryption.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">Thrown when the underlying cryptographic algorithm fails.</exception>
    protected abstract IBlockCipher CreateCipher(byte[] key, byte[] tweak);

    /// <summary>
    /// Validates the provided key, IV, and tweak against expected lengths and legal sizes.
    /// </summary>
    /// <param name="key">The encryption key.</param>
    /// <param name="iv">The initialisation vector.</param>
    /// <param name="tweak">The tweak value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/>, <paramref name="iv"/>, or <paramref name="tweak"/> is <see langword="null"/>.</exception>
    /// <exception cref="CryptographicException">Thrown when any input does not match the required length.</exception>
    protected void Validate(byte[] key, byte[]? iv, byte[] tweak)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(tweak);

        if (key.Length != this.KeySizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidKeySize, key.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalKeySizesValue)));

        CryptoHelpers.ThrowIfInvalidIVForMode(iv, this.BlockMode, this.BlockSizeBytes, this.LegalBlockSizes);

        if (tweak.Length != this._defaultTweakSizeBytes)
            throw new CryptographicException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidTweakSize, tweak.Length * 8, CryptoHelpers.FormatLegalSizes(this.LegalTweakSizes)));
    }
}
